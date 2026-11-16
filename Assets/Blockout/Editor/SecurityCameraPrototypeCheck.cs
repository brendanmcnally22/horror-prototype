using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UHFPS.Runtime;
using Blockout.Discovery;
[InitializeOnLoad]
public static class SecurityCameraPrototypeCheck
{
    const string Key="SecurityCameraPrototypeCheck";
    static double start,step;static int phase;
    static SecurityCameraPanel panel;static SecurityPanelDoor free,locked;
    static readonly List<string> report=new List<string>(),errors=new List<string>();
    static SecurityCameraPrototypeCheck()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>
        {
            if(!SessionState.GetBool(Key,false))return;
            if(s==PlayModeStateChange.EnteredPlayMode){phase=0;start=EditorApplication.timeSinceStartup;report.Clear();errors.Clear();Application.runInBackground=true;}
            if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);string previous=SessionState.GetString(Key+".previous","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(previous)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);}
        };
        Application.logMessageReceived+=(m,t,type)=>{if(SessionState.GetBool(Key,false)&&(type==LogType.Exception||type==LogType.Error)&&!m.StartsWith("Layout update failed"))errors.Add(m);};
    }
    [MenuItem("Tools/Dead Air/Test Security Camera Prototype")]
    static void Begin()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save existing scene edits before this check. No edits discarded.");
        SessionState.SetString(Key+".previous",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SecurityCameraPrototypeSetup.ScenePath);
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void Require(bool b,string message){if(!b)throw new Exception(message);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||start==0||phase==99)return;
        try
        {
            double now=EditorApplication.timeSinceStartup;if(now-start>50)throw new Exception("Security test timed out at "+phase);
            if(phase==0)
            {
                if(now-start<6)return;
                panel=UnityEngine.Object.FindAnyObjectByType<SecurityCameraPanel>();Require(panel&&panel.Open(),"Panel did not open.");
                free=panel.Channels[0].Door;locked=panel.Channels[1].Door;
                Require(panel.Feed&&panel.Channels[0].Camera.LiveCamera.enabled&&!panel.Channels[1].Camera.LiveCamera.enabled,"Initial feed routing incorrect.");
                Require(panel.OperateDoor(true),"Standard door rejected opening.");
                panel.SelectChannel(1);Require(!panel.OperateDoor(true)&&!locked.Mover.TargetEnd,"Restricted door bypassed keycard check.");
                Require(!panel.Channels[0].Camera.LiveCamera.enabled&&panel.Channels[1].Camera.LiveCamera.enabled,"Camera switching failed.");
                report.Add("PASS: live feeds switch; one camera renders at a time; standard door opens without a card; restricted door denies access.");
                panel.Close();Require(GameManager.Instance.PlayerPresence.IsUnlockedAndCamera,"Leaving panel failed to restore player.");
                var pickup=UnityEngine.Object.FindObjectsByType<InteractableItem>(FindObjectsSortMode.None).Single(x=>x.name.StartsWith("GREEN SECURITY KEYCARD"));pickup.InteractWithObject();
                Require(locked.HasAccess,"Native green keycard pickup did not grant access.");
                Require(panel.Open(),"Panel reopen failed.");panel.SelectChannel(1);Require(panel.OperateDoor(true),"Authorized door rejected opening.");
                report.Add("PASS: actual UHFPS pickup grants card authorization and panel can be exited/reopened.");step=now;phase=1;return;
            }
            if(phase==1)
            {
                if(!locked.Mover.AtEnd||!free.Mover.AtEnd)return;
                Require(Inventory.Instance.GetItemQuantity(locked.RequiredCardId)==1,"Keycard was consumed.");
                ScreenCapture.CaptureScreenshot(Application.dataPath+"/../Logs/SecurityCameraPanelPreview.png");
                step=now;phase=10;return;
            }
            if(phase==10)
            {
                if(now-step<.6)return;
                panel.Close();var player=PlayerManager.Instance;player.PlayerCollider.enabled=false;player.transform.position=locked.transform.TransformPoint(new Vector3(0,.05f,0));player.PlayerCollider.enabled=true;Physics.SyncTransforms();
                Require(!locked.Request(false)&&locked.Occupied,"Occupied doorway accepted close request.");
                player.PlayerCollider.enabled=false;player.transform.position=locked.transform.TransformPoint(new Vector3(0,.05f,-3));player.PlayerCollider.enabled=true;Physics.SyncTransforms();
                Require(locked.Request(false),"Clear doorway rejected closing.");step=now;phase=2;return;
            }
            if(phase==2)
            {
                if(now-step<.5)return;
                var player=PlayerManager.Instance;player.PlayerCollider.enabled=false;player.transform.position=locked.transform.TransformPoint(new Vector3(0,.05f,0));player.PlayerCollider.enabled=true;Physics.SyncTransforms();step=now;phase=3;return;
            }
            if(phase==3)
            {
                if(now-step<.2)return;Require(locked.Mover.TargetEnd,"Closing door did not safety-reopen when player entered.");
                report.Add("PASS: both doors reach open position; keycard retained; occupied close blocked; intrusion during closing reopens door.");
                var player=PlayerManager.Instance;player.PlayerCollider.enabled=false;player.transform.position=new Vector3(0,.1f,-5);player.PlayerCollider.enabled=true;Physics.SyncTransforms();
                Require(locked.Request(false)&&free.Request(false),"Final clear close requests failed.");phase=4;return;
            }
            if(phase==4)
            {
                if(!locked.Mover.AtStart||!free.Mover.AtStart)return;
                report.Add("PASS: both shutters fully close when clear; all panel cameras disabled on exit.");
                Require(panel.Channels.All(c=>!c.Camera.LiveCamera.enabled),"A camera stayed active after exiting.");
                Require(errors.Count==0,string.Join("\n",errors));Finish("PASS");
            }
        }
        catch(Exception e){report.Add(e.ToString());Finish("FAIL");}
    }
    static void Finish(string status){phase=99;if(panel)panel.Close();File.WriteAllText(Application.dataPath+"/../Logs/SecurityCameraPrototypeCheck.txt",status+"\n"+string.Join("\n",report));EditorApplication.isPlaying=false;}
}
