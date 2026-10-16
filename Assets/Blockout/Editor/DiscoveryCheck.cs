using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UHFPS.Runtime;
using Blockout.Discovery;

[InitializeOnLoad]
public static class DiscoveryCheck
{
    const string Key = "DiscoveryCheck.Running";
    static double began;
    static int phase;
    static PrototypeMover elevator;
    static PrototypeMover shuttle;
    static PlayerManager player;
    static Vector3 playerStart;
    static readonly List<string> errors = new List<string>();
    [MenuItem("Tools/Blockout/Check Discovery Mechanics in Play Mode")]
    public static void Request() { File.WriteAllText("Logs/DiscoveryCheck.request", "check"); }
    static DiscoveryCheck()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (!SessionState.GetBool(Key, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode) { began = EditorApplication.timeSinceStartup; phase = 0; errors.Clear(); }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                var previous = SessionState.GetString("DiscoveryCheck.Previous", "");
                EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
                SessionState.SetBool(Key, false);
            }
        };
        Application.logMessageReceived += (message, trace, type) => {
            if (SessionState.GetBool(Key, false) && (type == LogType.Exception || type == LogType.Error)
                && !message.StartsWith("Layout update failed to stabilize")) errors.Add(message);
        };
    }
    static T Spawn<T>(string name) where T : Component
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blockout/Discovery/Prefabs/" + name + ".prefab");
        return UnityEngine.Object.Instantiate(prefab).GetComponent<T>();
    }
    static void Require(bool valid, string message) { if (!valid) throw new Exception(message); }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false))
        {
            if (!File.Exists("Logs/DiscoveryCheck.request") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete("Logs/DiscoveryCheck.request");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var openScene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!openScene.isDirty) continue;
                if (openScene.path == DiscoverySetup.ScenePath) EditorSceneManager.SaveScene(openScene);
                else { File.WriteAllText("Logs/DiscoveryCheck.txt", "SKIPPED: unsaved scene " + openScene.path); return; }
            }
            SessionState.SetString("DiscoveryCheck.Previous", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(DiscoverySetup.ScenePath);
            SessionState.SetBool(Key, true); EditorApplication.isPlaying = true; return;
        }
        if (!EditorApplication.isPlaying || began == 0 || EditorApplication.timeSinceStartup - began < 8 || phase == 4) return;
        try
        {
            if (phase == 0)
            {
                player = UnityEngine.Object.FindAnyObjectByType<PlayerManager>();
                Require(player && player.MainCamera && PlayerPresenceManager.Instance.PlayerIsUnlocked, "Hero wiring/startup failed.");
                Require(player.transform.position.y > -.5f && player.transform.position.y < .5f, "Hero did not settle on foundation.");
                var panel = Spawn<PrototypeFusePanel>("Fuse Return Panel - One per Map"); panel.transform.position = new Vector3(15, 1, 15);
                var loop = panel.Loop;
                var light = Spawn<PrototypeLight>("Light - Red Flicker");
                var door = Spawn<PrototypeMover>("Door - Sliding"); door.transform.position = new Vector3(15, 0, 10);
                loop.FirstReturnLights = new[] { light }; loop.SecondReturnShortcuts = new[] { door }; loop.FinalReturnDoors = new[] { door };
                int first = 0, second = 0, completed = 0;
                loop.OnFirstReturned.AddListener(() => first++); loop.OnSecondReturned.AddListener(() => second++); loop.OnCompleted.AddListener(() => completed++);
                var a = Spawn<InteractableItem>("Fuse - Optional Pickup");
                var b = Spawn<InteractableItem>("Fuse - Optional Pickup");
                var c = Spawn<InteractableItem>("Fuse - Optional Pickup");
                Require(!loop.ReturnOne(), "Empty deposit accepted.");
                a.InteractWithObject(); Require(loop.Carried == 1 && !a.gameObject.activeSelf, "Native fuse inventory pickup failed.");
                Require(loop.ReturnOne() && first == 1 && !light.Flicker && light.Color == Color.white, "First-return reward failed.");
                b.InteractWithObject(); c.InteractWithObject(); Require(loop.Carried == 2, "Multiple native fuses could not be carried.");
                Require(loop.ReturnOne() && loop.Carried == 1 && second == 1 && door.TargetEnd, "Second-return reward or single-fuse consumption failed.");
                Require(loop.ReturnOne() && loop.Carried == 0 && completed == 1 && !loop.ReturnOne() && completed == 1, "Completion protection failed.");
                var lever = Spawn<PrototypeLever>("Lever"); lever.transform.position = Vector3.one * 15;
                door.Close();
                lever.Targets = new[] { door }; lever.InteractStart(); Require(door.TargetEnd, "Lever open failed.");
                lever.InteractStart(); Require(!door.TargetEnd, "Lever close failed.");
                elevator = Spawn<PrototypeMover>("Platform - Elevator"); elevator.Travel = Vector3.up; elevator.Speed = 1;
                elevator.transform.position = player.transform.position;
                player.transform.position += Vector3.up * .25f;
                shuttle = Spawn<PrototypeMover>("Platform - Side to Side"); shuttle.transform.position = new Vector3(10, 0, 0); shuttle.Travel = Vector3.right;
                phase = 1; began = EditorApplication.timeSinceStartup - 6; return;
            }
            if (phase == 1)
            {
                Require(player.transform.parent == elevator.MovingPart, "Hero did not board rider trigger.");
                playerStart = player.transform.position;
                elevator.Open(); shuttle.Open(); phase = 2; began = EditorApplication.timeSinceStartup - 5; return;
            }
            if (phase == 2)
            {
                Require(Vector3.Distance(elevator.MovingPart.localPosition, Vector3.up) < .01f, "Elevator travel failed.");
                Require(Vector3.Distance(shuttle.MovingPart.localPosition, Vector3.right) < .01f, "Horizontal travel failed.");
                Require(player.transform.position.y - playerStart.y > .8f, "Hero did not ride elevator.");
                player.transform.position = new Vector3(0,.1f,2);
                phase = 3; began = EditorApplication.timeSinceStartup - 6; return;
            }
            var display = UnityEngine.Object.FindAnyObjectByType<DiscoveryObjectiveDisplay>();
            Require(display && display.Text.gameObject.activeSelf && display.Text.text.Contains("fuses"), "Objective walk-through trigger failed.");
            Require(errors.Count == 0, string.Join("\n", errors));
            Finish("PASS: hero startup, foundation, native horror-pack inventory fuse pickups, multiple carried fuses, one-at-a-time consumption, three return rewards, repeat completion protection, lever open/close, vertical and horizontal travel, hero elevator riding, objective walk-through trigger. Unity toolbar layout errors excluded; other runtime errors: 0.\n");
        }
        catch (Exception e) { Finish("FAIL: " + e + "\n" + string.Join("\n", errors)); }
    }
    static void Finish(string result)
    {
        phase = 4; File.WriteAllText("Logs/DiscoveryCheck.txt", result); EditorApplication.isPlaying = false;
    }
}
