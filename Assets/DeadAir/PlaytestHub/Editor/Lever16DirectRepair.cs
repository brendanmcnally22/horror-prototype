using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Blockout.Discovery;
public static class Lever16DirectRepair{
 [MenuItem("Tools/Dead Air/Repair Lever16 On Its Interaction Root")]
 public static void Apply(){
 if(EditorApplication.isPlaying)throw new Exception("Exit Play mode.");
 var previous=SceneManager.GetActiveScene();string path="Assets/DeadAir/PlaytestHub/Scenes/Security Terminal Trial.unity";
 var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
 try{
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 var root=all.Single(t=>t.name=="Lever_16");var source=all.Single(t=>t.name=="Lever_7"&&t.GetComponent<PrototypeLever>()).GetComponent<PrototypeLever>();
 var visuals=root.Find("Working lever mechanism");if(!visuals)throw new Exception("Lever16 working visual missing.");
 EditorSceneManager.SaveScene(scene,Application.dataPath+"/../Logs/Security-BeforeLever16DirectRepair-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 // Keep one controller on the same object the kit raycast hits.
 var old=visuals.GetComponent<PrototypeLever>();
 var lever=root.GetComponent<PrototypeLever>();if(!lever)lever=Undo.AddComponent<PrototypeLever>(root.gameObject);
 Undo.RecordObject(lever,"Repair direct lever controller");
 EditorUtility.CopySerialized(source,lever);lever.enabled=true;lever.Handle=old&&old.Handle?old.Handle:visuals;
 // Preserve the explicit shared door connection from Lever_7.
 lever.Targets=source.Targets.ToArray();lever.IsOn=false;
 var collider=root.GetComponent<BoxCollider>();if(!collider)collider=Undo.AddComponent<BoxCollider>(root.gameObject);
 Undo.RecordObject(collider,"Repair interaction hitbox");
 var renderers=visuals.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
 if(renderers.Length==0)throw new Exception("No active lever geometry.");
 var corners=new System.Collections.Generic.List<Vector3>();foreach(var r in renderers){var b=r.bounds;foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})corners.Add(root.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));}
 var min=corners.Aggregate(Vector3.Min);var max=corners.Aggregate(Vector3.Max);collider.center=(min+max)*.5f;collider.size=max-min+Vector3.one*.2f;collider.isTrigger=true;collider.enabled=true;
 Undo.RecordObject(root.gameObject,"Make lever interactable");root.gameObject.layer=6;
 foreach(var c in visuals.GetComponentsInChildren<Collider>(true)){Undo.RecordObject(c,"Single lever hitbox");c.enabled=false;EditorUtility.SetDirty(c);}
 if(old){Undo.RecordObject(old,"Single lever controller");old.enabled=false;}
 foreach(var status in visuals.GetComponentsInChildren<DeadAir.Playtesting.LeverStatusLights>(true)){Undo.RecordObject(status,"Use direct lever state");status.Lever=lever;EditorUtility.SetDirty(status);}
 foreach(var c in new Component[]{lever,collider,root}){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
 PrefabUtility.RecordPrefabInstancePropertyModifications(root.gameObject);
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 File.WriteAllText(Application.dataPath+"/../Logs/Lever16DirectRepair.txt","Saved: Lever16 controller and hitbox are on the same Interact-layer root; child controller/hitboxes disabled; geometry-sized hitbox; Lever7 door links and behavior retained. Targets: "+string.Join(", ",lever.Targets.Where(m=>m).Select(m=>m.name))+".");
 }finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
 }
}
