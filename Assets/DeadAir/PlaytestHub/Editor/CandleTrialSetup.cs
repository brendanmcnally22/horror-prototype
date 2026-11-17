using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using Blockout.Discovery;
using UHFPS.Runtime;
public static class CandleTrialSetup{
 [MenuItem("Tools/Dead Air/Use Table Candle In AI Discovery")]
 static void Apply(){
 if(EditorApplication.isPlaying)throw new Exception("Exit Play mode.");
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.name!="AI Discovery Trial")throw new Exception("Open AI Discovery Trial.");
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 var player=all.Select(t=>t.GetComponent<PlayerManager>()).Single(p=>p);var candle=player.GetComponentInChildren<CandleItem>(true);
 var pickup=all.Select(t=>t.GetComponent<InteractableItem>()).FirstOrDefault(i=>i&&!i.GetComponentInParent<PlayerManager>()&&new SerializedObject(i).FindProperty("PickupItem")?.FindPropertyRelative("m_GUID")?.stringValue=="6993e34dac744c23966afbcbba685cf9");
 if(!candle||!pickup)throw new Exception("Could not locate table candle pickup and equipped CandleItem; stopped.");
 EditorSceneManager.SaveScene(scene,Application.dataPath+"/../Logs/AI-BeforeCandleAndAudio.unity",true);
 var rules=player.GetComponent<FacilityPlayerRules>();if(!rules)rules=Undo.AddComponent<FacilityPlayerRules>(player.gameObject);Undo.RecordObject(rules,"Disable sprinting in AI level");rules.DisableSprinting=true;
 var experience=player.GetComponent<CandleTrialExperience>();if(!experience)experience=Undo.AddComponent<CandleTrialExperience>(player.gameObject);
 experience.Player=player;experience.Candle=candle;
 experience.SlenderAmbience=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Slender Game Ambience.mp3");
 experience.AnxiousBreathing=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Anxious Breathing - Sound Effect.mp3");
 experience.Elevator=all.Select(t=>t.GetComponent<PrototypeMover>()).Single(m=>m&&m.name=="STARTING ELEVATOR");
 var rider=experience.Elevator.GetComponentInChildren<PrototypeRider>(true);experience.ElevatorRiderZone=rider?rider.GetComponent<BoxCollider>():null;
 Undo.RecordObject(pickup,"Candle pickup controls");pickup.OnTakeEvent??=new UnityEngine.Events.UnityEvent();
 if(!Enumerable.Range(0,pickup.OnTakeEvent.GetPersistentEventCount()).Any(i=>pickup.OnTakeEvent.GetPersistentTarget(i)==experience))UnityEventTools.AddPersistentListener(pickup.OnTakeEvent,experience.CandlePickedUp);
 var inventory=all.Select(t=>t.GetComponent<Inventory>()).Single(i=>i);Undo.RecordObject(inventory,"Remove starting flashlight");inventory.startingItems.RemoveAll(i=>i.GUID=="ba53a6ad76bb4513ba434a3b7542345a");
 var old=player.GetComponent<FacilityFlashlightControl>();if(old){Undo.RecordObject(old,"Candle replaces flashlight");old.enabled=false;EditorUtility.SetDirty(old);PrefabUtility.RecordPrefabInstancePropertyModifications(old);}
 foreach(var c in new Component[]{rules,experience,pickup,inventory}){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 File.WriteAllText(Application.dataPath+"/../Logs/CandleTrialSetup.txt","AI Discovery saved: sprint disabled; existing table candle pickup linked to F equip + right mouse focus hint; starting flashlight removed; Slender ambience starts after exiting elevator at bottom; darkness-triggered anxious breathing fades out near light or lit candle. Other trials untouched.");
 }
}
