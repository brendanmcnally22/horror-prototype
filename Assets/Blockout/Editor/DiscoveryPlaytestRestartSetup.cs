using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Blockout.Discovery;

[InitializeOnLoad]
public static class DiscoveryPlaytestRestartSetup
{
    static DiscoveryPlaytestRestartSetup() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (!File.Exists("Logs/DiscoveryPlaytestRestart.request") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete("Logs/DiscoveryPlaytestRestart.request");
        try { Apply(); } catch (Exception e) { File.WriteAllText("Logs/DiscoveryPlaytestRestartResult.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Tools/Blockout/Add Playtest Completion Restart")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != DiscoverySetup.ScenePath) throw new Exception("Open Discovery Blank first.");
        var loop = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DiscoveryLoop>(true)).Single();
        var restart = loop.GetComponent<DiscoveryPlaytestRestart>();
        if (!restart) restart = Undo.AddComponent<DiscoveryPlaytestRestart>(loop.gameObject);
        Undo.RecordObject(restart, "Link playtest completion"); restart.Loop = loop;
        EditorUtility.SetDirty(restart);
        if (PrefabUtility.IsPartOfPrefabInstance(restart)) PrefabUtility.RecordPrefabInstancePropertyModifications(restart);
        var builds = EditorBuildSettings.scenes.ToList();
        int index = builds.FindIndex(s => s.path == scene.path);
        if (index < 0) builds.Add(new EditorBuildSettingsScene(scene.path, true)); else builds[index].enabled = true;
        EditorBuildSettings.scenes = builds.ToArray();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Logs/DiscoveryPlaytestRestartResult.txt", "PASS: completion restart added to discovery loop. Appears only after all required fuses are returned; Restart Playtest loads this scene fresh, Keep Exploring restores control. Layout preserved.");
    }
}
