using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class TrialGloomSetup {
 static T Effect<T>(VolumeProfile p) where T:VolumeComponent {var e=p.components.OfType<T>().FirstOrDefault();return e?e:p.Add<T>(true);}
 [MenuItem("Tools/Dead Air/Apply Gloom Bloom And Haze")]
 static void Apply(){
 if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
 const string folder="Assets/DeadAir/PlaytestHub/Profiles";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
 string path=folder+"/Gloom Bloom Haze.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
 if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
 var bloom=Effect<Bloom>(profile);bloom.intensity.Override(1.15f);bloom.threshold.Override(.85f);bloom.scatter.Override(.75f);bloom.highQualityFiltering.Override(true);
 var color=Effect<ColorAdjustments>(profile);color.postExposure.Override(-.8f);color.contrast.Override(12);color.saturation.Override(-12);color.colorFilter.Override(new Color(.89f,.94f,1));
 var balance=Effect<WhiteBalance>(profile);balance.temperature.Override(-9);
 var vignette=Effect<Vignette>(profile);vignette.intensity.Override(.23f);vignette.smoothness.Override(.5f);
 foreach(var c in profile.components){EditorUtility.SetDirty(c);if(!AssetDatabase.Contains(c))AssetDatabase.AddObjectToAsset(c,profile);}EditorUtility.SetDirty(profile);
 var previous=SceneManager.GetActiveScene();var report=new List<string>();
 foreach(string name in new[]{"AI Discovery Trial","Security Terminal Trial","Three Doors Lighting Trial"}){
 string scenePath="Assets/DeadAir/PlaytestHub/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(scenePath);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
 try{
 EditorSceneManager.SaveScene(scene,Application.dataPath+"/../Logs/"+name+"-BeforeGloom-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 var roots=scene.GetRootGameObjects();var go=roots.FirstOrDefault(g=>g.name=="ATMOSPHERE - Gloom Bloom Haze");if(!go){go=new GameObject("ATMOSPHERE - Gloom Bloom Haze");SceneManager.MoveGameObjectToScene(go,scene);Undo.RegisterCreatedObjectUndo(go,"Gloom atmosphere");}
 var volume=go.GetComponent<Volume>();if(!volume)volume=Undo.AddComponent<Volume>(go);volume.isGlobal=true;volume.priority=.9f;volume.weight=1;volume.sharedProfile=profile;EditorUtility.SetDirty(volume);
 RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Exponential;RenderSettings.fogDensity=.018f;RenderSettings.fogColor=new Color(.022f,.029f,.041f);RenderSettings.reflectionIntensity=Mathf.Min(RenderSettings.reflectionIntensity,.12f);
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);report.Add(name+": saved darker post-exposure, stronger bloom, cool atmospheric distance haze.");
 }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
 }if(previous.IsValid())SceneManager.SetActiveScene(previous);AssetDatabase.SaveAssets();File.WriteAllLines(Application.dataPath+"/../Logs/TrialGloom.txt",report);
 }
}
