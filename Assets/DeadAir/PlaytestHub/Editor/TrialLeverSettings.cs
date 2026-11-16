using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Blockout.Discovery;

public static class TrialLeverSettings
{
    [MenuItem("Tools/Dead Air/Apply Requested Lever Angles To Three Trials")]
    public static void Apply(){
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
        var previous=SceneManager.GetActiveScene();var report=new List<string>();
        foreach(string name in new[]{"AI Discovery Trial","Security Terminal Trial","Three Doors Lighting Trial"}){
            string path="Assets/DeadAir/PlaytestHub/Scenes/"+name+".unity";
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try{
                var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
                var before=all.ToDictionary(t=>t,t=>(t.localPosition,t.localRotation,t.localScale));
                EditorSceneManager.SaveScene(scene,Application.dataPath+"/../Logs/"+name+"-BeforeLeverAngles-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
                var levers=all.Select(t=>t.GetComponent<PrototypeLever>()).Where(l=>l).ToArray();
                foreach(var lever in levers){
                    Undo.RecordObject(lever,"Requested lever angles");lever.OffAngle=new Vector3(0,0,-50);lever.OnAngle=new Vector3(0,0,195);
                    EditorUtility.SetDirty(lever);if(PrefabUtility.IsPartOfPrefabInstance(lever))PrefabUtility.RecordPrefabInstancePropertyModifications(lever);
                }
                foreach(var t in all){var old=before[t];if(t.localPosition!=old.Item1||t.localRotation!=old.Item2||t.localScale!=old.Item3)throw new Exception("Unexpected transform change");}
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                report.Add(name+": "+levers.Length+" lever components set to Off (0,0,-50), On (0,0,195). Transforms preserved.");
                report.AddRange(all.Where(t=>t.name.Contains("tube",StringComparison.OrdinalIgnoreCase)).Select(t=>"TUBE: "+t.name+" at "+t.position));
            }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        if(previous.IsValid())SceneManager.SetActiveScene(previous);
        File.WriteAllLines(Application.dataPath+"/../Logs/TrialLeverAngles.txt",report);
    }
}

[CustomEditor(typeof(VirgoTerminal))]
public sealed class VirgoDesktopInspector : Editor
{
    public override void OnInspectorGUI(){
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Appearance fields update during Play mode. Open the preview to inspect the desktop theme without running gameplay. Play mode edits need to be copied back before stopping Play.",MessageType.Info);
        if(GUILayout.Button("Open Live Desktop Appearance Preview"))VirgoAppearancePreview.Show((VirgoTerminal)target);
    }
}
public sealed class VirgoAppearancePreview : EditorWindow
{
    VirgoTerminal terminal;
    public static void Show(VirgoTerminal t){var w=GetWindow<VirgoAppearancePreview>("PC Appearance");w.terminal=t;w.minSize=new Vector2(500,360);w.Show();}
    void OnInspectorUpdate()=>Repaint();
    static void Fill(Rect r,Color color){var c=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=c;}
    void OnGUI(){
        if(!terminal){EditorGUILayout.LabelField("Select a Virgo Terminal and open its preview.");return;}
        EditorGUILayout.LabelField("Live appearance preview — "+terminal.StationName,EditorStyles.boldLabel);
        var region=new Rect(0,28,position.width,position.height-28);
        GUI.BeginGroup(region);var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(region.width/1024,region.height/768,1));
        Fill(new Rect(0,0,1024,768),terminal.DesktopColor);
        if(terminal.DesktopWallpaper)GUI.DrawTexture(new Rect(0,0,1024,726),terminal.DesktopWallpaper,ScaleMode.ScaleAndCrop);
        var text=new GUIStyle(EditorStyles.label){fontSize=terminal.DesktopFontSize};text.normal.textColor=terminal.TextColor;
        Fill(new Rect(160,70,720,510),terminal.WindowColor);Fill(new Rect(163,73,714,24),terminal.TitleBarColor);
        var title=new GUIStyle(text);title.normal.textColor=Color.white;GUI.Label(new Rect(175,74,650,24),"File Explorer — C:\\Records",title);
        GUI.Label(new Rect(180,110,620,30),"File     Edit     View     Help",text);Fill(new Rect(180,150,680,370),Color.white);
        GUI.Label(new Rect(205,175,620,35),"Reception        Staff        Director",text);
        GUI.Label(new Rect(205,235,620,65),"Select a file to read its contents.\nPreview reflects the selected terminal's colors, font and wallpaper.",text);
        Fill(new Rect(0,726,1024,42),terminal.TaskbarColor);GUI.Label(new Rect(20,737,980,30),"Start      Explorer      Googus      Security                         VIRGO OS 95",title);
        GUI.matrix=old;GUI.EndGroup();
    }
}
