using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Blockout.Discovery;
[InitializeOnLoad]
public static class GoblinTrialCheck {
 const string Key="GoblinTrialCheck";static double start;static Vector3 initial;static bool moved,wall,ceiling;static float maxTravel;static Quaternion clockStart;static FacilityClock clock;static readonly System.Collections.Generic.List<string> errors=new System.Collections.Generic.List<string>();static GoblinSurfacePatrol goblin;
 static GoblinTrialCheck(){Application.logMessageReceived+=(m,t,type)=>{if(SessionState.GetBool(Key,false)&&(type==LogType.Error||type==LogType.Exception)&&!m.StartsWith("Layout update failed"))errors.Add(m);};EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(!SessionState.GetBool(Key,false))return;if(s==PlayModeStateChange.EnteredPlayMode){start=EditorApplication.timeSinceStartup;moved=wall=ceiling=false;goblin=null;maxTravel=0;errors.Clear();clock=null;}if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);var prev=SessionState.GetString(Key+".prev","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(prev)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(prev);}};}
 [MenuItem("Tools/Dead Air/Test Goblin Floor Wall Ceiling")]
 static void Begin(){
 if(EditorApplication.isPlaying)throw new Exception("Exit play first.");
 for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save scene edits before test.");
 SessionState.SetString(Key+".prev",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/DeadAir/PlaytestHub/Scenes/Security Terminal Trial.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
 }
 static void Tick(){
 if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||start==0)return;
 try{
 if(!goblin){goblin=UnityEngine.Object.FindAnyObjectByType<GoblinSurfacePatrol>();if(goblin){initial=goblin.transform.position;clock=UnityEngine.Object.FindAnyObjectByType<FacilityClock>();if(clock)clockStart=clock.SecondHand.localRotation;}}
 if(goblin){maxTravel=Mathf.Max(maxTravel,Vector3.Distance(initial,goblin.transform.position));moved|=maxTravel>.5f;wall|=Mathf.Abs(goblin.transform.up.y)<.3f;ceiling|=goblin.transform.up.y<-.75f;
 if(moved&&wall&&ceiling){if(clock&&Quaternion.Angle(clockStart,clock.SecondHand.localRotation)<1)throw new Exception("Clock hand not moving");if(errors.Count>0)throw new Exception(string.Join("\n",errors));Finish("PASS: larger goblin traversed floor, wall and ceiling by the selected crates in Security Terminal Trial. Working clock hands rotated. No runtime errors.");return;}}
 if(EditorApplication.timeSinceStartup-start>45)Finish("FAIL: moved="+moved+" wall="+wall+" ceiling="+ceiling+" travelled="+maxTravel+" position="+(goblin?goblin.transform.position.ToString():"missing")+" up="+(goblin?goblin.transform.up.ToString():"missing"));
 }catch(Exception e){Finish("FAIL: "+e);}
 }
 static void Finish(string s){start=0;File.WriteAllText(Application.dataPath+"/../Logs/GoblinTrialCheck.txt",s);EditorApplication.isPlaying=false;}
}

