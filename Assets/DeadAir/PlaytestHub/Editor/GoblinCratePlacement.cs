using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Blockout.Discovery;
public static class GoblinCratePlacement {
 [MenuItem("Tools/Dead Air/Place Larger Goblin Near Selected Crates")]
 static void Place(){
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.name!="Security Terminal Trial")throw new Exception("Open Security Terminal Trial in edit mode.");
 if(scene.GetRootGameObjects().Any(g=>g.name=="GOBLIN - Crate Area"))throw new Exception("Crate goblin already placed.");
 var crates=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>()).Single(t=>t.name=="Debris - Scattered Crates");
 Physics.SyncTransforms();var route=FacilityIterationApply.Route(crates.position,.85f);if(route==null)throw new Exception("No route with room for 1.6m goblin found by crates.");
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DeadAir/PlaytestHub/Prefabs/Goblin Surface Enemy - 1.6m.prefab");
 EditorSceneManager.SaveScene(scene,Application.dataPath+"/../Logs/Security-BeforeCrateGoblin.unity",true);
 var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);root.name="GOBLIN - Crate Area";root.transform.SetPositionAndRotation(route[0].Position,Quaternion.identity);
 var patrol=root.GetComponent<GoblinSurfacePatrol>();patrol.Route=route.ToArray();patrol.CollisionRadius=.8f;patrol.ScareSound=null;
 EditorUtility.SetDirty(patrol);PrefabUtility.RecordPrefabInstancePropertyModifications(patrol);Undo.RegisterCreatedObjectUndo(root,"Place larger goblin by crates");
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 File.WriteAllLines(Application.dataPath+"/../Logs/GoblinCratePlacement.txt",new[]{"Placed 1.6m goblin near "+crates.name+" at "+crates.position,".85m swept route clearance passed."}.Concat(route.Select(p=>p.Position+" normal "+p.Normal)));
 }
}

