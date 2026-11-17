using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Blockout.Discovery;
public static class AILever16InteractionFix{
 [MenuItem("Tools/Dead Air/Fix AI Lever16 Interaction Prompt")]
 static void Apply(){
 if(EditorApplication.isPlaying)throw new Exception("Exit Play mode to save the scene fix.");
 var previous=SceneManager.GetActiveScene();string path="Assets/DeadAir/PlaytestHub/Scenes/AI Discovery Trial.unity";
 var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
 try{
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 var root=all.Single(t=>t.name=="Lever_16");var source=all.Single(t=>t.name=="Lever_7"&&t.GetComponent<PrototypeLever>()).GetComponent<PrototypeLever>();
 var lever=root.GetComponent<PrototypeLever>();if(!lever)throw new Exception("AI Lever16 component missing.");
 EditorSceneManager.SaveScene(scene,Application.dataPath+"/../Logs/AI-BeforeLever16Prompt-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 int previousLayer=root.gameObject.layer;Undo.RecordObject(root.gameObject,"Enable UHFPS lever interaction");root.gameObject.layer=6;
 Undo.RecordObject(lever,"Match working lever settings");lever.enabled=true;lever.OffAngle=source.OffAngle;lever.OnAngle=source.OnAngle;lever.JamRecoilAxis=source.JamRecoilAxis;lever.FollowTarget=source.FollowTarget;lever.OffPrompt=source.OffPrompt;lever.OnPrompt=source.OnPrompt;lever.Label=source.Label;lever.Jammed=false;
 if(lever.Targets==null||!lever.Targets.Any(t=>t))lever.Targets=source.Targets.ToArray();
 var collider=root.GetComponent<BoxCollider>();if(!collider)collider=Undo.AddComponent<BoxCollider>(root.gameObject);
 Undo.RecordObject(collider,"Lever face hitbox");
 var renderers=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
 var points=new System.Collections.Generic.List<Vector3>();
 foreach(var r in renderers){var b=r.bounds;foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})points.Add(root.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));}
 if(points.Count>0){var min=points.Aggregate(Vector3.Min);var max=points.Aggregate(Vector3.Max);collider.center=(min+max)*.5f;collider.size=max-min+Vector3.one*.15f;}
 collider.enabled=true;collider.isTrigger=true;
 // Put every lever hit surface on the same interaction owner.
 foreach(var childCollider in root.GetComponentsInChildren<Collider>(true).Where(c=>c!=collider)){Undo.RecordObject(childCollider,"Root owns lever raycast");childCollider.enabled=false;EditorUtility.SetDirty(childCollider);if(PrefabUtility.IsPartOfPrefabInstance(childCollider))PrefabUtility.RecordPrefabInstancePropertyModifications(childCollider);}
 foreach(var c in new Component[]{lever,collider}){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
 EditorUtility.SetDirty(root.gameObject);PrefabUtility.RecordPrefabInstancePropertyModifications(root.gameObject);
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 File.WriteAllText(Application.dataPath+"/../Logs/AILever16PromptFix.txt","Saved AI Discovery Lever16: previous layer "+previousLayer+" -> Interact (6); root controller + trigger enabled; angles match Lever7; existing door link retained. Root placement unchanged.");
 }finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
 }
}
