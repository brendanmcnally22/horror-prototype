using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class TrialTubeReplacement {
 static Bounds BoundsOf(GameObject go){var rs=go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();if(rs.Length==0)throw new Exception("No tube geometry");var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 [MenuItem("Tools/Dead Air/Replace Trial Tubes With Ephis")]
 static void Apply(){
 if(EditorApplication.isPlaying)throw new Exception("Exit Play mode.");
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DeadAir/Ephis Test Tube.prefab");if(!prefab)throw new Exception("Ephis prefab missing.");
 var previous=SceneManager.GetActiveScene();var reports=new List<string>();
 foreach(string name in new[]{"AI Discovery Trial","Security Terminal Trial"}){
 var path="Assets/DeadAir/PlaytestHub/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
 if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
 try{
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 var originals=all.ToDictionary(t=>t,t=>(t.parent,t.localPosition,t.localRotation,t.localScale,t.gameObject.activeSelf));
 var old=all.Where(t=>t.name.Equals("Test Tube",StringComparison.OrdinalIgnoreCase)||t.name.StartsWith("Test Tube (",StringComparison.OrdinalIgnoreCase)).ToArray();
 EditorSceneManager.SaveScene(scene,Application.dataPath+"/../Logs/"+name+"-BeforeEphis-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 foreach(var t in old){
 if(t.GetComponentsInChildren<MonoBehaviour>(true).Any(m=>m))throw new Exception("Tube has gameplay scripts; stopped before replacement: "+t.name);
 var bounds=BoundsOf(t.gameObject);var replacement=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
 Undo.RegisterCreatedObjectUndo(replacement,"Replace tube with Ephis");
 replacement.transform.SetParent(t.parent,false);replacement.transform.SetPositionAndRotation(t.position,t.rotation);replacement.transform.localScale=prefab.transform.localScale;
 var current=BoundsOf(replacement);if(current.size.y<.001f)throw new Exception("Invalid Ephis bounds.");
 replacement.transform.localScale*=bounds.size.y/current.size.y;
 current=BoundsOf(replacement);
 replacement.transform.position+=new Vector3(bounds.center.x-current.center.x,bounds.min.y-current.min.y,bounds.center.z-current.center.z);
 replacement.name="Ephis Test Tube";
 replacement.transform.SetSiblingIndex(t.GetSiblingIndex());
 // Retain the old object as an inactive backup so any existing object references remain valid.
 Undo.RecordObject(t.gameObject,"Retain replaced tube backup");t.gameObject.SetActive(false);t.name="REPLACED - Test Tube (inactive backup)";
 reports.Add(name+": replaced old tube at "+t.position+"; Ephis fitted to original height and floor contact.");
 }
 foreach(var pair in originals){var t=pair.Key;var b=pair.Value;if(old.Any(o=>t==o||t.IsChildOf(o)))continue;
 if(!t||t.parent!=b.Item1||t.localPosition!=b.Item2||t.localRotation!=b.Item3||t.localScale!=b.Item4||t.gameObject.activeSelf!=b.Item5)throw new Exception("Unrelated transform changed.");
 }
 if(old.Length>0){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
 reports.Add(name+": "+old.Length+" replacements; "+all.Count(t=>t.name.StartsWith("Ephis Test Tube"))+" existing Ephis instances preserved; unrelated transforms verified.");
 }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
 }
 if(previous.IsValid())SceneManager.SetActiveScene(previous);AssetDatabase.SaveAssets();File.WriteAllLines(Application.dataPath+"/../Logs/TrialEphisReplacement.txt",reports);
 }
}
