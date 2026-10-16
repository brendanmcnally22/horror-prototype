using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Blockout.Discovery;
using UHFPS.Runtime;

[InitializeOnLoad]
public static class FacilityDiscoveryCheck
{
    const string Key="FacilityDiscoveryCheck.Running";
    static double start,stepTime;
    static int phase;
    static FacilityPowerSequence power;
    static PrototypeMover storage;
    static Vector3 storageStart;
    static FacilityLamp normal,emergency;
    static readonly List<string> errors=new List<string>();
    static FacilityDiscoveryCheck()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>{
            if(!SessionState.GetBool(Key,false))return;
            if(s==PlayModeStateChange.EnteredPlayMode){start=EditorApplication.timeSinceStartup;phase=0;errors.Clear();Application.runInBackground=true;}
            if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);string previous=SessionState.GetString(Key+".Previous","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(previous)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);}
        };
        Application.logMessageReceived+=(message,trace,type)=>{if(SessionState.GetBool(Key,false)&&(type==LogType.Error||type==LogType.Exception)&&!message.StartsWith("Layout update failed to stabilize"))errors.Add(message);};
    }
    static T Spawn<T>(string name)where T:Component
    {
        var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blockout/Discovery/Prefabs/"+name+".prefab"));go.transform.position=new Vector3(0,-100,0);return go.GetComponent<T>();
    }
    static void Require(bool result,string message){if(!result)throw new Exception(message);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false))
        {
            if(!File.Exists("Logs/FacilityDiscoveryCheck.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete("Logs/FacilityDiscoveryCheck.request");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty){File.WriteAllText("Logs/FacilityDiscoveryCheck.txt","SKIPPED: unsaved scene; no changes discarded.");return;}
            SessionState.SetString(Key+".Previous",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(DiscoverySetup.ScenePath);
            SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;return;
        }
        if(!EditorApplication.isPlaying||start==0||phase==9)return;
        try
        {
            if(EditorApplication.timeSinceStartup-start>50)throw new Exception("Play check timed out.");
            if(phase==0)
            {
                if(EditorApplication.timeSinceStartup-start<8)return;
                power=UnityEngine.Object.FindAnyObjectByType<FacilityPowerSequence>();Require(power,"Power sequence missing.");
                Require(!power.PowerCut&&power.SecurityDoors.Length==2,"Initial sequence state incorrect.");
                Require(power.GetComponent<PrototypeLever>().Targets.SequenceEqual(power.SecurityDoors),"LEVER1 targets incorrect.");
                Require(!power.ObjectiveDisplay.Text.gameObject.activeSelf,"Fuse objective appeared before blackout.");
                storage=UnityEngine.Object.FindObjectsByType<PrototypeMover>(FindObjectsSortMode.None).Single(x=>x.name=="STORAGE PLATFORM");storageStart=storage.MovingPart.position;
                normal=Spawn<FacilityLamp>("Facility Light - White Steady");emergency=Spawn<FacilityLamp>("Facility Light - Green Steady");
                stepTime=EditorApplication.timeSinceStartup;phase=1;return;
            }
            if(phase==1)
            {
                if(EditorApplication.timeSinceStartup-stepTime<1)return;
                Require(normal.Sources.All(x=>x.enabled&&x.shadows==LightShadows.None)&&emergency.Sources.All(x=>x.enabled&&x.shadows==LightShadows.None),"Steady-light configuration failed.");
                power.GetComponent<PrototypeLever>().InteractStart();
                var lever=UnityEngine.Object.FindObjectsByType<PrototypeLever>(FindObjectsSortMode.None).Single(x=>x.name=="STORAGE LEVER");
                Require(lever.Targets.Length==1&&lever.Targets[0]==storage,"Storage target incorrect.");lever.InteractStart();
                Require(!power.PowerCut,"Power cut before doors finished opening.");phase=2;return;
            }
            if(phase==2)
            {
                if(!power.PowerCut)return;
                Require(power.SecurityDoors.All(x=>x.AtEnd),"Power cut before both doors fully opened.");
                Require(Vector3.Distance(storage.MovingPart.position,storageStart)>1,"Storage platform did not move.");
                Require(normal.Sources.All(x=>!x.enabled)&&emergency.Sources.All(x=>x.enabled),"Normal/emergency circuits failed.");
                Require(power.EmergencyLights.All(x=>x.enabled)&&power.ObjectiveDisplay.Text.gameObject.activeSelf,"Emergency lamps or objective failed.");
                var loop=power.ObjectiveDisplay.Loop;var box=UnityEngine.Object.FindAnyObjectByType<FuseboxPuzzle>();
                Require(box&&box.Fuses.Count(x=>x.IsInserted)==1,"Expected one preinstalled fuse.");
                box.UseInteract=true;
                var first=Spawn<InteractableItem>("Fuse - Optional Pickup");first.InteractWithObject();box.InteractStart();
                Require(loop.Returned==1&&power.EmergencyLights[0].color.g>.8f&&!power.PowerRestored,"First fuse did not restore local light only.");
                var second=Spawn<InteractableItem>("Fuse - Optional Pickup");second.InteractWithObject();box.InteractStart();
                Require(loop.Returned==2&&!power.PowerRestored,"Power restored before third fuse.");
                var third=Spawn<InteractableItem>("Fuse - Optional Pickup");third.InteractWithObject();box.InteractStart();
                Require(loop.Returned==3&&power.PowerRestored&&normal.Sources.All(x=>x.enabled),"Third fuse did not restore facility power.");
                power.ObjectiveDisplay.Refresh();Require(power.ObjectiveDisplay.Text.text.Contains("POWER RESTORED"),"Completion objective failed.");
                power.GetComponent<PrototypeLever>().InteractStart();Require(power.PowerRestored,"Lever replay reset the power sequence.");
                Require(errors.Count==0,string.Join("\n",errors));
                Finish("PASS: LEVER1 opens both fitted sliding doors; blackout waits for both arrivals; storage lever moves storage platform; steady lights have no shadows; normal lights go off and emergency lights remain on; objective appears at blackout; native fusebox accepts three pack fuses (one preinstalled); first restores local light, third restores full power; sequence does not reset on lever replay. Gameplay errors: 0. Known Unity toolbar layout errors excluded.\n");
            }
        }
        catch(Exception e){Finish("FAIL: "+e+"\n"+string.Join("\n",errors));}
    }
    static void Finish(string message){phase=9;File.WriteAllText("Logs/FacilityDiscoveryCheck.txt",message);EditorApplication.isPlaying=false;}
}
