using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class FacilityIterationAudit {
 public static string Log=>Application.dataPath+"/../Logs/FacilityIterationNew.txt";
 [MenuItem("Tools/Dead Air/Remove Misplaced Goblin And Prepare Larger Prefab")]
 public static void RemoveGoblin(){
 if(EditorApplication.isPlaying)throw new Exception("Exit play first.");
 var previous=SceneManager.GetActiveScene();string path="Assets/DeadAir/PlaytestHub/Scenes/AI Discovery Trial.unity";
 var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
 try{
 var goblin=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Blockout.Discovery.GoblinSurfacePatrol>(true)).SingleOrDefault();
 if(goblin){
 EditorSceneManager.SaveScene(scene,Application.dataPath+"/../Logs/AI-BeforeGoblinRemoval.unity",true);
 var model=goblin.Visual;
 var rs=model.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
 model.localScale*=1.6f/Mathf.Max(.01f,b.size.y);b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);model.position+=goblin.transform.position-b.center;
 goblin.Route=new Blockout.Discovery.GoblinSurfacePatrol.SurfacePoint[0];goblin.ScareSound=null;
 string folder="Assets/DeadAir/PlaytestHub/Prefabs";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
 PrefabUtility.SaveAsPrefabAsset(goblin.gameObject,folder+"/Goblin Surface Enemy - 1.6m.prefab");
 Undo.DestroyObjectImmediate(goblin.gameObject);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 File.WriteAllText(Application.dataPath+"/../Logs/GoblinPlacementCorrection.txt","Removed goblin from wrong enemy-slot area. Prepared 1.6m prefab with no route. Correct placement pending user-selected area.");
 }
 }finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
 }
 [MenuItem("Tools/Dead Air/Locate Goblin Crate Area")]
 public static void Locate(){var t=Selection.activeTransform;var scene=SceneManager.GetActiveScene();var candidates=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>()).Where(x=>x.name=="Debris - Scattered Crates");File.WriteAllLines(Application.dataPath+"/../Logs/GoblinCrateArea.txt",new[]{"SELECTED "+(t?t.name+" world="+t.position+" local="+t.localPosition:"none"),"VIEW "+(SceneView.lastActiveSceneView?SceneView.lastActiveSceneView.pivot.ToString():"none")}.Concat(candidates.Select(x=>x.name+" world="+x.position+" local="+x.localPosition+" parent="+x.parent?.name)));}
 [MenuItem("Tools/Dead Air/Calibrate Working Clock")]
 public static void Clock(){
 var scene=SceneManager.GetActiveScene();if(scene.name!="Security Terminal Trial"||EditorApplication.isPlaying)throw new Exception("Open Security Terminal Trial in edit mode.");
 var driver=UnityEngine.Object.FindAnyObjectByType<Blockout.Discovery.FacilityClock>();if(!driver)throw new Exception("Clock driver missing.");
 var face=driver.GetComponentsInChildren<Renderer>().First(r=>r.name=="Numbers").bounds;
 var baseBounds=driver.GetComponentsInChildren<Renderer>().First(r=>r.name=="Clock Base").bounds;
 Vector3 normal=driver.transform.up;if(Vector3.Dot(face.center-baseBounds.center,normal)<0)normal=-normal;
 Vector3 up=Vector3.ProjectOnPlane(Vector3.up,normal).normalized;
 foreach(var hand in new[]{driver.HourHand,driver.MinuteHand,driver.SecondHand}){
 bool second=hand==driver.SecondHand;float length=Mathf.Max(face.size.x,face.size.y)* (hand==driver.HourHand?.29f:second?.47f:.41f);
 hand.SetPositionAndRotation(face.center+normal*.007f,Quaternion.LookRotation(up,normal));
 float scale=hand.lossyScale.x;
 var mesh=hand.GetChild(0);mesh.localPosition=new Vector3(0,0,length/scale*.5f);mesh.localScale=new Vector3((second?.0018f:.004f)/scale,.002f/scale,length/scale);
 }
 driver.Axis=Vector3.up;driver.Direction=-1;EditorUtility.SetDirty(driver);
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 }
 [MenuItem("Tools/Dead Air/Audit Cameras Clock Goblin")]
 public static void Audit(){
 var report=new List<string>();var previous=SceneManager.GetActiveScene();
 foreach(string name in new[]{"Security Terminal Trial","AI Discovery Trial"}){
 string path="Assets/DeadAir/PlaytestHub/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
 try{
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 report.AddRange(all.Where(t=>t.name.Contains("clock",StringComparison.OrdinalIgnoreCase)||t.name.Contains("SecurityCamera")||t.name.Contains("navigation",StringComparison.OrdinalIgnoreCase)||t.name.Contains("route",StringComparison.OrdinalIgnoreCase)||t.name.Contains("enemy",StringComparison.OrdinalIgnoreCase)).Select(t=>name+": "+t.name+" at "+t.position+" scale="+t.lossyScale+" prefab="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)+" children="+string.Join(",",t.GetComponentsInChildren<Transform>(true).Take(25).Select(c=>c.name))+" components="+string.Join(",",t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name))));
 }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
 }
 if(previous.IsValid())SceneManager.SetActiveScene(previous);File.WriteAllLines(Log,report);
 }
}



