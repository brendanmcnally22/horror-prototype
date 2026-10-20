using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Blockout.Discovery;
using UHFPS.Runtime;

[InitializeOnLoad]
public static class FacilityIterationCheck
{
    const string Key="FacilityIterationCheck.Active";
    static double start,step,lastProgress;
    static int phase,lastFrame,corner;
    static PlayerManager hero;
    static PrototypeMover elevator;
    static FacilityPanelUse panel;
    static InteractableItem[] fuses;
    static readonly HashSet<PrototypeLever> used=new HashSet<PrototypeLever>();
    static readonly List<string> report=new List<string>();
    static readonly Dictionary<string,string> approaches=new Dictionary<string,string>();
    static string errors="";
    static NavMeshData navData;
    static NavMeshDataInstance navInstance;
    static Vector3[] route;
    static Component destination;
    static bool returning;
    static int collected;
    static Vector3 lastPosition;
    static Vector3 liftStartPosition;
    static FacilityIterationCheck()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>{
            if(!SessionState.GetBool(Key,false))return;
            if(s==PlayModeStateChange.EnteredPlayMode){start=EditorApplication.timeSinceStartup;phase=0;report.Clear();used.Clear();errors="";collected=0;returning=false;route=null;Application.runInBackground=true;}
            if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);var previous=SessionState.GetString(Key+".Previous","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(previous)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);}
        };
        Application.logMessageReceived+=(m,t,type)=>{if(SessionState.GetBool(Key,false)&&(type==LogType.Error||type==LogType.Exception)&&!m.StartsWith("Layout update failed"))errors+=m+"\n";};
    }
    static void Require(bool value,string text){if(!value)throw new Exception(text);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false))
        {
            if(!File.Exists("Logs/FacilityIterationCheck.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete("Logs/FacilityIterationCheck.request");SessionState.SetString(Key+".Previous",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));EditorSceneManager.playModeStartScene=null;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;return;
        }
        if(!EditorApplication.isPlaying||start==0||phase==99)return;
        try
        {
            if(EditorApplication.timeSinceStartup-start>360)throw new Exception("Traversal time limit reached.");
            if(phase==0)
            {
                if(EditorApplication.timeSinceStartup-start<8)return;
                hero=PlayerManager.Instance;panel=UnityEngine.Object.FindAnyObjectByType<FacilityPanelUse>();Require(panel,"Panel interaction missing.");
                elevator=UnityEngine.Object.FindObjectsByType<PrototypeMover>(FindObjectsSortMode.None).Single(m=>m.name=="STARTING ELEVATOR");
                fuses=UnityEngine.Object.FindObjectsByType<InteractableItem>(FindObjectsSortMode.None).Where(f=>f.name.StartsWith("Fuse - Optional Pickup")).ToArray();Require(fuses.Length==3,"Expected the actual three placed fuses.");
                var player=hero.PlayerStateMachine;player.ChangeState(PlayerStateMachine.JUMP_STATE);Require(!player.IsCurrent(PlayerStateMachine.JUMP_STATE),"Jump still available.");report.Add("PASS: player cannot transition into Jump.");
                var jams=UnityEngine.Object.FindObjectsByType<PrototypeLever>(FindObjectsSortMode.None).Where(l=>l.Jammed).ToArray();Require(jams.Length==2,"Expected two jammed levers.");
                foreach(var lever in jams){bool on=lever.IsOn;var targets=lever.Targets.Where(t=>t).Select(t=>t.TargetEnd).ToArray();lever.InteractStart();Require(lever.IsOn==on&&targets.SequenceEqual(lever.Targets.Where(t=>t).Select(t=>t.TargetEnd)),"Jammed lever operated a target.");}
                report.Add("PASS: both jammed levers reject activation.");
                var control=elevator.GetComponentInChildren<PrototypeLever>();Require(control&&control.Targets.Contains(elevator),"Starting elevator lever is not wired.");
                var marker=new GameObject("Temporary trigger verification");var metric=marker.AddComponent<DesignMetricTrigger>();metric.ZoneId="audit_zone";
                metric.SendMessage("OnTriggerEnter",hero.PlayerCollider);metric.SendMessage("OnTriggerEnter",hero.PlayerCollider);metric.SendMessage("OnTriggerExit",hero.PlayerCollider);UnityEngine.Object.Destroy(marker);
                report.Add("Checked tracking trigger entry deduplication and exit; verify audit_zone rows in CSV.");
                GameManager.Instance.FreezePlayer(true);liftStartPosition=hero.transform.position;
                Vector3 doorway=elevator.MovingPart.TransformPoint(new Vector3(0,0,-1.1f));doorway.y=hero.transform.position.y;hero.PlayerCollider.Move(doorway-hero.transform.position);
                control.InteractStart();step=EditorApplication.timeSinceStartup;phase=5;return;
            }
            if(phase==5)
            {
                if(EditorApplication.timeSinceStartup-step<1)return;
                var gate=elevator.GetComponentInChildren<PlatformSafetyGate>();Require(gate.Blocked,"Player in gate did not block departure.");Require(Mathf.Abs(elevator.MovingPart.localPosition.y)<.01f,"Elevator moved with player obstructing doorway.");
                report.Add("PASS: player obstruction kept elevator stationary and reopened gate.");hero.PlayerCollider.Move(liftStartPosition-hero.transform.position);step=EditorApplication.timeSinceStartup;phase=1;return;
            }
            if(phase==1)
            {
                var gate=elevator.GetComponentInChildren<PlatformSafetyGate>();Require(gate,"Starting gate missing.");
                if(elevator.TravelRequested&&elevator.MovingPart.localPosition.y<-.05f)Require(gate.ReadyForTravel,"Elevator moved before gate closed.");
                if(!elevator.AtEnd){if(EditorApplication.timeSinceStartup-step>25)throw new Exception("Starting elevator did not reach landing.");return;}
                if(!gate.IsOpen)return;
                if(EditorApplication.timeSinceStartup-step<10)return;
                report.Add("PASS: starting lift descended with closed doors, then opened at lower landing. Hero position "+hero.transform.position);
                GameManager.Instance.FreezePlayer(true);BuildNavigation();phase=2;
            }
            if(phase==2)
            {
                if(collected==3&&!returning){Finish("PASS");return;}
                if(!panel.IsOpen||returning)
                {
                    if(!SetRoute(panel))throw new Exception("No walkable interaction approach to the electrical panel from "+hero.transform.position);
                }
                else
                {
                    bool found=false;
                    foreach(var fuse in fuses.Where(f=>f&&f.gameObject.activeInHierarchy))if(SetRoute(fuse)){found=true;break;}
                    if(!found)
                    {
                        foreach(var lever in UnityEngine.Object.FindObjectsByType<PrototypeLever>(FindObjectsSortMode.None).Where(l=>!l.Jammed&&!used.Contains(l)&&l.Targets.Any(t=>t&&!t.TargetEnd)))
                            if(SetRoute(lever)){found=true;used.Add(lever);break;}
                    }
                    if(!found)throw new Exception("No further fuse or usable lever reachable by a walking-only route. Remaining: "+string.Join(", ",fuses.Where(f=>f&&f.gameObject.activeInHierarchy).Select(f=>f.name+" "+f.transform.position)));
                }
                phase=3;lastProgress=EditorApplication.timeSinceStartup;lastPosition=hero.transform.position;return;
            }
            if(phase==3)
            {
                if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
                if(corner<route.Length)
                {
                    Vector3 delta=route[corner]-hero.transform.position;delta.y=0;
                    if(delta.magnitude<.08f){corner++;return;}
                    hero.PlayerCollider.Move(Vector3.ClampMagnitude(delta,4*Time.deltaTime)+Vector3.down*2*Time.deltaTime);
                    if(Vector3.Distance(lastPosition,hero.transform.position)>.2f){lastProgress=EditorApplication.timeSinceStartup;lastPosition=hero.transform.position;}
                    if(EditorApplication.timeSinceStartup-lastProgress>5)throw new Exception("Walking route blocked at "+hero.transform.position+" while approaching "+destination.name);
                    return;
                }
                if(destination is FacilityPanelUse p)
                {
                    int before=p.Panel.Fuses.Count(f=>f.IsInserted);p.InteractStart();
                    if(returning){Require(p.Panel.Fuses.Count(f=>f.IsInserted)==before+1,"Native panel did not insert the carried fuse.");returning=false;report.Add("PASS: returned fuse "+collected+" through the panel interaction.");}
                    else report.Add("PASS: walked to electrical panel and opened cover.");
                    if(collected==3){Require(UnityEngine.Object.FindAnyObjectByType<FacilityPowerSequence>().PowerRestored,"Third return did not restore power.");}
                    phase=2;return;
                }
                if(destination is InteractableItem item)
                {
                    int before=Inventory.Instance.GetItemQuantity(panel.Panel.FuseItem);item.InteractWithObject();Require(Inventory.Instance.GetItemQuantity(panel.Panel.FuseItem)==before+1,"Placed fuse failed to enter inventory.");
                    report.Add("PASS: walked to and collected "+item.name+" at "+item.transform.position);collected++;returning=true;phase=2;return;
                }
                if(destination is PrototypeLever lever){lever.InteractStart();report.Add("Operated reachable lever: "+lever.name);step=EditorApplication.timeSinceStartup;phase=4;return;}
            }
            if(phase==4)
            {
                var lever=(PrototypeLever)destination;
                if(lever.Targets.Any(t=>t&&t.TravelRequested)){if(EditorApplication.timeSinceStartup-step>40)throw new Exception("Platform or door did not finish after reachable lever.");return;}
                BuildNavigation();phase=2;
            }
        }
        catch(Exception e)
        {
            report.Add(e.Message);foreach(var pair in approaches)report.Add(pair.Key+": "+pair.Value);
            // Separate component verification from walking reachability. Never claim these pickups were reached.
            if(panel&&fuses!=null)try
            {
                DesignSession.Record("audit_component_test","remaining_fuses",hero.transform.position);
                if(!panel.IsOpen)panel.InteractStart();
                foreach(var fuse in fuses.Where(f=>f&&f.gameObject.activeInHierarchy))
                {
                    int before=panel.Panel.Fuses.Count(f=>f.IsInserted);fuse.InteractWithObject();panel.InteractStart();Require(panel.Panel.Fuses.Count(f=>f.IsInserted)>before,"Component check: fuse failed to insert.");
                }
                Require(UnityEngine.Object.FindAnyObjectByType<FacilityPowerSequence>().PowerRestored,"Component check: third fuse failed to restore facility power.");
                report.Add("PASS COMPONENT CHECK ONLY: all three existing pickups can be consumed by the panel and the final return restores facility power. Remaining pickups invoked directly; this does not prove their walking routes.");
            }catch(Exception error){report.Add("FAIL component check: "+error.Message);}
            Finish("INCOMPLETE");
        }
    }
    static void BuildNavigation()
    {
        navInstance.Remove();if(navData)UnityEngine.Object.Destroy(navData);
        var sources=new List<NavMeshBuildSource>();NavMeshBuilder.CollectSources(new Bounds(new Vector3(-125,8,10),new Vector3(100,30,120)),LayerMask.GetMask("Default","Ground","Slope","Interact"),NavMeshCollectGeometry.PhysicsColliders,0,new List<NavMeshBuildMarkup>(),sources);
        // This temporary audit mesh never ships. Use bounds for imported non-readable meshes.
        for(int i=0;i<sources.Count;i++){var src=sources[i];if(src.sourceObject is Mesh mesh&&!mesh.isReadable){src.shape=NavMeshBuildSourceShape.Box;src.transform=src.transform*Matrix4x4.Translate(mesh.bounds.center);src.size=mesh.bounds.size;src.sourceObject=null;sources[i]=src;}}
        var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=hero.PlayerCollider.radius+.02f;settings.agentHeight=hero.PlayerCollider.height;settings.agentClimb=hero.PlayerCollider.stepOffset;settings.overrideVoxelSize=true;settings.voxelSize=.08f;
        navData=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(-125,8,10),new Vector3(100,30,120)),Vector3.zero,Quaternion.identity);Require(navData,"Navigation audit mesh failed.");navInstance=NavMesh.AddNavMeshData(navData);
    }
    static bool SetRoute(Component target)
    {
        var collider=target.GetComponent<Collider>();if(!collider)return false;Vector3 aim=collider.bounds.center;
        if(!NavMesh.SamplePosition(hero.transform.position,out var source,1.5f,NavMesh.AllAreas))return false;
        NavMeshPath best=null;float bestLength=float.MaxValue;int sampled=0,seen=0,complete=0;
        foreach(float radius in new[]{.7f,1.2f,1.8f,2.3f})for(int i=0;i<16;i++)
        {
            float angle=i*Mathf.PI/8;var point=aim+new Vector3(Mathf.Cos(angle)*radius,-1,Mathf.Sin(angle)*radius);
            if(!NavMesh.SamplePosition(point,out var hit,1.2f,NavMesh.AllAreas))continue;
            sampled++;
            Vector3 eye=hit.position+Vector3.up*1.6f;Vector3 direction=aim-eye;
            if(direction.magnitude>hero.InteractController.RaycastRange||!Physics.Raycast(eye,direction.normalized,out var visible,direction.magnitude+.05f,hero.InteractController.CullLayers,QueryTriggerInteraction.Collide)||visible.collider!=collider)continue;
            seen++;
            var path=new NavMeshPath();if(!NavMesh.CalculatePath(source.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
            complete++;
            float length=0;for(int n=1;n<path.corners.Length;n++)length+=Vector3.Distance(path.corners[n-1],path.corners[n]);
            if(length<bestLength){bestLength=length;best=path;}
        }
        approaches[target.name]="sampled="+sampled+", visible="+seen+", complete walking paths="+complete;
        if(best==null)return false;destination=target;route=best.corners;corner=0;return true;
    }
    static void Finish(string result)
    {
        phase=99;report.Add("Log: "+DesignSession.CurrentFile);if(!string.IsNullOrEmpty(errors))report.Add("Errors: "+errors);
        if(!string.IsNullOrEmpty(errors)&&result=="PASS")result="FAIL: gameplay errors";
        File.WriteAllText("Logs/FacilityIterationCheck.txt",result+"\n"+string.Join("\n",report));navInstance.Remove();if(navData)UnityEngine.Object.Destroy(navData);EditorApplication.isPlaying=false;
        FacilityIterationSetup.ExportLogs();
    }
}
