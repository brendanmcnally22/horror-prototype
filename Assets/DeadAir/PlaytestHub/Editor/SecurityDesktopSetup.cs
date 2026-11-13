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
using UHFPS.Runtime;

public static class SecurityDesktopSetup
{
    [MenuItem("Tools/Dead Air/Security/Connect Placed Cameras in Current Scene")]
    public static void ConnectCurrent()
    {
        var report=Connect(SceneManager.GetActiveScene());
        Debug.Log(report);
    }

    public static string Connect(Scene scene)
    {
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var feeds=all.Select(t=>t.GetComponent<CCTV_Camera>()).Where(c=>c&&c.LiveCamera).Select(c=>c.LiveCamera)
            .Concat(all.Select(t=>t.GetComponent<Camera>()).Where(c=>c&&IsSecurity(c.transform)))
            .Distinct().OrderBy(c=>Hierarchy(c.transform),StringComparer.OrdinalIgnoreCase).ToArray();
        var terminals=all.Select(t=>t.GetComponent<VirgoTerminal>()).Where(t=>t&&!t.PhoneOnly).Select(t=>t.gameObject)
            .Concat(all.Select(t=>t.GetComponent<SecurityTrialTerminal>()).Where(t=>t).Select(t=>t.gameObject)).Distinct().ToArray();
        foreach(var host in terminals)
        {
            var desktop=host.GetComponent<VirgoTerminal>();
            if(!desktop)desktop=Undo.AddComponent<VirgoTerminal>(host);
            if(!desktop.Archive)desktop.Archive=AssetDatabase.LoadAssetAtPath<VirgoArchive>("Assets/Blockout/Discovery/Data/Virgo Archive.asset");
            if(!desktop.DesktopWallpaper)desktop.DesktopWallpaper=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/hamterbackground.png");
            var security=host.GetComponent<SecurityTrialTerminal>();
            if(!security)security=Undo.AddComponent<SecurityTrialTerminal>(host);
            Undo.RecordObject(security,"Connect placed security cameras");
            // Keep explicit authored assignments; add the cameras actually present in this scene.
            security.CameraFeeds=security.CameraFeeds.Where(c=>c&&c.gameObject.scene==scene).Concat(feeds).Distinct().ToArray();
            foreach(var camera in security.CameraFeeds){Undo.RecordObject(camera,"Disable idle CCTV rendering");camera.enabled=false;EditorUtility.SetDirty(camera);}
            EditorUtility.SetDirty(desktop);EditorUtility.SetDirty(security);
        }
        if(terminals.Length>0)EditorSceneManager.MarkSceneDirty(scene);
        return scene.path+": "+terminals.Length+" desktops, "+feeds.Length+" placed security cameras.\n"+string.Join("\n",feeds.Select(c=>Hierarchy(c.transform)));
    }

    static bool IsSecurity(Transform t)
    {
        // Never capture a player, item, menu, or Cinemachine camera by accident.
        if(t.GetComponentInParent<PlayerManager>())return false;
        for(var parent=t;parent;parent=parent.parent)
        {
            string name=parent.name.ToLowerInvariant();
            if(name.Contains("securitycamera")||name.Contains("security camera")||name.Contains("cctv")||name.Contains("surveillance"))return true;
        }
        return false;
    }
    static string Hierarchy(Transform t)=>t.parent?Hierarchy(t.parent)+"/"+t.name:t.name;

    // Used for unattended import/verification when the editor is closed.
    public static void ApplySavedScenes()
    {
        var reports=new List<string>();Directory.CreateDirectory("Logs/SecurityDesktopSafety");
        foreach(string path in new[]{"Assets/Blockout/Discovery/Scenes/Discovery Blank 1.unity","Assets/Blockout/Discovery/Scenes/Discovery Blank.unity","Assets/DeadAir/PlaytestHub/Scenes/AI Discovery Trial.unity","Assets/DeadAir/PlaytestHub/Scenes/Security Terminal Trial.unity"})
        {
            File.Copy(path,"Logs/SecurityDesktopSafety/"+Path.GetFileNameWithoutExtension(path)+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",false);
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            reports.Add(Connect(scene));EditorSceneManager.SaveScene(scene);
        }
        File.WriteAllText("Logs/SecurityDesktopSetup.txt",string.Join("\n\n",reports));
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Dead Air/UI/Edit Interaction Prompt")]
    public static void SelectInteraction()
    {
        var panel=UnityEngine.Object.FindObjectsByType<InteractInfoPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault();
        Select(panel?panel.gameObject:null,"Open your facility scene first, then choose this shortcut again.");
    }
    [MenuItem("Tools/Dead Air/UI/Edit Pickup Notification")]
    public static void SelectPickup()
    {
        var manager=UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault();
        Select(manager?manager.ItemPickup:null,"Open your facility scene first, then choose this shortcut again.");
    }
    static void Select(UnityEngine.Object target,string missing)
    {
        if(!target){Debug.LogWarning(missing);return;}Selection.activeObject=target;EditorGUIUtility.PingObject(target);
    }
}
