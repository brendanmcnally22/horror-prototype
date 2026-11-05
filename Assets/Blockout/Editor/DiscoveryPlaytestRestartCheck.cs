using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UHFPS.Runtime;
using Blockout.Discovery;

[InitializeOnLoad]
public static class DiscoveryPlaytestRestartCheck
{
    const string Key = "DiscoveryPlaytestRestartCheck";
    static double began, step;
    static int phase, loads, inserted;
    static Vector3 spawn;
    static DiscoveryPlaytestRestart restart;
    static FacilityPanelUse panel;
    static InteractableItem[] fuses;
    static DiscoveryPlaytestRestartCheck()
    {
        EditorApplication.update += Tick;
        SceneManager.sceneLoaded += (s, m) => { if (SessionState.GetBool(Key, false)) loads++; };
        EditorApplication.playModeStateChanged += s =>
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (s == PlayModeStateChange.EnteredPlayMode) { began = EditorApplication.timeSinceStartup; phase = 0; inserted = 0; loads = 0; Application.runInBackground = true; }
            if (s == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Key, false);
                string previous = SessionState.GetString(Key + "Previous", "");
                EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
            }
        };
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false))
        {
            if (!File.Exists("Logs/DiscoveryPlaytestRestartCheck.request") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete("Logs/DiscoveryPlaytestRestartCheck.request");
            SessionState.SetString(Key + "Previous", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            EditorSceneManager.playModeStartScene = null; SessionState.SetBool(Key, true); EditorApplication.isPlaying = true; return;
        }
        if (!EditorApplication.isPlaying || began == 0 || phase == 99) return;
        try
        {
            if (EditorApplication.timeSinceStartup - began > 60) throw new Exception("Completion restart check timed out.");
            if (phase == 0)
            {
                if (EditorApplication.timeSinceStartup - began < 7) return;
                restart = UnityEngine.Object.FindAnyObjectByType<DiscoveryPlaytestRestart>();
                panel = UnityEngine.Object.FindAnyObjectByType<FacilityPanelUse>();
                fuses = UnityEngine.Object.FindObjectsByType<InteractableItem>().Where(f => f.name.StartsWith("Fuse - Optional Pickup")).ToArray();
                if (!restart || !panel || fuses.Length != 3) throw new Exception("Missing restart UI, native panel, or three existing fuses.");
                spawn = PlayerManager.Instance.transform.position;
                if (restart.IsOpen) throw new Exception("Restart prompt appeared before completion.");
                panel.InteractStart(); phase = 1; step = EditorApplication.timeSinceStartup; return;
            }
            if (phase == 1)
            {
                if (EditorApplication.timeSinceStartup - step < .5) return;
                if (inserted < 3)
                {
                    if (restart.IsOpen) throw new Exception("Restart prompt appeared before the third fuse.");
                    fuses[inserted].InteractWithObject(); panel.InteractStart(); inserted++;
                    if (restart.Loop.Returned != inserted) throw new Exception("Native fuse insertion failed.");
                    step = EditorApplication.timeSinceStartup; return;
                }
                if (!restart.IsOpen || !Cursor.visible || Cursor.lockState != CursorLockMode.None) throw new Exception("Completion prompt/cursor after third fuse: open="+restart.IsOpen+", visible="+Cursor.visible+", lock="+Cursor.lockState+", paused="+(GameManager.HasReference&&GameManager.Instance.IsPaused));
                restart.ContinueExploring();
                if (restart.IsOpen) throw new Exception("Keep Exploring failed to close the prompt.");
                restart.Open(); step = EditorApplication.timeSinceStartup; phase = 2; return;
            }
            if (phase == 2)
            {
                if (EditorApplication.timeSinceStartup - step < 3) return;
                restart.RestartPlaytest(); phase = 3; return;
            }
            if (phase == 3)
            {
                if (loads == 0) return;
                step = EditorApplication.timeSinceStartup; phase = 4; return;
            }
            if (phase == 4)
            {
                if (EditorApplication.timeSinceStartup - step < 7) return;
                var fresh = UnityEngine.Object.FindAnyObjectByType<DiscoveryPlaytestRestart>();
                if (!fresh || fresh.IsOpen || fresh.Loop.Returned != 0 || Vector3.Distance(PlayerManager.Instance.transform.position, spawn) > .3f) throw new Exception("Restart did not reset the player, fuse progress, and completion UI.");
                Finish("PASS: native panel accepted the three existing pickups; prompt hidden for first two returns; third return opened clickable completion prompt; Keep Exploring closed it; Restart Playtest reloaded scene at original spawn with zero fuses returned and prompt hidden.");
            }
        }
        catch (Exception e) { Finish("FAIL: " + e.Message); }
    }
    static void Finish(string result) { phase = 99; File.WriteAllText("Logs/DiscoveryPlaytestRestartCheck.txt", result); EditorApplication.isPlaying = false; }
}
