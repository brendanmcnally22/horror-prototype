using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UHFPS.Runtime;

// Editor-only authoring utility. Never overwrites an existing blockout scene.
[InitializeOnLoad]
public static class BlockoutSetup
{
    const string Root = "Assets/Blockout";
    const string ScenePath = Root + "/Scenes/Blockout.unity";
    const string Kit = "Assets/ThunderWire Studio/UHFPS/Content";
    static BlockoutSetup() { EditorApplication.delayCall += FirstImport; }

    static void FirstImport()
    {
        if (File.Exists(ScenePath) || SessionState.GetBool("BlockoutSetup.Attempted", false)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        SessionState.SetBool("BlockoutSetup.Attempted", true);
        Create();
    }

    [MenuItem("Tools/Blockout/Create Starter Assets")]
    public static void Create()
    {
        if (File.Exists(ScenePath)) { Debug.Log("Blockout scene already exists; left unchanged."); return; }
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            foreach (var folder in new[] { "Scenes", "Materials", "Meshes", "Prefabs/Building", "Prefabs/Setup", "Prefabs/Kit" })
                Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            var floorMat = Material("Floor - Grey", new Color(.38f, .42f, .46f));
            var wallMat = Material("Walls - Light Grey", new Color(.7f, .73f, .75f));
            var accentMat = Material("Landmarks - Orange", new Color(.9f, .4f, .12f));
            SaveBox("Floor_4x4", new Vector3(4, .25f, 4), new Vector3(0, -.125f, 0), floorMat);
            SaveBox("Floor_8x8", new Vector3(8, .25f, 8), new Vector3(0, -.125f, 0), floorMat);
            SaveBox("Foundation_40x40", new Vector3(40, .5f, 40), new Vector3(0, -.25f, 0), floorMat);
            SaveBox("Wall_4x3", new Vector3(4, 3, .25f), new Vector3(0, 1.5f, 0), wallMat);
            SaveBox("Wall_2x3", new Vector3(2, 3, .25f), new Vector3(0, 1.5f, 0), wallMat);
            SaveBox("Ceiling_4x4", new Vector3(4, .25f, 4), new Vector3(0, 3.125f, 0), wallMat);
            SaveBox("Block_1m", Vector3.one, new Vector3(0, .5f, 0), accentMat);
            SaveBox("Pillar_0.5x3", new Vector3(.5f, 3, .5f), new Vector3(0, 1.5f, 0), wallMat);
            SaveBox("Cover_2x1", new Vector3(2, 1, .5f), new Vector3(0, .5f, 0), accentMat);

            var doorway = new GameObject("Doorway_4x3_Opening1.5x2.25");
            Box(doorway, "Left", new Vector3(1.25f, 3, .25f), new Vector3(-1.375f, 1.5f, 0), wallMat);
            Box(doorway, "Right", new Vector3(1.25f, 3, .25f), new Vector3(1.375f, 1.5f, 0), wallMat);
            Box(doorway, "Header", new Vector3(1.5f, .75f, .25f), new Vector3(0, 2.625f, 0), wallMat);
            SaveBuilding(doorway);
            var stairs = new GameObject("Stairs_2Wide_3High_6Long");
            for (int i = 0; i < 15; i++)
            {
                float height = (i + 1) * .2f;
                Box(stairs, "Step_" + (i + 1), new Vector3(2, height, .4f), new Vector3(0, height / 2, .2f + i * .4f), wallMat);
            }
            SaveBuilding(stairs);
            MakeRamp(wallMat);

            var setup = new GameObject("Player Setup - ONE per scene");
            var manager = Instantiate(Kit + "/Resources/Setup/GAMEMANAGER.prefab", scene);
            var player = Instantiate(Kit + "/Resources/Setup/HEROPLAYER.prefab", scene);
            // Match the kit's official setup tool, then package both objects together
            // so their cross-references survive dragging the prefab into another scene.
            PrefabUtility.UnpackPrefabInstance(manager, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            PrefabUtility.UnpackPrefabInstance(player, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            manager.transform.SetParent(setup.transform, false);
            player.transform.SetParent(setup.transform, false);
            manager.transform.localPosition = Vector3.zero;
            player.transform.SetLocalPositionAndRotation(new Vector3(0, .1f, 0), Quaternion.identity);
            player.GetComponent<PlayerManager>().MainCamera = manager.GetComponentInChildren<Camera>(true);
            manager.GetComponent<PlayerPresenceManager>().Player = player;
            manager.GetComponent<PlayerPresenceManager>().PlayerUnlockType = PlayerPresenceManager.UnlockType.Automatically;
            PrefabUtility.SaveAsPrefabAssetAndConnect(setup, Root + "/Prefabs/Setup/Player Setup.prefab", InteractionMode.AutomatedAction);

            var geometry = new GameObject("BLOCKOUT - Place geometry here");
            var ground = Instantiate(Root + "/Prefabs/Building/Foundation_40x40.prefab", scene);
            ground.transform.SetParent(geometry.transform, false);
            var lighting = new GameObject("LIGHTING");
            var sun = new GameObject("Directional Light");
            sun.transform.SetParent(lighting.transform, false);
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.shadows = LightShadows.Soft;
            RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.55f, .58f, .63f);
            RenderSettings.fog = false;
            var lamp = new GameObject("Point Light - Blockout");
            var point = lamp.AddComponent<Light>();
            point.type = LightType.Point;
            point.range = 10;
            point.intensity = 3;
            PrefabUtility.SaveAsPrefabAsset(lamp, Root + "/Prefabs/Setup/Point Light.prefab");
            UnityEngine.Object.DestroyImmediate(lamp);

            int count = CreateKitVariants(scene);
            Validate(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Root + "/Setup Report.txt", "Created " + DateTime.Now.ToString("s") + "\nScene: " + ScenePath + "\nKit prefab variants: " + count + "\nValidated: player/camera references, one player, one manager, one enabled camera, ground collider, missing scripts.\nPlay-mode testing is separate from this authoring check.\n");
            Debug.Log("Blockout ready: " + ScenePath + "; " + count + " kit prefabs. See Assets/Blockout/README.md.");
        }
        catch (Exception e) { File.WriteAllText(Root + "/Setup Error.txt", e.ToString()); Debug.LogException(e); }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.Refresh();
        }
    }

