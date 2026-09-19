using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UHFPS.Runtime;

// Explicit, file-triggered smoke check used while authoring the starter scene.
[InitializeOnLoad]
public static class BlockoutPlayCheck
{
    const string Request = "Logs/BlockoutPlayCheck.request";
    const string Report = "Logs/BlockoutPlayCheck.txt";
    const string Running = "Blockout.PlayCheck.Running";
    static readonly List<string> errors = new List<string>();
    static double started;
    static bool captured;
    static BlockoutPlayCheck()
    {
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged += State;
        Application.logMessageReceived += Log;
    }
    static void Log(string message, string trace, LogType type)
    {
        if (SessionState.GetBool(Running, false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
            errors.Add(message + "\n" + trace);
    }
    static void State(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Running, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) { started = EditorApplication.timeSinceStartup; captured = false; }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            var previous = SessionState.GetString("Blockout.PlayCheck.Previous", "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
            SessionState.SetBool(Running, false);
        }
    }
    static void Update()
    {
        if (!SessionState.GetBool(Running, false))
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                { File.WriteAllText(Report, "SKIPPED: open scene has unsaved changes.\n"); return; }
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Blockout/Scenes/Blockout.unity");
            if (!scene) { File.WriteAllText(Report, "FAILED: blockout scene missing.\n"); return; }
            SessionState.SetString("Blockout.PlayCheck.Previous", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetBool(Running, true);
            EditorSceneManager.playModeStartScene = scene;
            errors.Clear(); captured = false;
            EditorApplication.isPlaying = true;
            return;
        }
        if (!EditorApplication.isPlaying || captured || started == 0 || EditorApplication.timeSinceStartup - started < 10) return;
        captured = true;
        try
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerManager>();
            var manager = UnityEngine.Object.FindFirstObjectByType<PlayerPresenceManager>();
            bool valid = player && manager && manager.Player == player.gameObject && player.MainCamera && manager.PlayerIsUnlocked
                && player.transform.position.y > -.5f && player.transform.position.y < .5f;
            File.WriteAllText(Report, (valid && errors.Count == 0 ? "PASS" : "FAIL") + "\n10 second Play Mode startup check\nPlayer unlocked: " + (manager && manager.PlayerIsUnlocked)
                + "\nPlayer position: " + (player ? player.transform.position.ToString("F3") : "missing") + "\nRuntime errors: " + errors.Count + "\n" + string.Join("\n", errors));
            if (player && player.MainCamera)
            {
                var camera = player.MainCamera;
                var target = RenderTexture.GetTemporary(960, 540, 24);
                var previousTarget = camera.targetTexture;
                var previousActive = RenderTexture.active;
                try
                {
                    camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                    var image = new Texture2D(960, 540, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); image.Apply();
                    File.WriteAllBytes("Logs/BlockoutPreview.png", image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image);
                }
                finally { camera.targetTexture = previousTarget; RenderTexture.active = previousActive; RenderTexture.ReleaseTemporary(target); }
            }
        }
        catch (Exception e) { File.AppendAllText(Report, "\n" + e); }
        finally { EditorApplication.isPlaying = false; }
    }
}
