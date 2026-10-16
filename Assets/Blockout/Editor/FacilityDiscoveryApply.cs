using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Blockout.Discovery;
using UHFPS.Runtime;

public static partial class FacilityDiscoverySetup
{
    sealed class LayoutState
    {
        public Transform Parent; public int Sibling; public Vector3 Position,Scale; public Quaternion Rotation; public bool Active;
        public LayoutState(Transform t){Parent=t.parent;Sibling=t.GetSiblingIndex();Position=t.localPosition;Rotation=t.localRotation;Scale=t.localScale;Active=t.gameObject.activeSelf;}
        public bool Same(Transform t,bool leaf)=>Parent==t.parent&&Sibling==t.GetSiblingIndex()&&Rotation==t.localRotation&&Active==t.gameObject.activeSelf&&(leaf||(Position==t.localPosition&&Scale==t.localScale));
    }
    static T Single<T>(Transform[] all,string name)where T:Component
    {
        var found=all.Where(t=>t.name==name).Select(t=>t.GetComponent<T>()).Where(c=>c).ToArray();
        if(found.Length!=1)throw new Exception("Expected one "+name+"; found "+found.Length+". Stopped before editing.");return found[0];
    }
    static bool World(Light l)=>!l.GetComponentInParent<PlayerManager>()&&!l.GetComponentInParent<PlayerPresenceManager>()&&!Path(l.transform).Contains("GAMEMANAGER");
    [MenuItem("Tools/Blockout/Apply Facility Wiring Without Moving Layout")]
    public static void Apply()
    {
        var scene=Scene();
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var lever=Single<PrototypeLever>(all,"LEVER1");
        var storageLever=Single<PrototypeLever>(all,"STORAGE LEVER");
        var platform=Single<PrototypeMover>(all,"STORAGE PLATFORM");
        var doors=all.Where(t=>t.name=="BOTH OF THESE ACTIVATE BY LEVEL 1").Select(t=>t.GetComponent<PrototypeMover>()).Where(c=>c).ToArray();
        if(doors.Length!=2)throw new Exception("Expected the two marked security doors; no edits made.");
        foreach(var door in doors)if(!door.MovingPart || !door.transform.Find("Left armored jamb") || !door.transform.Find("Right armored jamb") || !door.transform.Find("Overhead drive rail"))throw new Exception("Door frame references missing; no edits made.");
        var display=all.Select(t=>t.GetComponent<DiscoveryObjectiveDisplay>()).FirstOrDefault(c=>c);
        var loop=all.Select(t=>t.GetComponent<DiscoveryLoop>()).FirstOrDefault(c=>c);
        if(!display||!loop)throw new Exception("Existing objective display/loop required; no layout was changed.");
        Directory.CreateDirectory("Logs/FacilityDiscoverySafety");
        string stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss");
        EditorSceneManager.SaveScene(scene,"Logs/FacilityDiscoverySafety/Before-"+stamp+".unity",true);
        var before=all.ToDictionary(t=>t,new Func<Transform,LayoutState>(t=>new LayoutState(t)));
        File.WriteAllText("Logs/FacilityDiscoverySafety/Layout-"+stamp+".json",JsonConvert.SerializeObject(all.Select(t=>new{path=Path(t),id=GlobalObjectId.GetGlobalObjectIdSlow(t).ToString(),parent=before[t].Parent ? Path(before[t].Parent) : null,sibling=before[t].Sibling,position=Vec(before[t].Position),scale=Vec(before[t].Scale),rotation=new[]{before[t].Rotation.x,before[t].Rotation.y,before[t].Rotation.z,before[t].Rotation.w},active=before[t].Active}),Formatting.Indented));
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Facility settings and wiring; preserve layout");
        foreach(var root in scene.GetRootGameObjects())Undo.RegisterFullObjectHierarchyUndo(root,"Facility wiring");
        try
        {
            CreateAssets();
            lever.Targets=doors;lever.FollowTarget=true;lever.Label="Security access — open both doors";
            storageLever.Targets=new[]{platform};storageLever.FollowTarget=true;storageLever.Label="Storage platform — move / return";
            platform.Automatic=false;
            foreach(var door in doors)
            {
                // Fit only the moving leaf. Root, frame, pocket and all surrounding geometry stay unchanged.
                var left=door.transform.Find("Left armored jamb");var right=door.transform.Find("Right armored jamb");var header=door.transform.Find("Overhead drive rail");
                float min=left.localPosition.x+left.localScale.x/2,max=right.localPosition.x-right.localScale.x/2;
                float bottom=Mathf.Min(left.localPosition.y-left.localScale.y/2,right.localPosition.y-right.localScale.y/2);
                float top=header.localPosition.y-header.localScale.y/2;
                door.MovingPart.localPosition=new Vector3((min+max)/2,bottom+.01f,door.MovingPart.localPosition.z);
                door.MovingPart.localScale=new Vector3((max-min-.04f)/3f,(top-bottom-.02f)/3.2f,door.MovingPart.localScale.z);
                door.Travel=new Vector3(max-min+.3f,0,0);door.Speed=.55f;door.Acceleration=.35f;door.Automatic=false;
            }
            var sequence=lever.GetComponent<FacilityPowerSequence>();if(!sequence)sequence=Undo.AddComponent<FacilityPowerSequence>(lever.gameObject);
            sequence.SecurityDoors=doors;sequence.ObjectiveDisplay=display;display.Loop=loop;
            bool linked=false;for(int i=0;i<lever.OnChanged.GetPersistentEventCount();i++)if(lever.OnChanged.GetPersistentTarget(i)==sequence)linked=true;
            if(!linked)UnityEventTools.AddPersistentListener(lever.OnChanged,sequence.OnLeverChanged);
            foreach(var trigger in all.Select(t=>t.GetComponent<DiscoveryObjectiveTrigger>()).Where(c=>c&&c.ShowFuseProgress))trigger.RequiredPowerCut=sequence;
            var lights=all.Select(t=>t.GetComponent<Light>()).Where(l=>l&&World(l)).ToArray();
            foreach(var light in lights)
            {
                light.shadows=LightShadows.None;
                if(light.type==LightType.Directional){light.color=new Color(.48f,.60f,.85f);light.intensity=.08f;}
                else if(!light.GetComponentInParent<FuseboxPuzzle>()) light.range=Mathf.Min(light.range,8);
                var flicker=light.GetComponent<PrototypeLight>();if(flicker){flicker.Flicker=false;flicker.Range=Mathf.Min(flicker.Range,8);}
            }
            // Emergency point sources attach to existing lever objects; no objects or transforms are added to the layout.
            var emergency=new List<Light>();
            foreach(var control in new[]{lever,storageLever})
            {
                var light=control.GetComponent<Light>();if(!light)light=Undo.AddComponent<Light>(control.gameObject);
                light.type=LightType.Point;light.color=new Color(1,.045f,.02f);light.intensity=1.6f;light.range=6;light.shadows=LightShadows.None;light.enabled=false;emergency.Add(light);
            }
            sequence.EmergencyLights=emergency.ToArray();
            sequence.NormalLights=lights.Where(l=>l.type!=LightType.Directional&&!emergency.Contains(l)&&!l.GetComponentInParent<FuseboxPuzzle>()&&!l.GetComponent<PrototypeLight>()).ToArray();
            sequence.NormalPrototypeLights=lights.Select(l=>l.GetComponent<PrototypeLight>()).Where(c=>c&&c.Color.g>.4f&&c.Color.b>.4f).ToArray();
            string skyPath=AssetsRoot+"/Materials/Discovery Night Sky.mat";
            if(!AssetDatabase.LoadAssetAtPath<Material>(skyPath))AssetDatabase.CopyAsset("Assets/Blockout/Facility/Materials/Facility Night Sky.mat",skyPath);
            RenderSettings.skybox=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.13f,.15f,.19f);RenderSettings.reflectionIntensity=.15f;RenderSettings.fog=false;
            loop.RequiredFuses=3;loop.UsePackInventory=true;
            bool restoreLinked=false;for(int i=0;i<loop.OnCompleted.GetPersistentEventCount();i++)if(loop.OnCompleted.GetPersistentTarget(i)==sequence)restoreLinked=true;
            if(!restoreLinked)UnityEventTools.AddPersistentListener(loop.OnCompleted,sequence.RestorePower);
            bool localLinked=false;for(int i=0;i<loop.OnFirstReturned.GetPersistentEventCount();i++)if(loop.OnFirstReturned.GetPersistentTarget(i)==sequence)localLinked=true;
            if(!localLinked)UnityEventTools.AddPersistentListener(loop.OnFirstReturned,sequence.RestoreLocalPower);
            var fusebox=all.Select(t=>t.GetComponent<FuseboxPuzzle>()).FirstOrDefault(c=>c);
            if(fusebox)
            {
                // Four physical sockets remain untouched; one is preinstalled, leaving three to find.
                var source=PrefabUtility.GetCorrespondingObjectFromOriginalSource(fusebox);
                var instanceRoot=PrefabUtility.GetOutermostPrefabInstanceRoot(fusebox.gameObject).transform;
                if(!source || source.Fuses.Count!=4)throw new Exception("Cannot resolve the original pack fusebox socket references.");
                var sockets=new List<FuseboxPuzzle.FuseElement>();
                for(int i=0;i<source.Fuses.Count;i++)
                {
                    var original=source.Fuses[i];
                    var fuseTransform=instanceRoot.Find(AnimationUtility.CalculateTransformPath(original.FuseObject.transform,source.transform.root));
                    var lightTransform=instanceRoot.Find(AnimationUtility.CalculateTransformPath(original.FuseLight.transform,source.transform.root));
                    var rendererTransform=instanceRoot.Find(AnimationUtility.CalculateTransformPath(original.LightRenderer.transform,source.transform.root));
                    if(!fuseTransform||!lightTransform||!rendererTransform)throw new Exception("Existing fusebox socket geometry could not be resolved. Layout was not changed.");
                    sockets.Add(new FuseboxPuzzle.FuseElement{FuseObject=fuseTransform.gameObject,FuseLight=lightTransform.GetComponent<Light>(),LightRenderer=rendererTransform.GetComponent<MeshRenderer>(),IsInserted=i==3});
                }
                fusebox.Fuses=sockets;
                bool connected=false;for(int i=0;i<fusebox.OnFuseConnected.GetPersistentEventCount();i++)if(fusebox.OnFuseConnected.GetPersistentTarget(i)==loop)connected=true;
                if(!connected)UnityEventTools.AddPersistentListener(fusebox.OnFuseConnected,loop.RecordPackFuse);
            }
            var after=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            if(after.Length!=all.Length || after.Any(t=>!before.ContainsKey(t)))throw new Exception("Layout object count changed. Rolling back scene edits.");
            foreach(var t in all)if(!before[t].Same(t,doors.Any(d=>d.MovingPart==t)))throw new Exception("Protected layout changed at "+Path(t)+". Rolling back scene edits.");
            foreach(var component in all.SelectMany(t=>t.GetComponents<Component>()).Where(c=>c))
            {
                EditorUtility.SetDirty(component);
                if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/FacilityDiscoveryResult.txt","PASS: "+all.Length+" existing transforms preserved, except authorized sizing of the two moving door leaves. No scene objects added, removed or reparented. Root/frame transforms unchanged.\nLEVER1 -> both security doors; wait for both fully open -> emergency power + fuse objective (once). STORAGE LEVER -> STORAGE PLATFORM.\nNight sky and low-cost lighting configured. Four steady-light and three observation-window prefabs saved without placing them.\nExisting pack fusebox: fourth fuse preinstalled; three collected fuses advance objective; third return restores power.\n");
            Undo.CollapseUndoOperations(undo);
        }
        catch{Undo.RevertAllDownToGroup(undo);throw;}
    }
}

