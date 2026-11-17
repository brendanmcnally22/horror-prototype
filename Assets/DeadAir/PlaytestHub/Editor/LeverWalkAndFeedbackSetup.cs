using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Blockout.Discovery;
using UHFPS.Runtime;
using DeadAir.Playtesting;
public static class LeverWalkAndFeedbackSetup {
 [MenuItem("Tools/Dead Air/Fix Lever16 Slow AI Walking And Reset Feedback")]
 static void Apply(){
 if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before saving.");
 var previous=SceneManager.GetActiveScene();var lines=new System.Collections.Generic.List<string>();
 foreach(string name in new[]{"Security Terminal Trial","AI Discovery Trial"}){
 string path="Assets/DeadAir/PlaytestHub/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
 try{
 EditorSceneManager.SaveScene(scene,Application.dataPath+"/../Logs/"+name+"-BeforeLever16Walk-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 if(name.StartsWith("Security")){
 var target=all.Single(t=>t.name=="Lever_16");var source=all.Single(t=>t.name=="Lever_7"&&t.GetComponent<PrototypeLever>());
 if(!target.Find("Working lever mechanism")){
 // Keep the authored placement; substitute the already-working mechanism on the opposite side.
 foreach(Transform child in target){Undo.RecordObject(child.gameObject,"Retain old lever visuals");child.gameObject.SetActive(false);}
 foreach(var c in target.GetComponents<Collider>()){Undo.RecordObject(c,"Disable old hitbox");c.enabled=false;}
 foreach(var c in target.GetComponents<MonoBehaviour>()){Undo.RecordObject(c,"Replace old lever controller");c.enabled=false;}
 var clone=UnityEngine.Object.Instantiate(source.gameObject);clone.name="Working lever mechanism";Undo.RegisterCreatedObjectUndo(clone,"Repair Lever_16");
 clone.transform.SetParent(target,false);clone.transform.localPosition=Vector3.zero;clone.transform.localRotation=Quaternion.identity;clone.transform.localScale=Vector3.one;
 lines.Add("Lever_16 uses a copy of Lever_7's working handle, interaction collider, status lights and same door link; root placement retained.");
 }
 }else{
 var player=all.Select(t=>t.GetComponent<PlayerManager>()).Single(p=>p);
 Undo.RecordObject(player.PlayerStateMachine,"Slower AI walking");float old=player.PlayerStateMachine.PlayerBasicSettings.WalkSpeed;player.PlayerStateMachine.PlayerBasicSettings.WalkSpeed=2.6f;
 EditorUtility.SetDirty(player.PlayerStateMachine);if(PrefabUtility.IsPartOfPrefabInstance(player.PlayerStateMachine))PrefabUtility.RecordPrefabInstancePropertyModifications(player.PlayerStateMachine);
 lines.Add("AI Discovery walking: "+old+" -> 2.6 m/s. Sprint remains disabled.");
 }
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
 }
 if(previous.IsValid())SceneManager.SetActiveScene(previous);AssetDatabase.SaveAssets();
 string root=HubRunStore.Root;string backup=Application.dataPath+"/../Logs/FeedbackBeforeReset-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);int count=0;
 if(Directory.Exists(root))foreach(string folder in Directory.GetDirectories(root)){
 string file=Path.Combine(folder,"hub-run.json");if(!File.Exists(file))continue;
 var run=JsonUtility.FromJson<HubRun>(File.ReadAllText(file));if(run==null||!Guid.TryParseExact(run.id,"N",out _))continue;
 File.Copy(file,Path.Combine(backup,run.id+".json"),false);
 run.clarity=run.tension=run.enjoyment=0;run.worked=run.confusing=run.notes=run.skipReason="";run.feedbackStatus="pending";
 if(!HubRunStore.Save(run))throw new IOException(HubRunStore.LastError);count++;
 }
 lines.Add("Cleared feedback answers/ratings for "+count+" saved runs. Movement recordings and run metrics retained.");
 File.WriteAllLines(Application.dataPath+"/../Logs/LeverWalkFeedbackReset.txt",lines);
 }
}
