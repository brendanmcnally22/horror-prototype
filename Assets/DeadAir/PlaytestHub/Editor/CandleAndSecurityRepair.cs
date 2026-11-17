using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UHFPS.Runtime;
using DeadAir.Playtesting;
public static class CandleAndSecurityRepair{
 [MenuItem("Tools/Dead Air/Brighten Candle And Repair Security Entry")]
 static void Apply(){
 if(EditorApplication.isPlaying)throw new Exception("Exit Play mode.");
 var previous=SceneManager.GetActiveScene();var report=new List<string>();
 foreach(string name in new[]{"AI Discovery Trial","Security Terminal Trial"}){
 var path="Assets/DeadAir/PlaytestHub/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
 try{
 EditorSceneManager.SaveScene(scene,Application.dataPath+"/../Logs/"+name+"-BeforeCandleSecurityRepair-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 if(name.StartsWith("AI")){
 foreach(var candle in all.Select(t=>t.GetComponent<CandleItem>()).Where(c=>c)){
 Undo.RecordObject(candle,"Readable candle lighting");candle.FlameLightIntensity=7;candle.NormalLightMultiplier=1;candle.FocusLightMultiplier=1.65f;candle.FlameFlickerLimits=new UHFPS.Runtime.MinMax(.8f,1.1f);
 var light=candle.FlameLight;Undo.RecordObject(light,"Candle range");light.range=12;light.intensity=7;light.colorTemperature=2600;light.useColorTemperature=true;light.shadowBias=.03f;light.shadowNormalBias=.08f;
 foreach(var c in new Component[]{candle,light}){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
 report.Add("AI candle: intensity 7, 80–110% flicker, 12m range, warmer readable light.");
 }
 }else{
 var terminal=all.Select(t=>t.GetComponent<SecurityTrialTerminal>()).FirstOrDefault(t=>t);
 if(terminal){
 // Dedicated surveillance cameras must never own the game screen.
 foreach(var camera in terminal.CameraFeeds.Where(c=>c)){
 if(camera.GetComponent<AudioListener>()||camera.GetComponent<Unity.Cinemachine.CinemachineBrain>())throw new Exception("Player camera is incorrectly assigned as CCTV.");
 Undo.RecordObject(camera,"CCTV offscreen-only");camera.enabled=false;camera.targetTexture=null;camera.tag="Untagged";EditorUtility.SetDirty(camera);
 if(PrefabUtility.IsPartOfPrefabInstance(camera))PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
 }
 report.Add("Security: "+terminal.CameraFeeds.Length+" surveillance feeds disabled at scene entry, no fullscreen output.");
 }
 foreach(var player in all.Select(t=>t.GetComponent<PlayerManager>()).Where(p=>p)){
 var main=player.MainCamera;if(main){Undo.RecordObject(main,"Restore gameplay output");main.enabled=true;main.targetTexture=null;EditorUtility.SetDirty(main);if(PrefabUtility.IsPartOfPrefabInstance(main))PrefabUtility.RecordPrefabInstancePropertyModifications(main);report.Add("Gameplay output: "+main.name);}
 if(player.MainVirtualCamera){Undo.RecordObject(player.MainVirtualCamera.gameObject,"Enable player view");player.MainVirtualCamera.gameObject.SetActive(true);EditorUtility.SetDirty(player.MainVirtualCamera.gameObject);if(PrefabUtility.IsPartOfPrefabInstance(player.MainVirtualCamera.gameObject))PrefabUtility.RecordPrefabInstancePropertyModifications(player.MainVirtualCamera.gameObject);}
 }
 }
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
 }if(previous.IsValid())SceneManager.SetActiveScene(previous);AssetDatabase.SaveAssets();File.WriteAllLines(Application.dataPath+"/../Logs/CandleSecurityRepair.txt",report);
 }
}

