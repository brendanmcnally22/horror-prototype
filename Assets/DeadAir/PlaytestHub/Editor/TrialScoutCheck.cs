using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Blockout.Discovery;
using DeadAir.Playtesting;
using UnityEngine.SceneManagement;
[InitializeOnLoad]
public static class TrialScoutCheck {
 const string Key="TrialScoutCheck";static double start;static int phase;static int initialScore;static double shotTime;static Vector3 first;static SecurityTrialTerminal terminal;static GameObject scout;
 static TrialScoutCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(!SessionState.GetBool(Key,false))return;if(s==PlayModeStateChange.EnteredPlayMode){start=EditorApplication.timeSinceStartup;phase=0;}if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);var prev=SessionState.GetString(Key+".previous","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(prev)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(prev);}};}
 [MenuItem("Tools/Dead Air/Security/Test camera pan and scout")]
 static void Begin(){if(EditorApplication.isPlaying||SceneManager.GetActiveScene().name!="Security Terminal Trial")throw new Exception("Open saved Security Terminal Trial with no play mode override.");var scene=SceneManager.GetActiveScene();if(scene.isDirty&&!EditorSceneManager.SaveScene(scene,Application.dataPath+"/../Logs/TrialBeforeScoutCheck.unity",true))throw new Exception("Could not snapshot current scene.");SessionState.SetString(Key+".previous",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));EditorSceneManager.playModeStartScene=null;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 static void Tick(){if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||start==0||phase==99)return;
 try{double t=EditorApplication.timeSinceStartup-start;if(t>95)throw new Exception("Timeout phase "+phase);
 if(phase==0&&t>4){terminal=UnityEngine.Object.FindAnyObjectByType<SecurityTrialTerminal>();terminal.GetComponent<VirgoTerminal>().Open();terminal.OpenDesktop();var c=terminal.WatchedCamera;var op=terminal.GetComponent<TrialCameraOperator>();var home=c.transform.localRotation;var pos=c.transform.position;op.Aim(c,new Vector2(23,12));if(Quaternion.Angle(home,c.transform.localRotation)<20||pos!=c.transform.position)throw new Exception("pan failed");op.Aim(c,Vector2.zero,true);if(Quaternion.Angle(home,c.transform.localRotation)>.01f)throw new Exception("center failed");phase=1;}
 if(phase==1){
    var op=terminal.GetComponent<TrialCameraOperator>();var hs=terminal.GetComponents<TrialCameraHider>();
    var cam=terminal.WatchedCamera;
    if(hs.Length!=3)throw new Exception("Missing scout controllers");
    foreach(var h in hs)if(h.SpotDistance(cam)<2){initialScore=op.Spots;op.Spot();if(op.Spots!=initialScore+1)throw new Exception("Visible spot rejected");if(h.SpotDistance(cam)<2)throw new Exception("Duplicate sighting eligible");phase=2;break;}
    if(phase==1)terminal.NextFeed();
 }
 if(phase==2){
    var h=terminal.GetComponents<TrialCameraHider>();int moving=0;foreach(var actor in h)if(actor.Feet!=Vector3.zero)moving++;
    if(moving<3){if(t>20)throw new Exception("Only "+moving+" scouts placed");return;}
    var cam=terminal.WatchedCamera;terminal.NextFeed();if(cam==terminal.WatchedCamera)throw new Exception("Feed cycle failed");
    terminal.GetComponent<VirgoTerminal>().OpenApplication(4);shotTime=t;phase=3;
 }
 if(phase==3&&t-shotTime>1){ScreenCapture.CaptureScreenshot(Application.dataPath+"/../Logs/TrialSpotMonitor.png");phase=4;}
 if(phase==4&&t>70){
    if(!terminal.IsOpen)throw new Exception("Terminal no longer open after 60 seconds");
    Finish("PASS: three placed scouts; live camera cycling, pan and recenter; visible spot counted, duplicate sighting ineligible; security still open after 60 seconds.");
 }}
 catch(Exception e){Finish("FAIL "+e);}}
 static void Finish(string msg){phase=99;File.WriteAllText(Application.dataPath+"/../Logs/TrialScoutCheck.txt",msg);EditorApplication.isPlaying=false;}
}




