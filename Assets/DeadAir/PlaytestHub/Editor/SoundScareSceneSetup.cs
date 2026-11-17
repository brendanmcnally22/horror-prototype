using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Blockout.Discovery;
public static class SoundScareSceneSetup {
 [MenuItem("Tools/Dead Air/Apply Sound Only Locker Transit And Side Gates")]
 static void Apply(){
 if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
 var previous=SceneManager.GetActiveScene();var report=new List<string>();
 foreach(string name in new[]{"AI Discovery Trial","Security Terminal Trial"}){
 string path="Assets/DeadAir/PlaytestHub/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
 try{
 EditorSceneManager.SaveScene(scene,Application.dataPath+"/../Logs/"+name+"-BeforeSoundOnly-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 foreach(var enemy in all.Select(t=>t.GetComponent<GoblinSurfacePatrol>()).Where(e=>e).ToArray()){report.Add(name+": removed visible goblin "+enemy.name);Undo.DestroyObjectImmediate(enemy.gameObject);}
 foreach(var scout in all.Where(t=>t).Select(t=>t.GetComponent<DeadAir.Playtesting.TrialCameraHider>()).Where(h=>h).ToArray())Undo.DestroyObjectImmediate(scout);
 if(name.StartsWith("AI")){
 int gates=0;
 foreach(var gate in all.Where(t=>t).Select(t=>t.GetComponent<PlatformSafetyGate>()).Where(g=>g)){
 Undo.RecordObject(gate,"Side-aware sliding gates");
 gate.LiftOpen=false;gate.OpenForNearbyPlayer=true;gate.OpenAtStart=true;gate.OpenAtEnd=true;
 gate.DetectionSize=new Vector3(gate.EntranceSize.x+.25f,Mathf.Max(2.2f,gate.EntranceSize.y),2.5f);
 gate.SlideDistance=gate.EntranceSize.x*.5f+.06f;
 EditorUtility.SetDirty(gate);if(PrefabUtility.IsPartOfPrefabInstance(gate))PrefabUtility.RecordPrefabInstancePropertyModifications(gate);gates++;
 }
 if(!all.Where(t=>t).Any(t=>t.GetComponent<FirstFuseAudioScare>())){
 var audio=new GameObject("FIRST FUSE - Sound Only Scare");SceneManager.MoveGameObjectToScene(audio,scene);audio.AddComponent<FirstFuseAudioScare>();Undo.RegisterCreatedObjectUndo(audio,"Create first-fuse shriek");
 }
 var locker=all.FirstOrDefault(t=>t&&t.name.StartsWith("FALLING LOCKER",StringComparison.OrdinalIgnoreCase));
 if(locker){
 var effect=locker.GetComponent<LockerFallScare>();if(!effect)effect=Undo.AddComponent<LockerFallScare>(locker.gameObject);
 effect.LockerPivot=locker;effect.RotateAroundBase=true;
 var rb=locker.GetComponent<Rigidbody>();if(!rb)rb=Undo.AddComponent<Rigidbody>(locker.gameObject);rb.isKinematic=true;rb.useGravity=false;
 var triggers=all.Where(t=>t).Select(t=>t.GetComponent<LockerFallScare>()).Where(e=>e&&e!=effect).ToArray();
 foreach(var prior in triggers){
 var sample=prior.LockerPivot;
 Undo.RecordObject(prior,"Use renamed scene locker");prior.LockerPivot=locker;prior.RotateAroundBase=true;
 if(sample&&sample!=locker&&sample.IsChildOf(prior.transform)){Undo.RecordObject(sample.gameObject,"Disable sample locker");sample.gameObject.SetActive(false);}
 EditorUtility.SetDirty(prior);if(PrefabUtility.IsPartOfPrefabInstance(prior))PrefabUtility.RecordPrefabInstancePropertyModifications(prior);
 }
 if(triggers.Length==0&&!scene.GetRootGameObjects().Any(g=>g.name=="TRIGGER - Actual Falling Locker")){
 var triggerRoot=new GameObject("TRIGGER - Actual Falling Locker");SceneManager.MoveGameObjectToScene(triggerRoot,scene);triggerRoot.transform.position=locker.position-locker.forward*2;triggerRoot.transform.rotation=locker.rotation;
 var box=triggerRoot.AddComponent<BoxCollider>();box.isTrigger=true;box.center=Vector3.up;box.size=new Vector3(3,2,2);
 var trigger=triggerRoot.AddComponent<PlaceableScareTrigger>();UnityEditor.Events.UnityEventTools.AddPersistentListener(trigger.OnPlayerEntered,effect.Activate);Undo.RegisterCreatedObjectUndo(triggerRoot,"Actual locker trigger");
 }
 EditorUtility.SetDirty(effect);EditorUtility.SetDirty(rb);if(PrefabUtility.IsPartOfPrefabInstance(effect))PrefabUtility.RecordPrefabInstancePropertyModifications(effect);if(PrefabUtility.IsPartOfPrefabInstance(rb))PrefabUtility.RecordPrefabInstancePropertyModifications(rb);
 report.Add("Actual named locker wired, original placement preserved; sample locker disabled where present.");
 }else report.Add("Named locker not found.");
 var rail=all.Where(t=>t).Select(t=>t.GetComponent<PrototypeMover>()).FirstOrDefault(m=>m&&m.name=="TRANSIT RAIL");
 foreach(var transit in all.Where(t=>t).Select(t=>t.GetComponent<TransitNoiseScare>()).Where(e=>e)){
 Undo.RecordObject(transit,"Run actual transit car");transit.TransitMover=rail;transit.TransitCar=rail?rail.MovingPart:null;transit.RunCar=true;
 EditorUtility.SetDirty(transit);if(PrefabUtility.IsPartOfPrefabInstance(transit))PrefabUtility.RecordPrefabInstancePropertyModifications(transit);
 report.Add("Transit trigger assigned to "+(rail?rail.name:"no rail found")+". Existing route and lights retained.");
 }
 report.Add("AI: "+gates+" sideways gates with per-side approach detection, sound-only first-fuse scare.");
 }
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
 }if(previous.IsValid())SceneManager.SetActiveScene(previous);AssetDatabase.SaveAssets();File.WriteAllLines(Application.dataPath+"/../Logs/SoundOnlyScareUpdate.txt",report);
 }
 [MenuItem("Tools/Dead Air/Inspect Locker And Transit Wiring")]
 static void Inspect(){
 var report=new List<string>();var previous=SceneManager.GetActiveScene();
 foreach(string name in new[]{"AI Discovery Trial","Security Terminal Trial"}){
 var path="Assets/DeadAir/PlaytestHub/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
 try{var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 report.AddRange(all.Where(t=>t.name.Contains("FALLING",StringComparison.OrdinalIgnoreCase)||t.name.Contains("TRANSIT",StringComparison.OrdinalIgnoreCase)||t.name.Contains("CAR",StringComparison.OrdinalIgnoreCase)&&!t.name.Contains("Carriage")||t.GetComponent<LockerFallScare>()||t.GetComponent<TransitNoiseScare>()).Select(t=>name+" | "+t.name+" | "+t.position+" | parent="+t.parent?.name+" | "+string.Join(",",t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name))+" | children="+string.Join(",",t.Cast<Transform>().Select(c=>c.name))));
 report.AddRange(all.Select(t=>t.GetComponent<PrototypeMover>()).Where(m=>m).Select(m=>name+" MOVER "+m.name+" Travel="+m.Travel+" Part="+m.MovingPart?.name+" at "+m.transform.position));
 }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
 }if(previous.IsValid())SceneManager.SetActiveScene(previous);File.WriteAllLines(Application.dataPath+"/../Logs/LockerTransitWiring.txt",report);
 }
}

