using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UHFPS.Input;
using Blockout.Discovery;

namespace DeadAir.Playtesting
{
    [DefaultExecutionOrder(-1000)]
    public sealed partial class DeveloperSettingsMenu : MonoBehaviour
    {
        public bool IsOpen {get;private set;}
        public LoadedSession Selected {get;private set;}
        public double PlaybackTime {get;private set;}
        public float PlaybackSpeed {get;private set;}=1;
        public bool Playing {get;private set;}
        public Camera ReplayCamera=>mapCamera;
        public PositionSample Marker=>Selected==null?default:PlaytestStorage.Evaluate(Selected.samples,PlaybackTime,out _);
        PlaytestRecorder recorder;
        List<SessionMetadata> sessions=new List<SessionMetadata>();
        readonly HashSet<string> comparison=new HashSet<string>();
        readonly List<InputAction> disabledActions=new List<InputAction>();
        readonly string[] tabs={"Analytics Dashboard","Playtest Sessions","Replay Viewer","Heatmap Viewer","Event Log","Export Data","Debug Settings"};
        string message="",versionFilter="",eventFilter="";
        int page=1;
        Vector2 scroll;
        float previousScale,minFloor,maxFloor,zoom=1,panX,panZ;
        CursorLockMode previousCursor;bool previousCursorVisible,fullRoute=true,showEvents=true;
        GUIStyle title,label,small,button,box;
        Texture2D panelTexture,buttonTexture;
        Camera mapCamera;RenderTexture mapTexture;Vector3 mapCenter;float mapSpan=10;double nextMapRender;
        Rect mapRect;
        void Start(){recorder=GetComponent<PlaytestRecorder>();}
        void Update()
        {
            if(!recorder)recorder=GetComponent<PlaytestRecorder>();if(!recorder||!recorder.Settings||!recorder.Settings.Available)return;
            if(Keyboard.current!=null&&Keyboard.current[recorder.Settings.MenuKey].wasPressedThisFrame)
            {if(IsOpen)Close();else if(!VirgoTerminal.Active)Open();}
            if(IsOpen)Advance(Time.unscaledDeltaTime);
            if(mapCamera)mapCamera.enabled=IsOpen&&(page==2||page==3);
        }
        public void Advance(float seconds){if(Playing&&Selected!=null){PlaybackTime=Math.Min(Selected.metadata.duration,PlaybackTime+Math.Max(0,seconds)*PlaybackSpeed);if(PlaybackTime>=Selected.metadata.duration)Playing=false;}}
        public void Open()
        {
            if(IsOpen||!recorder||!recorder.Settings.Available)return;
            IsOpen=true;recorder.Flush();recorder.DeveloperViewOpen=true;
            previousScale=Time.timeScale;previousCursor=Cursor.lockState;previousCursorVisible=Cursor.visible;
            if(InputManager.HasReference)
                foreach(var action in InputManager.ActionsAsset)if(action.enabled){disabledActions.Add(action);action.Disable();}
            Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            minFloor=recorder.Settings.MinimumElevation;maxFloor=recorder.Settings.MaximumElevation;RefreshSessions();
        }
        public void Close()
        {
            if(!IsOpen)return;IsOpen=false;Playing=false;
            foreach(var action in disabledActions)if(action!=null)action.Enable();disabledActions.Clear();
            Time.timeScale=previousScale;Cursor.lockState=previousCursor;Cursor.visible=previousCursorVisible;
            if(recorder)recorder.DeveloperViewOpen=false;
            if(mapCamera)mapCamera.enabled=false;
        }
        public void RefreshSessions(){sessions=PlaytestStorage.ListSessions(out var warning);message=warning;}
        public bool SelectSession(string id)
        {
            try{Selected=PlaytestStorage.Load(id);PlaybackTime=0;Playing=false;zoom=1;panX=panZ=0;page=2;FitMap();message=Selected.warning;return true;}
            catch(Exception e){message="Cannot load session: "+e.Message;Selected=null;return false;}
        }
        public void Seek(double time){if(Selected!=null)PlaybackTime=Math.Max(0,Math.Min(Selected.metadata.duration,time));}
        public void SetSpeed(float speed){PlaybackSpeed=speed;}
        public void SetPlaying(bool value){Playing=value&&Selected!=null&&Selected.samples.Count>0;}
        void FitMap()
        {
            if(Selected==null||Selected.samples.Count==0){mapCenter=Vector3.zero;mapSpan=10;return;}
            var bounds=new Bounds(Selected.samples[0].position,Vector3.one);
            foreach(var sample in Selected.samples)bounds.Encapsulate(sample.position);
            mapCenter=bounds.center;mapSpan=Mathf.Max(8,Mathf.Max(bounds.size.x,bounds.size.z)*.6f);nextMapRender=0;
        }
        static Texture2D Texture(Color color){var t=new Texture2D(1,1){hideFlags=HideFlags.HideAndDontSave};t.SetPixel(0,0,color);t.Apply();return t;}
        void Styles()
        {
            if(label!=null)return;
            panelTexture=Texture(new Color(.055f,.065f,.085f));buttonTexture=Texture(new Color(.13f,.17f,.22f));
            label=new GUIStyle(GUI.skin.label){fontSize=15,wordWrap=true};label.normal.textColor=new Color(.88f,.91f,.96f);
            small=new GUIStyle(label){fontSize=12};title=new GUIStyle(label){fontSize=25,fontStyle=FontStyle.Bold};
            button=new GUIStyle(GUI.skin.button){fontSize=14,padding=new RectOffset(10,10,7,7)};button.normal.background=buttonTexture;button.normal.textColor=Color.white;
            box=new GUIStyle(GUI.skin.box){normal={background=panelTexture},padding=new RectOffset(14,14,12,12)};
        }
        void OnGUI()
        {
            if(!IsOpen)return;Styles();GUI.depth=-10000;
            var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/800f,1));
            GUI.Box(new Rect(0,0,1280,800),GUIContent.none,box);
            GUI.Label(new Rect(24,18,1000,38),"DEAD AIR  /  DEVELOPER SETTINGS",title);
            if(GUI.Button(new Rect(1120,18,136,36),"Close  /  "+recorder.Settings.MenuKey,button))Close();
            for(int i=0;i<tabs.Length;i++)if(GUI.Button(new Rect(20,85+i*54,220,44),(page==i?"›  ":"")+tabs[i],button)){page=i;scroll=Vector2.zero;}
            GUI.Label(new Rect(24,490,210,120),recorder.Recording?"RECORDING SUSPENDED\nDeveloper viewing time is excluded.\nActive time: "+Clock(recorder.Elapsed):"NOT RECORDING\n"+recorder.LastError,small);
            if(recorder.Recording)
            {if(GUI.Button(new Rect(24,635,212,40),"Stop recording",button)){recorder.Stop();RefreshSessions();}}
            else if(GUI.Button(new Rect(24,635,212,40),"Start new recording",button)){Close();recorder.Begin();}
            GUI.Label(new Rect(260,70,990,24),"Prototype version filter",small);versionFilter=GUI.TextField(new Rect(420,68,225,26),versionFilter);
            if(GUI.Button(new Rect(1070,65,185,30),"Refresh sessions",button))RefreshSessions();
            GUILayout.BeginArea(new Rect(260,110,995,620));
            if(page==1)SessionList();else if(page==2)Replay();else DrawAnalysisPage(page);
            GUILayout.EndArea();GUI.Label(new Rect(260,742,995,50),message,small);
            GUI.matrix=old;
            if(Event.current.type==EventType.KeyDown||Event.current.type==EventType.MouseDown||Event.current.type==EventType.ScrollWheel)Event.current.Use();
        }
        bool Matches(SessionMetadata m)=>string.IsNullOrWhiteSpace(versionFilter)||m.version.IndexOf(versionFilter,StringComparison.OrdinalIgnoreCase)>=0;
        void SessionList()
        {
            GUILayout.Label("PLAYTEST SESSIONS",title);GUILayout.Label("Select a session to replay. Check multiple sessions for dashboard/heatmap comparison.",label);
            scroll=GUILayout.BeginScrollView(scroll);
            foreach(var s in sessions)
            {
                if(!Matches(s))continue;GUILayout.BeginHorizontal(box);
                bool use=comparison.Contains(s.id),next=GUILayout.Toggle(use,"",GUILayout.Width(24));if(next!=use){if(next)comparison.Add(s.id);else comparison.Remove(s.id);}
                GUILayout.Label(s.startedUtc+"\n"+s.sceneName+"  /  v"+s.version+"  /  "+s.status+"  /  "+Clock(s.duration),label);
                if(GUILayout.Button("Replay",button,GUILayout.Width(100))&&SelectSession(s.id))page=2;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
        void Replay()
        {
            if(Selected==null){GUILayout.Label("Select a saved playtest from Playtest Sessions.",label);return;}
            GUILayout.BeginHorizontal();GUILayout.Label(Selected.metadata.sceneName+"  /  "+Selected.metadata.id.Substring(0,8),label);
            fullRoute=GUILayout.Toggle(fullRoute,"Full route");showEvents=GUILayout.Toggle(showEvents,"Events");GUILayout.EndHorizontal();
            DrawMap(false);
            GUILayout.BeginHorizontal();
            if(GUILayout.Button(Playing?"Pause":"Play",button,GUILayout.Width(90)))SetPlaying(!Playing);
            if(GUILayout.Button("Restart",button,GUILayout.Width(90))){Seek(0);SetPlaying(true);}
            foreach(float speed in new[]{.5f,1,2,4,8})if(GUILayout.Button((PlaybackSpeed==speed?"● ":"")+speed+"x",button,GUILayout.Width(70)))SetSpeed(speed);
            GUILayout.Label(Clock(PlaybackTime)+" / "+Clock(Selected.metadata.duration),label);GUILayout.EndHorizontal();
            Seek(GUILayout.HorizontalSlider((float)PlaybackTime,0,(float)Math.Max(.001,Selected.metadata.duration)));
            MapControls();
        }
        void MapControls()
        {
            GUILayout.BeginHorizontal();GUILayout.Label("Elevation",small,GUILayout.Width(65));
            float.TryParse(GUILayout.TextField(minFloor.ToString("0.##"),GUILayout.Width(60)),out minFloor);
            GUILayout.Label("to",small,GUILayout.Width(20));float.TryParse(GUILayout.TextField(maxFloor.ToString("0.##"),GUILayout.Width(60)),out maxFloor);
            if(maxFloor<=minFloor)maxFloor=minFloor+.1f;
            GUILayout.Label("Zoom",small,GUILayout.Width(40));zoom=GUILayout.HorizontalSlider(zoom,.3f,4,GUILayout.Width(140));
            if(GUILayout.Button("←",button,GUILayout.Width(35)))panX-=mapSpan*.15f;
            if(GUILayout.Button("→",button,GUILayout.Width(35)))panX+=mapSpan*.15f;
            if(GUILayout.Button("↑",button,GUILayout.Width(35)))panZ+=mapSpan*.15f;
            if(GUILayout.Button("↓",button,GUILayout.Width(35)))panZ-=mapSpan*.15f;
            if(GUILayout.Button("Fit route",button,GUILayout.Width(95))){panX=panZ=0;zoom=1;FitMap();}
            GUILayout.EndHorizontal();
        }
        void DrawMap(bool heatmap)
        {
            mapRect=GUILayoutUtility.GetRect(985,420,GUILayout.ExpandWidth(true));
            if(Event.current.type!=EventType.Repaint)return;
            if(!mapCamera)
            {
                var go=new GameObject("Dead Air - Replay Camera"){hideFlags=HideFlags.HideAndDontSave};go.transform.SetParent(transform);
                mapCamera=go.AddComponent<Camera>();mapCamera.enabled=false;mapCamera.orthographic=true;mapCamera.clearFlags=CameraClearFlags.SolidColor;mapCamera.backgroundColor=new Color(.025f,.03f,.04f);
                var data=mapCamera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=false;
                mapTexture=new RenderTexture(1600,800,24){hideFlags=HideFlags.HideAndDontSave};mapCamera.targetTexture=mapTexture;
            }
            float height=Mathf.Max(recorder.Settings.CameraHeight,maxFloor+1);
            mapCamera.transform.SetPositionAndRotation(new Vector3(mapCenter.x+panX,height,mapCenter.z+panZ),Quaternion.Euler(90,0,0));
            mapCamera.orthographicSize=mapSpan/zoom;mapCamera.aspect=mapRect.width/mapRect.height;
            mapCamera.nearClipPlane=Mathf.Max(.01f,height-maxFloor);mapCamera.farClipPlane=Mathf.Max(mapCamera.nearClipPlane+.1f,height-minFloor);
            bool same=Selected!=null&&Selected.metadata.scenePath==SceneManager.GetActiveScene().path;
            mapCamera.cullingMask=same?recorder.Settings.MapLayers.value:0;
            // Let URP schedule this camera normally; nested Camera.Render from OnGUI
            // re-enters the active render graph in Unity 6.6.
            mapCamera.enabled=true;
            GUI.DrawTexture(mapRect,mapTexture,ScaleMode.StretchToFill);
            if(Selected==null)return;
            GUI.BeginGroup(mapRect);var inside=new Rect(0,0,mapRect.width,mapRect.height);
            if(heatmap)DrawHeatmapOverlay(inside);
            else
            {
                var samples=Selected.samples;int stride=Mathf.Max(1,samples.Count/12000);
                for(int i=stride;i<samples.Count;i+=stride)
                {
                    var a=samples[i-stride];var b=samples[i];if(!Visible(a.position)||!Visible(b.position)||(b.flags&1)!=0)continue;
                    if(fullRoute)Line(Project(a.position,inside),Project(b.position,inside),new Color(.25f,.4f,.55f,.45f),1);
                    if(b.time<=PlaybackTime)Line(Project(a.position,inside),Project(b.position,inside),new Color(.1f,.85f,1),2);
                }
                if(samples.Count>0)
                {
                    var marker=PlaytestStorage.Evaluate(samples,PlaybackTime,out int index);
                    if(Visible(marker.position))
                    {
                        if(index>0)Line(Project(samples[index].position,inside),Project(marker.position,inside),Color.cyan,2);
                        var p=Project(marker.position,inside);var forward=Project(marker.position+marker.rotation*Vector3.forward*mapSpan/zoom*.06f,inside);
                        Dot(p,Color.white,9);Line(p,forward,Color.yellow,4);Dot(forward,Color.yellow,4);
                    }
                }
                if(showEvents)foreach(var e in Selected.events)if(e.time<=PlaybackTime&&Visible(e.position)&&e.type!="movement_sample")Dot(Project(e.position,inside),new Color(1,.5f,.15f),3);
            }
            GUI.EndGroup();
            if(!same)GUI.Label(new Rect(mapRect.x+10,mapRect.y+8,mapRect.width-20,40),"Route only — open the recorded scene for its blockout overlay.",small);
        }
        bool Visible(Vector3 p)=>p.y>=minFloor&&p.y<=maxFloor;
        Vector2 Project(Vector3 p,Rect rect){var v=mapCamera.WorldToViewportPoint(p);return new Vector2(v.x*rect.width,(1-v.y)*rect.height);}
        static void Dot(Vector2 p,Color color,float size){var old=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(p.x-size*.5f,p.y-size*.5f,size,size),Texture2D.whiteTexture);GUI.color=old;}
        static void Line(Vector2 a,Vector2 b,Color color,float width)
        {
            var matrix=GUI.matrix;var old=GUI.color;GUI.color=color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y-width*.5f,Vector2.Distance(a,b),width),Texture2D.whiteTexture);GUI.matrix=matrix;GUI.color=old;
        }
        public static string Clock(double value)=>TimeSpan.FromSeconds(Math.Max(0,value)).ToString(@"hh\:mm\:ss\.f");
        partial void DrawAnalysisPage(int tab);
        partial void DrawHeatmapOverlay(Rect rect);
        void OnDisable()=>Close();
        void OnDestroy(){Close();if(mapTexture){mapTexture.Release();Destroy(mapTexture);}if(mapCamera)Destroy(mapCamera.gameObject);if(panelTexture)Destroy(panelTexture);if(buttonTexture)Destroy(buttonTexture);}
    }
}
