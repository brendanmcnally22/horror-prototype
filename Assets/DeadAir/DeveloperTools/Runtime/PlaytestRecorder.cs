using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UHFPS.Runtime;

namespace DeadAir.Playtesting
{
    [DefaultExecutionOrder(500)]
    public sealed class PlaytestRecorder : MonoBehaviour
    {
        public static PlaytestRecorder Instance {get;private set;}
        public PlaytestSettings Settings {get;private set;}
        public SessionMetadata Metadata {get;private set;}
        public PlayerManager Player {get;private set;}
        public bool Recording=>positions!=null;
        public bool DeveloperViewOpen {get;set;}
        public string LastError {get;private set;}="";
        public string CurrentFolder {get;private set;}="";
        public double Elapsed {get;private set;}
        public event Action<PlaytestEvent> EventRecorded;
        BinaryWriter positions;StreamWriter events;
        double wallStart,nextSample,nextFlush,nextFind;
        bool stopped,appPaused,hasLast,hasPending,pausedLast;
        PositionSample last,pending;
        readonly Dictionary<string,int> states=new Dictionary<string,int>();
        readonly Dictionary<string,int> areaIndices=new Dictionary<string,int>();
        readonly Dictionary<string,AreaVisit> activeAreas=new Dictionary<string,AreaVisit>();
        readonly List<string> areaOrder=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){Instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            var settings=Resources.Load<PlaytestSettings>("DeadAirPlaytestSettings");
            if(!settings||!settings.Available||Instance)return;
            var go=new GameObject("Dead Air - Developer Tools");DontDestroyOnLoad(go);
            var recorder=go.AddComponent<PlaytestRecorder>();recorder.Settings=settings;
            if(settings.EnableDeveloperUI)go.AddComponent<DeveloperSettingsMenu>();
        }
        void Awake(){if(Instance&&Instance!=this){Destroy(gameObject);return;}Instance=this;SceneManager.activeSceneChanged+=SceneChanged;}
        void SceneChanged(Scene before,Scene after){if(Recording)Stop("exited","scene_changed");stopped=false;Player=null;nextFind=0;}
        void Update()
        {
            if(!Settings||!Settings.Available)return;
            if(Time.realtimeSinceStartupAsDouble>=nextFind)
            {
                nextFind=Time.realtimeSinceStartupAsDouble+1;
                if(!Player)Player=FindAnyObjectByType<PlayerManager>();
                if(!Recording&&!stopped&&Settings.AutoRecord&&Player&&Ready())Begin();
            }
            if(!Recording)return;
            if(!Player){Stop("exited","player_unloaded");return;}
            bool paused=DeveloperViewOpen||appPaused||Time.timeScale<=0||(GameManager.HasReference&&GameManager.Instance.IsPaused);
            if(paused!=pausedLast){RecordEvent(paused?"pause_started":"pause_ended","session",Player.transform.position);pausedLast=paused;}
            if(!paused)
            {
                Elapsed+=Time.unscaledDeltaTime;
                if(Elapsed>=nextSample){Capture(false);nextSample=Elapsed+1d/Mathf.Clamp(Settings.SamplesPerSecond,1,60);}
            }
            if(Time.realtimeSinceStartupAsDouble>=nextFlush){Flush();nextFlush=Time.realtimeSinceStartupAsDouble+Mathf.Max(1,Settings.FlushInterval);}
        }
        bool Ready()=>!GameManager.HasReference||GameManager.Instance.PlayerPresence.IsUnlockedAndCamera;
        public bool Begin()
        {
            if(Recording||DeveloperViewOpen||!Settings||!Settings.Available)return false;
            if(!Player)Player=FindAnyObjectByType<PlayerManager>();if(!Player){LastError="No active player found. Start gameplay first.";return false;}
            var scene=SceneManager.GetActiveScene();
            Metadata=new SessionMetadata{id=Guid.NewGuid().ToString("N"),startedUtc=DateTime.UtcNow.ToString("O"),sceneName=scene.name,scenePath=scene.path,
                version=string.IsNullOrWhiteSpace(Settings.PrototypeVersion)?Application.version:Settings.PrototypeVersion,buildGuid=Application.buildGUID,unityVersion=Application.unityVersion};
            Elapsed=0;states.Clear();areaIndices.Clear();activeAreas.Clear();areaOrder.Clear();hasLast=hasPending=false;stopped=false;pausedLast=false;LastError="";
            wallStart=Time.realtimeSinceStartupAsDouble;nextSample=0;nextFlush=wallStart+Settings.FlushInterval;
            CurrentFolder=Path.Combine(PlaytestStorage.Root,Metadata.id);
            try
            {
                Directory.CreateDirectory(CurrentFolder);
                positions=new BinaryWriter(new FileStream(Path.Combine(CurrentFolder,"positions.bin"),FileMode.CreateNew,FileAccess.Write,FileShare.Read,65536));positions.Write(PlaytestStorage.Magic);positions.Write(1);
                events=new StreamWriter(new FileStream(Path.Combine(CurrentFolder,"events.jsonl"),FileMode.CreateNew,FileAccess.Write,FileShare.Read,16384),new UTF8Encoding(false));
                RecordEvent("session_started","session",Player.transform.position);Capture(true);Flush();return true;
            }
            catch(Exception e){Fail(e);return false;}
        }
        PositionSample Sample()
        {
            string state=Player.PlayerStateMachine?Player.PlayerStateMachine.CurrentStateKey:"unknown";state??="unknown";
            if(!states.TryGetValue(state,out int index)){index=Metadata.movementStates.Count;states.Add(state,index);Metadata.movementStates.Add(state);}
            var camera=Player.MainCamera;
            return new PositionSample{time=Elapsed,position=Player.transform.position,rotation=Player.transform.rotation,
                camera=Settings.RecordCamera&&camera?camera.transform.rotation:Player.transform.rotation,state=index,area=areaOrder.Count>0?areaIndices[areaOrder[areaOrder.Count-1]]:-1};
        }
        void Capture(bool force)
        {
            if(!Recording||!Player)return;
            try
            {
                var current=Sample();
                bool changed=!hasLast||(current.position-last.position).sqrMagnitude>.000004f||Quaternion.Angle(current.rotation,last.rotation)>.25f||
                    Quaternion.Angle(current.camera,last.camera)>.25f||current.state!=last.state||current.area!=last.area;
                if(changed||force||!hasLast||Elapsed-last.time>=Mathf.Max(.1f,Settings.StationaryHeartbeat))
                {
                    if(changed&&hasPending&&pending.time>last.time)Write(pending);
                    if(hasLast&&Vector3.Distance(current.position,last.position)>Mathf.Max(3,(float)(current.time-last.time)*30))current.flags|=1;
                    Write(current);hasPending=false;
                }
                else{pending=current;hasPending=true;}
            }
            catch(Exception e){Fail(e);}
        }
        void Write(PositionSample sample){PlaytestStorage.Write(positions,sample);last=sample;hasLast=true;Metadata.sampleCount++;}
        public void RecordEvent(string type,string objectId,Vector3 position,float value=0,float duration=0,string metadata="")
        {
            if(!Recording||DeveloperViewOpen)return;
            var e=new PlaytestEvent{time=Elapsed,type=type,objectId=objectId??"",position=position,value=value,duration=duration,metadata=metadata??""};
            try{events.WriteLine(JsonUtility.ToJson(e));Metadata.eventCount++;}
            catch(Exception error){Fail(error);return;}
            EventRecorded?.Invoke(e);
        }
        public static void Record(string type,string id,Vector3 position,float value=0,float duration=0,string metadata="")
        {if(Instance)Instance.RecordEvent(type,id,position,value,duration,metadata);}
        public void EnterArea(string id,string displayName,string category="")
        {
            if(!Recording||string.IsNullOrWhiteSpace(id)||activeAreas.ContainsKey(id))return;
            Capture(true);
            if(!areaIndices.ContainsKey(id)){areaIndices.Add(id,Metadata.areas.Count);Metadata.areas.Add(new AreaDefinition{id=id,displayName=displayName,category=category});}
            var visit=new AreaVisit{areaId=id,entered=Elapsed,exited=-1};Metadata.visits.Add(visit);activeAreas.Add(id,visit);areaOrder.Add(id);
            RecordEvent("area_enter",id,Player.transform.position);Capture(true);
        }
        public void ExitArea(string id)
        {
            if(!Recording||!activeAreas.TryGetValue(id,out var visit))return;
            Capture(true);visit.exited=Elapsed;activeAreas.Remove(id);areaOrder.Remove(id);
            RecordEvent("area_exit",id,Player?Player.transform.position:Vector3.zero,0,(float)(visit.exited-visit.entered));Capture(true);
        }
        public void Complete(string reason="prototype_completed")=>Stop("completed",reason);
        public void Stop(string status="stopped",string reason="developer_stop")
        {
            stopped=true;if(!Recording)return;
            Capture(true);if(!Recording)return;
            foreach(var visit in activeAreas.Values)visit.exited=Elapsed;
            activeAreas.Clear();areaOrder.Clear();
            RecordEvent(status=="completed"?"prototype_completed":"session_ended","session",Player?Player.transform.position:Vector3.zero,0,0,reason);
            Metadata.status=status;Metadata.endReason=reason;Metadata.endedUtc=DateTime.UtcNow.ToString("O");Flush();DisposeWriters();
        }
        public void Flush()
        {
            if(!Recording)return;
            try{positions.Flush();events.Flush();Metadata.duration=Elapsed;Metadata.wallDuration=Time.realtimeSinceStartupAsDouble-wallStart;PlaytestStorage.SaveMetadata(CurrentFolder,Metadata);}
            catch(Exception e){Fail(e);}
        }
        void Fail(Exception e){LastError=e.Message;Debug.LogWarning("Dead Air playtest recording stopped: "+e.Message);DisposeWriters();stopped=true;}
        void DisposeWriters(){try{positions?.Dispose();}catch{}try{events?.Dispose();}catch{}positions=null;events=null;}
        void OnApplicationPause(bool paused){appPaused=paused;if(paused)Flush();}
        void OnApplicationQuit()=>Stop("exited","application_exit");
        void OnDestroy(){SceneManager.activeSceneChanged-=SceneChanged;if(Instance==this){Stop("exited","play_mode_or_scene_exit");Instance=null;}}
    }
}