    static GameObject Instantiate(string path, Scene scene)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!asset) throw new InvalidOperationException("Missing prefab: " + path);
        return (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
    }

    static Material Material(string name, Color color)
    {
        var path = Root + "/Materials/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing) return existing;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) throw new InvalidOperationException("URP Lit shader missing.");
        var material = new Material(shader) { name = name };
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", .1f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static void Box(GameObject parent, string name, Vector3 size, Vector3 position, Material material)
    {
        var child = GameObject.CreatePrimitive(PrimitiveType.Cube);
        child.name = name;
        child.layer = LayerMask.NameToLayer("Ground");
        child.transform.SetParent(parent.transform, false);
        child.transform.localPosition = position;
        child.transform.localScale = size;
        child.GetComponent<Renderer>().sharedMaterial = material;
    }

    static void SaveBox(string name, Vector3 size, Vector3 position, Material material)
    {
        var go = new GameObject(name);
        Box(go, "Geometry", size, position, material);
        SaveBuilding(go);
    }

    static void SaveBuilding(GameObject go)
    {
        PrefabUtility.SaveAsPrefabAsset(go, Root + "/Prefabs/Building/" + go.name + ".prefab");
        UnityEngine.Object.DestroyImmediate(go);
    }

    static void MakeRamp(Material material)
    {
        var path = Root + "/Meshes/Ramp.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!mesh)
        {
            mesh = new Mesh { name = "Ramp 2x3x6" };
            mesh.vertices = new[] { new Vector3(-1,0,0), new Vector3(1,0,0), new Vector3(-1,0,6), new Vector3(1,0,6), new Vector3(-1,3,6), new Vector3(1,3,6) };
            mesh.triangles = new[] { 0,4,1, 1,4,5, 0,2,4, 1,5,3, 2,3,5, 2,5,4, 0,1,3, 0,3,2 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
        }
        var go = new GameObject("Ramp_2Wide_3High_6Long");
        go.layer = LayerMask.NameToLayer("Ground");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
        SaveBuilding(go);
    }

    static int CreateKitVariants(Scene scene)
    {
        int count = 0;
        foreach (string category in new[] { "Props", "Interact", "NPC", "CCTV", "Particles", "Misc" })
        {
            string sourceRoot = Kit + "/Prefabs/" + category;
            foreach (var source in Directory.GetFiles(sourceRoot, "*.prefab", SearchOption.AllDirectories))
            {
                string normalized = source.Replace('\\', '/');
                string destination = Root + "/Prefabs/Kit/" + normalized.Substring((Kit + "/Prefabs/").Length);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                AssetDatabase.Refresh();
                if (!File.Exists(destination))
                {
                    var instance = Instantiate(normalized, scene);
                    PrefabUtility.SaveAsPrefabAsset(instance, destination);
                    UnityEngine.Object.DestroyImmediate(instance);
                }
                count++;
            }
        }
        return count;
    }

    static void Validate(Scene scene)
    {
        var components = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        if (components.Any(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0))
            throw new InvalidOperationException("Missing scripts in blockout setup.");
        var players = components.Select(t => t.GetComponent<PlayerManager>()).Where(p => p).ToArray();
        var managers = components.Select(t => t.GetComponent<PlayerPresenceManager>()).Where(p => p).ToArray();
        if (players.Length != 1 || managers.Length != 1 || managers[0].Player != players[0].gameObject || !players[0].MainCamera)
            throw new InvalidOperationException("Invalid player/manager wiring.");
        if (components.Select(t => t.GetComponent<Camera>()).Count(c => c && c.enabled && c.gameObject.activeInHierarchy) != 1)
            throw new InvalidOperationException("Expected one enabled camera.");
        Physics.SyncTransforms();
        if (!Physics.Raycast(new Vector3(0, 2, 0), Vector3.down, out _, 3, 1 << LayerMask.NameToLayer("Ground")))
            throw new InvalidOperationException("Missing ground at spawn.");
    }
}
