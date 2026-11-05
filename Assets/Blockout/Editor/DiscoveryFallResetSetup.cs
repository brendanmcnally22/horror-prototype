using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Blockout.Discovery;

[InitializeOnLoad]
public static class DiscoveryFallResetSetup
{
    const string PrefabPath = "Assets/Blockout/Discovery/Prefabs/Triggers/Fall Reset - Restart Discovery Loop.prefab";
    static DiscoveryFallResetSetup() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (!File.Exists("Logs/DiscoveryFallReset.request") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete("Logs/DiscoveryFallReset.request");
        try { Apply(); } catch (Exception e) { File.WriteAllText("Logs/DiscoveryFallResetResult.txt", e.ToString()); Debug.LogException(e); }
    }

    [MenuItem("Tools/Blockout/Add Discovery Fall Reset")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != DiscoverySetup.ScenePath) throw new Exception("Open Discovery Blank to add its fall reset.");
        Directory.CreateDirectory("Assets/Blockout/Discovery/Prefabs/Triggers");
        AssetDatabase.Refresh();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (!prefab)
        {
            var temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(temporary);
            try
            {
                var root = new GameObject("Fall Reset - Restart Discovery Loop");
                var box = root.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = new Vector3(30, 6, 30);
                root.AddComponent<DiscoveryFallReset>();
                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { SceneManager.SetActiveScene(scene); EditorSceneManager.CloseScene(temporary, true); }
        }
        var existing = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DiscoveryFallReset>(true)).FirstOrDefault();
        if (!existing)
        {
            var placed = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(placed, "Add discovery fall reset");
            placed.name = "FALL RESET - Discovery Loop";
            placed.transform.position = new Vector3(-125, -15, 10);
            placed.GetComponent<BoxCollider>().size = new Vector3(200, 6, 200);
            PrefabUtility.RecordPrefabInstancePropertyModifications(placed.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(placed.GetComponent<BoxCollider>());
            existing = placed.GetComponent<DiscoveryFallReset>();
        }
        // Direct reload must also work in standalone builds.
        var builds = EditorBuildSettings.scenes.ToList();
        int index = builds.FindIndex(s => s.path == scene.path);
        if (index < 0) builds.Add(new EditorBuildSettingsScene(scene.path, true));
        else builds[index].enabled = true;
        EditorBuildSettings.scenes = builds.ToArray();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Selection.activeGameObject = existing.gameObject;
        File.WriteAllText("Logs/DiscoveryFallResetResult.txt", "PASS: empty fall-reset trigger added beneath facility at (-125,-15,10), size 200 x 6 x 200. Player-only entry reloads this level fresh at its authored start. Reusable prefab saved at " + PrefabPath + ". Discovery scene enabled in build settings. Existing layout preserved.");
    }
}
