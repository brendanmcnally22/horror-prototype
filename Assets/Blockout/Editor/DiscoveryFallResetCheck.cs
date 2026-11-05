using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UHFPS.Runtime;
using Blockout.Discovery;

[InitializeOnLoad]
public static class DiscoveryFallResetCheck
{
    const string Key = "DiscoveryFallResetCheck";
    static double began, step;
    static int phase, loads;
    static Vector3 spawn;
    static DiscoveryFallReset trigger;
    static DiscoveryFallResetCheck()
    {
        EditorApplication.update += Tick;
        SceneManager.sceneLoaded += (s, m) => { if (SessionState.GetBool(Key, false)) loads++; };
        EditorApplication.playModeStateChanged += s =>
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (s == PlayModeStateChange.EnteredPlayMode) { began = EditorApplication.timeSinceStartup; phase = 0; loads = 0; Application.runInBackground = true; }
            if (s == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Key, false);
                string path = SessionState.GetString(Key + "Previous", "");
                EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            }
        };
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false))
        {
            if (!File.Exists("Logs/DiscoveryFallResetCheck.request") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete("Logs/DiscoveryFallResetCheck.request");
            SessionState.SetString(Key + "Previous", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            EditorSceneManager.playModeStartScene = null; SessionState.SetBool(Key, true); EditorApplication.isPlaying = true; return;
        }
        if (!EditorApplication.isPlaying || began == 0 || phase == 99) return;
        try
        {
            if (EditorApplication.timeSinceStartup - began > 60) throw new Exception("Fall-reset test timed out.");
            if (phase == 0)
            {
                if (EditorApplication.timeSinceStartup - began < 7) return;
                trigger = UnityEngine.Object.FindAnyObjectByType<DiscoveryFallReset>();
                if (!trigger || !PlayerManager.Instance) throw new Exception("Trigger or player missing.");
                spawn = PlayerManager.Instance.transform.position;
                var dummy = GameObject.CreatePrimitive(PrimitiveType.Cube); dummy.name = "Temporary non-player fall test"; dummy.transform.position = trigger.transform.position;
                dummy.AddComponent<Rigidbody>().useGravity = false; step = EditorApplication.timeSinceStartup; phase = 1; return;
            }
            if (phase == 1)
            {
                if (EditorApplication.timeSinceStartup - step < 1) return;
                if (loads != 0) throw new Exception("Non-player object restarted the level.");
                UnityEngine.Object.FindAnyObjectByType<DiscoveryLoop>().RecordPackFuse(0);
                // Put the real controller above the volume and let it fall through the trigger.
                PlayerPresenceManager.Instance.Teleport(trigger.transform.position + Vector3.up * 4.5f);
                step = EditorApplication.timeSinceStartup; phase = 2; return;
            }
            if (phase == 2)
            {
                if (loads == 0) return;
                step = EditorApplication.timeSinceStartup; phase = 3; return;
            }
            if (phase == 3)
            {
                if (EditorApplication.timeSinceStartup - step < 7) return;
                if (Vector3.Distance(PlayerManager.Instance.transform.position, spawn) > .3f) throw new Exception("Reloaded player did not return to discovery starting position.");
                if (UnityEngine.Object.FindAnyObjectByType<DiscoveryLoop>().Returned != 0) throw new Exception("Discovery progress did not reset.");
                Finish("PASS: non-player physics entry ignored; actual player fell into volume; scene reloaded; hero returned to authored start and discovery fuse progress reset to zero.");
            }
        }
        catch (Exception e) { Finish("FAIL: " + e.Message); }
    }
    static void Finish(string result) { phase = 99; File.WriteAllText("Logs/DiscoveryFallResetCheck.txt", result); EditorApplication.isPlaying = false; }
}
