using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Blockout.Discovery;
using UHFPS.Runtime;

[InitializeOnLoad]
public static class DiscoverySetup
{
    const string Root = "Assets/Blockout/Discovery";
    public const string ScenePath = Root + "/Scenes/Discovery Blank.unity";
    static Material grey, orange;
    static DiscoverySetup()
    {
        EditorApplication.delayCall += () => {
            if (!File.Exists("Logs/DiscoverySetup.request") || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete("Logs/DiscoverySetup.request");
            try { Create(); } catch (Exception e) { File.WriteAllText("Logs/DiscoverySetup.txt", e.ToString()); Debug.LogException(e); }
        };
    }

    [MenuItem("Tools/Blockout/Create Blank Discovery Map and Mechanics")]
    public static void Create()
    {
        if (File.Exists(ScenePath)) { Debug.Log("Discovery map exists; your edits are preserved."); return; }
        foreach (var folder in new[] { "/Scenes", "/Prefabs", "/Materials" }) Directory.CreateDirectory(Root + folder);
        AssetDatabase.Refresh();
        var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        try
        {
            grey = Mat("Neutral", new Color(.4f, .45f, .5f));
            orange = Mat("Interaction", new Color(1, .55f, .08f));
            foreach (var color in new[] { Color.red, Color.yellow, Color.green, Color.white })
            {
                string name = color == Color.red ? "Red Flicker" : color == Color.yellow ? "Yellow Flicker" : color == Color.green ? "Green Flicker" : "White Steady";
                var lamp = new GameObject("Light - " + name);
                lamp.AddComponent<Light>();
                var tune = lamp.AddComponent<PrototypeLight>();
                tune.Color = color; tune.Flicker = color != Color.white;
                var light = lamp.GetComponent<Light>(); light.color = color; light.intensity = tune.Intensity; light.range = tune.Range;
                Save(lamp);
            }
            MakeMover("Door - Sliding", new Vector3(2, 3, .3f), new Vector3(0, 1.5f, 0), new Vector3(0, 3.2f, 0), false);
            MakeMover("Platform - Elevator", new Vector3(3, .3f, 3), new Vector3(0, -.15f, 0), new Vector3(0, 3, 0), true);
            MakeMover("Platform - Side to Side", new Vector3(3, .3f, 3), new Vector3(0, -.15f, 0), new Vector3(6, 0, 0), true);
            var lever = new GameObject("Lever"); lever.layer = 6;
            var collider = lever.AddComponent<BoxCollider>(); collider.center = new Vector3(0, 1, 0); collider.size = new Vector3(.6f, .8f, .5f);
            Box(lever.transform, "Pedestal", new Vector3(.5f, 1, .4f), new Vector3(0, .5f, 0), grey, false);
            var pivot = new GameObject("Handle pivot").transform; pivot.SetParent(lever.transform); pivot.localPosition = Vector3.up;
            Box(pivot, "Handle", new Vector3(.12f, .5f, .12f), new Vector3(0, .25f, 0), orange, false);
            lever.AddComponent<PrototypeLever>().Handle = pivot; Save(lever);
            var fuse = new GameObject("Fuse - Optional Pickup"); fuse.layer = 6;
            fuse.AddComponent<BoxCollider>().size = new Vector3(.3f, .2f, .6f);
            Box(fuse.transform, "Fuse body", new Vector3(.22f, .16f, .5f), Vector3.zero, orange, false);
            Box(fuse.transform, "Cap A", new Vector3(.26f, .2f, .08f), new Vector3(0, 0, -.25f), grey, false);
            Box(fuse.transform, "Cap B", new Vector3(.26f, .2f, .08f), new Vector3(0, 0, .25f), grey, false);
            fuse.AddComponent<PrototypeFuse>(); Save(fuse);
            var panel = new GameObject("Fuse Return Panel - One per Map"); panel.layer = 6;
            panel.AddComponent<BoxCollider>().size = new Vector3(1.5f, 1, .3f);
            Box(panel.transform, "Panel housing", new Vector3(1.5f, 1, .3f), Vector3.zero, grey, false);
            for (int i = 0; i < 3; i++) Box(panel.transform, "Socket " + (i + 1), new Vector3(.25f, .5f, .08f), new Vector3((i - 1) * .4f, 0, -.19f), orange, false);
            panel.AddComponent<PrototypeFusePanel>().Loop = panel.AddComponent<DiscoveryLoop>(); Save(panel);

            var setup = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blockout/Prefabs/Setup/Player Setup.prefab");
            if (!setup) throw new Exception("Existing wired hero setup prefab missing.");
            var hero = (GameObject)PrefabUtility.InstantiatePrefab(setup, scene);
            hero.GetComponentInChildren<PlayerManager>().transform.position = new Vector3(0, .1f, 0);
            var geometry = new GameObject("MAP - Build your area here");
            Box(geometry.transform, "Blank foundation 40 x 40", new Vector3(40, .5f, 40), new Vector3(0, -.25f, 0), grey, true);
            var sun = new GameObject("White work light").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.2f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.sun = sun; RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.5f, .53f, .58f); RenderSettings.fog = false;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/DiscoverySetup.txt", "PASS: blank map created with wired hero, foundation and white light. Ten mechanics prefabs authored separately. Existing map untouched.\n");
        }
        finally
        {
            if (previous.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }
    static void MakeMover(string name, Vector3 size, Vector3 position, Vector3 travel, bool rideable)
    {
        var root = new GameObject(name);
        var moving = Box(root.transform, "Moving part", size, position, grey, true);
        // Geometry is a child: the moving transform must remain at scale one.
        var carriage = new GameObject("Carriage").transform; carriage.SetParent(root.transform, false);
        moving.transform.SetParent(carriage, true);
        var mover = root.AddComponent<PrototypeMover>(); mover.MovingPart = carriage; mover.Travel = travel;
        if (rideable)
        {
            var trigger = new GameObject("Rider zone"); trigger.transform.SetParent(carriage, false);
            var box = trigger.AddComponent<BoxCollider>(); box.isTrigger = true; box.center = new Vector3(0, .4f, 0); box.size = new Vector3(2.95f, .8f, 2.95f);
            var body = trigger.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            trigger.AddComponent<PrototypeRider>().Platform = carriage;
        }
        Save(root);
    }
    static GameObject Box(Transform parent, string name, Vector3 size, Vector3 position, Material material, bool collision)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.layer = 11;
        go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = material;
        if (!collision) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    static Material Mat(string name, Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(material, Root + "/Materials/" + name + ".mat"); return material;
    }
    static void Save(GameObject go)
    {
        PrefabUtility.SaveAsPrefabAsset(go, Root + "/Prefabs/" + go.name + ".prefab"); UnityEngine.Object.DestroyImmediate(go);
    }
}
