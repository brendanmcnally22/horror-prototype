using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class PortableDemoBuild {
 public static string Stamp=""; 
 [MenuItem("Tools/Dead Air/Build Portable Windows Demo")]
 public static void Build(){
 string logs=Application.dataPath+"/../Logs";
 if(EditorApplication.isPlaying){File.WriteAllText(logs+"/PortableDemoBuild.txt","BLOCKED: Exit Play mode before building.");return;}
 string stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss");
 string folder=Path.GetFullPath(Application.dataPath+"/../Builds/DeadAir-TestDemo-"+stamp);
 Directory.CreateDirectory(folder);
 string receipt=logs+"/PortableDemoBuild.txt";
 File.WriteAllText(receipt,"BUILDING\n"+folder);
 try{
   if(!EditorSceneManager.SaveOpenScenes())throw new Exception("Could not save open scene edits.");
   AssetDatabase.SaveAssets();
   string root="Assets/DeadAir/PlaytestHub/Scenes/";
   var scenes=new[]{"Playtest Menu","AI Discovery Trial","Security Terminal Trial","Three Doors Lighting Trial","Playtest Feedback","Playtest Results"}.Select(n=>root+n+".unity").ToArray();
   var options=new BuildPlayerOptions{scenes=scenes,locationPathName=folder+"/DeadAir.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.CompressWithLz4HC};
   var report=BuildPipeline.BuildPlayer(options);
   if(report.summary.result!=BuildResult.Succeeded){
      string messages=string.Join("\n",report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Exception).Select(m=>m.content));
      File.WriteAllText(receipt,"FAILED\n"+folder+"\n"+report.summary.result+"\n"+messages);return;
   }
   File.WriteAllText(folder+"/START HERE.txt",
      "DEAD AIR - TEST DEMO HUB\r\n\r\nExtract the entire ZIP into a folder, then run DeadAir.exe. Keep all files and the DeadAir_Data folder together. Windows 64-bit. No Unity installation required.\r\n\r\nChoose a trial from the hub. Press [ to finish a level early. Use the hub to choose another trial.\r\n\r\nObjectives:\r\nAI Discovery: Find 3 fuses.\r\nSecurity Terminal: Spot enemies on 3 different cameras.\r\nThree Doors: Choose a door; the first lever choice ends this trial.\r\n\r\nBasic controls: WASD movement, E interact, Esc pause/leave the computer. AI Discovery has no sprint; F equips/stows the collected candle and right mouse raises it. Security cameras: Space next feed, A/D pan, drag feed to aim, wheel to zoom, Spot enemy button to record a sighting.\r\n\r\nFeedback and playtest recordings save locally on this computer under Unity's application data folder (AppData/LocalLow, using the project's company/product names). They are not uploaded. The Results booklet is accessible from the hub.\r\n\r\nThis package contains the built game only, not the Unity project or previous playtest recordings.\r\n");
   File.WriteAllText(receipt,"SUCCEEDED\n"+folder+"\nBytes="+report.summary.totalSize+"\nBuild time="+report.summary.totalTime);
 }catch(Exception e){File.WriteAllText(receipt,"FAILED\n"+folder+"\n"+e);}
 }
}

