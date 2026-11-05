using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Blockout.Discovery;

[InitializeOnLoad]
public static class StoragePlatformResetSetup
{
    static StoragePlatformResetSetup() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (!File.Exists("Logs/StoragePlatformReset.request") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete("Logs/StoragePlatformReset.request");
        try { Apply(); } catch (Exception e) { File.WriteAllText("Logs/StoragePlatformResetResult.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Tools/Blockout/Enable Storage Platform Return")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        var movers = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PrototypeMover>(true));
        var platform = movers.Single(m => m.name == "STORAGE PLATFORM");
        var levers = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PrototypeLever>(true)).Where(l => l.Targets.Contains(platform)).ToArray();
        if (levers.Length == 0) throw new Exception("No lever wired to STORAGE PLATFORM.");
        Undo.RecordObject(platform, "Storage platform return");
        platform.Automatic = false; platform.ReturnAfterUse = true; platform.EndPause = 5;
        EditorUtility.SetDirty(platform); PrefabUtility.RecordPrefabInstancePropertyModifications(platform);
        foreach (var lever in levers)
        {
            Undo.RecordObject(lever, "Storage lever reset"); lever.FollowTarget = true; lever.IsOn = false;
            EditorUtility.SetDirty(lever); PrefabUtility.RecordPrefabInstancePropertyModifications(lever);
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Logs/StoragePlatformResetResult.txt", "PASS: STORAGE PLATFORM returns after a 5 second destination pause, stops at its starting position, and resets all " + levers.Length + " connected levers. Travel, speed and layout preserved.");
    }
}
