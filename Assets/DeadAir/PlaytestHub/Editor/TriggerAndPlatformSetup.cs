using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Blockout.Discovery;
public static class TriggerAndPlatformSetup {
 const string Folder="Assets/DeadAir/PlaytestHub/Prefabs/Scare Events";
 [MenuItem("Tools/Dead Air/Create Scare Triggers And Attach Platform Levers")]
 static void Apply(){
 if(EditorApplication.isPlaying)throw new Exception("Exit Play mode.");
 Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
 var previous=SceneManager.GetActiveScene();var temporary=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(temporary);
 try{
 foreach(bool locker in new[]{true,false}){
 string name=locker?"Trigger - Locker Fall":"Trigger - Transit Car Noise";string path=Folder+"/"+name+".prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(path))continue;
 var root=new GameObject(name);var box=root.AddComponent<BoxCollider>();box.isTrigger=true;box.center=Vector3.up;box.size=new Vector3(3,2,3);
 var body=root.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
 var trigger=root.AddComponent<PlaceableScareTrigger>();
 if(locker){
 var effect=root.AddComponent<LockerFallScare>();var pivot=new GameObject("Falling locker pivot - place base on floor").transform;pivot.SetParent(root.transform,false);pivot.localPosition=new Vector3(2,0,0);effect.LockerPivot=pivot;
 var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThunderWire Studio/UHFPS/Content/Prefabs/Interact/Hiding/Locker.prefab"),pivot);
 PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
 foreach(var script in model.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
 foreach(var rb in model.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(rb);
 model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
 var rs=model.GetComponentsInChildren<Renderer>();if(rs.Length>0){var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);model.transform.position+=new Vector3(pivot.position.x-b.center.x,pivot.position.y-b.min.y,pivot.position.z-b.center.z);}
 var kinematic=pivot.gameObject.AddComponent<Rigidbody>();kinematic.isKinematic=true;kinematic.useGravity=false;
 UnityEventTools.AddPersistentListener(trigger.OnPlayerEntered,effect.Activate);
 }else{var effect=root.AddComponent<TransitNoiseScare>();UnityEventTools.AddPersistentListener(trigger.OnPlayerEntered,effect.Activate);}
 PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);
 }
 }finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(temporary,true);}
 var scene=SceneManager.GetSceneByPath("Assets/DeadAir/PlaytestHub/Scenes/AI Discovery Trial.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/DeadAir/PlaytestHub/Scenes/AI Discovery Trial.unity",OpenSceneMode.Additive);
 var report=new List<string>();
 try{
 EditorSceneManager.SaveScene(scene,Application.dataPath+"/../Logs/AI-BeforePlatformLeverParenting.unity",true);
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 var levers=all.Select(t=>t.GetComponent<PrototypeLever>()).Where(l=>l).ToArray();
 foreach(var mover in all.Select(t=>t.GetComponent<PrototypeMover>()).Where(m=>m&&m.MovingPart&&(m.name.Contains("Platform",StringComparison.OrdinalIgnoreCase)||m.name=="STARTING ELEVATOR"))){
 var assigned=levers.Where(l=>l.Targets.Contains(mover)&&!l.Jammed&&!l.Label.Contains("CALL",StringComparison.OrdinalIgnoreCase)).ToArray();
 // Existing onboard control wins; only parent controls already sitting on the deck.
 bool hasOnboard=assigned.Any(l=>l.transform.IsChildOf(mover.MovingPart));
 var candidates=assigned.Where(l=>!l.transform.IsChildOf(mover.MovingPart)).OrderBy(l=>Vector3.Distance(l.transform.position,mover.MovingPart.position)).ToArray();
 foreach(var lever in candidates){
 var local=mover.MovingPart.InverseTransformPoint(lever.transform.position);
 // Far-away landing controls stay put.
 if(Vector2.Distance(new Vector2(local.x,local.z),Vector2.zero)>5){report.Add("Preserved landing control "+lever.name);continue;}
 if(hasOnboard)continue;
 Undo.SetTransformParent(lever.transform,mover.MovingPart,"Attach onboard platform lever");
 // Move only default-position controls off the platform, using its new railing footprint.
 var safety=mover.MovingPart.Find("CONFIGURABLE FOUR SIDE SAFETY");
 var gates=safety?safety.GetComponentsInChildren<PlatformSafetyGate>():Array.Empty<PlatformSafetyGate>();
 float halfX=1.5f,halfZ=1.5f,deckY=0;
 if(gates.Length>0){
 var front=gates.FirstOrDefault(g=>g.name.Contains("Front"));var back=gates.FirstOrDefault(g=>g.name.Contains("Rear"));var left=gates.FirstOrDefault(g=>g.name.Contains("Left"));
 if(front){halfX=front.EntranceSize.x/2;halfZ=Mathf.Abs(front.transform.localPosition.z);deckY=front.transform.localPosition.y;}
 else if(back){halfX=back.EntranceSize.x/2;halfZ=Mathf.Abs(back.transform.localPosition.z);deckY=back.transform.localPosition.y;}
 if(left)halfZ=left.EntranceSize.x/2;
 }
 if(Mathf.Abs(local.x)>halfX-.15f||Mathf.Abs(local.z)>halfZ-.15f||local.y<deckY-.05f||local.y>deckY+.65f){
 lever.transform.localPosition=new Vector3(Mathf.Max(.2f,halfX-.55f),deckY,Mathf.Max(.2f,halfZ-.55f));
 report.Add("Moved "+lever.name+" onto "+mover.name+" at "+lever.transform.localPosition);
 }else report.Add("Attached already-placed "+lever.name+" without moving it.");
 hasOnboard=true;EditorUtility.SetDirty(lever.transform);if(PrefabUtility.IsPartOfPrefabInstance(lever.transform))PrefabUtility.RecordPrefabInstancePropertyModifications(lever.transform);
 }
 }
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 }finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
 AssetDatabase.SaveAssets();File.WriteAllLines(Application.dataPath+"/../Logs/ScareTriggersAndLevers.txt",new[]{"Created wired locker-fall and transit-noise trigger prefabs."}.Concat(report));
 }
}
