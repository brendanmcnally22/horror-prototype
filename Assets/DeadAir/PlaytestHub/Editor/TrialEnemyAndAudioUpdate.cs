using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Blockout.Discovery;
using DeadAir.Playtesting;

public static class TrialEnemyAndAudioUpdate {
 static AudioClip Clip(string suffix)=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ThunderWire Studio/UHFPS/Content/Sounds/"+suffix);
 [MenuItem("Tools/Dead Air/Apply Security Enemy Objective And UHFPS Audio")]
 static void Apply(){
 if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
 var laugh=Clip("Jumpscares/EvilClownLaugh.wav");var locker=Clip("Interact/Hiding/Locker/Locker_Close.wav");var transit=Clip("Interact/Elevator/Elevator_StartMove.wav");var shriek=Clip("Jumpscares/Jumpscare_Monster.wav");
 var previous=SceneManager.GetActiveScene();var report=new List<string>();
 foreach(string name in new[]{"AI Discovery Trial","Security Terminal Trial","Three Doors Lighting Trial"}){
 string path="Assets/DeadAir/PlaytestHub/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
 try{
 EditorSceneManager.SaveScene(scene,Application.dataPath+"/../Logs/"+name+"-BeforeEnemyAudio-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var dirty=new List<Component>();int fuseCount=0;
 foreach(var item in all.Select(t=>t.GetComponent<UHFPS.Runtime.InteractableItem>()).Where(i=>i)){
 var data=new SerializedObject(item);var guid=data.FindProperty("PickupItem")?.FindPropertyRelative("m_GUID");
 if(guid==null||guid.stringValue!="277b6aee2d4b47aaa655f284bf75bf94")continue;
 var metric=item.GetComponent<DesignPickupMetric>();if(!metric)metric=Undo.AddComponent<DesignPickupMetric>(item.gameObject);
 Undo.RecordObject(metric,"UHFPS clown laugh");metric.PickupLaugh=laugh;dirty.Add(metric);fuseCount++;
 }
 foreach(var effect in all.Select(t=>t.GetComponent<LockerFallScare>()).Where(e=>e)){Undo.RecordObject(effect,"UHFPS locker impact");effect.ImpactSound=locker;dirty.Add(effect);}
 foreach(var effect in all.Select(t=>t.GetComponent<TransitNoiseScare>()).Where(e=>e)){Undo.RecordObject(effect,"Faster transit with UHFPS audio");effect.Sound=transit;effect.RunSpeed=15;dirty.Add(effect);if(effect.TransitMover){Undo.RecordObject(effect.TransitMover,"Faster transit");effect.TransitMover.Speed=15;effect.TransitMover.Acceleration=8;dirty.Add(effect.TransitMover);}}
 foreach(var effect in all.Select(t=>t.GetComponent<FirstFuseAudioScare>()).Where(e=>e)){Undo.RecordObject(effect,"UHFPS shriek");effect.Shriek=shriek;dirty.Add(effect);}
 if(name.StartsWith("Security")){
 foreach(var term in all.Select(t=>t.GetComponent<SecurityTrialTerminal>()).Where(t=>t)){Undo.RecordObject(term,"Enable roaming goblins");term.RoamingEnemyModel=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DeadAir/Goblin.fbx");term.RoamingEnemyCount=6;dirty.Add(term);}
 foreach(var op in all.Select(t=>t.GetComponent<TrialCameraOperator>()).Where(t=>t)){Undo.RecordObject(op,"Enable security enemies");op.EnableScouts=true;dirty.Add(op);}
 }
 foreach(var exp in all.Select(t=>t.GetComponent<PlaytestExperience>()).Where(e=>e)){
 Undo.RecordObject(exp,"Update objective");exp.Objective=name.StartsWith("Security")?"Spot enemies on 3 different cameras":name.StartsWith("Three")?"Choose a door":"Find 3 fuses";dirty.Add(exp);
 }
 foreach(var c in dirty){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);report.Add(name+": saved; "+fuseCount+" fuse pickups use EvilClownLaugh.");
 }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
 }
 foreach(string p in new[]{"Assets/DeadAir/PlaytestHub/Prefabs/Scare Events/Trigger - Locker Fall.prefab","Assets/DeadAir/PlaytestHub/Prefabs/Scare Events/Trigger - Transit Car Noise.prefab"}){
 var go=PrefabUtility.LoadPrefabContents(p);try{var l=go.GetComponent<LockerFallScare>();if(l)l.ImpactSound=locker;var t=go.GetComponent<TransitNoiseScare>();if(t){t.Sound=transit;t.RunSpeed=15;}PrefabUtility.SaveAsPrefabAsset(go,p);}finally{PrefabUtility.UnloadPrefabContents(go);}
 }
 if(previous.IsValid())SceneManager.SetActiveScene(previous);AssetDatabase.SaveAssets();File.WriteAllLines(Application.dataPath+"/../Logs/TrialEnemyAudioUpdate.txt",report);
 }
}
