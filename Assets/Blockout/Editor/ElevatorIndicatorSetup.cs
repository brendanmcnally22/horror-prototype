using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Blockout.Discovery;

[InitializeOnLoad]
public static class ElevatorIndicatorSetup
{
    const string PrefabPath = "Assets/Blockout/Discovery/Prefabs/Platform Safety/Elevator - Lever Floor Indicator.prefab";
    static ElevatorIndicatorSetup() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (!File.Exists("Logs/ElevatorIndicator.request") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete("Logs/ElevatorIndicator.request");
        try { Apply(); } catch (Exception e) { File.WriteAllText("Logs/ElevatorIndicatorResult.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Tools/Blockout/Update Starting Elevator Lights")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        var lift = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PrototypeMover>(true)).Single(m => m.name == "STARTING ELEVATOR");
        var lever = lift.GetComponentsInChildren<PrototypeLever>(true).Single(l => l.Targets.Contains(lift));
        var existing = lift.GetComponentInChildren<ElevatorFloorIndicator>();
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            // Rebuild only our indicator prefab, preserving the scene's placement and lift.
            foreach (Transform child in root.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            var indicator = root.GetComponent<ElevatorFloorIndicator>();
            var metal = MakeMaterial("Indicator dark steel", "Universal Render Pipeline/Lit", new Color(.09f,.10f,.11f));
            var upper = MakeMaterial("Indicator upper lens", "Universal Render Pipeline/Unlit", indicator.ArrivalColor);
            var lower = MakeMaterial("Indicator lower lens", "Universal Render Pipeline/Unlit", indicator.IdleColor);
            indicator.UpperLight = Circle(root,"Upper light",.065f,metal,upper);
            indicator.LowerLight = Circle(root,"Lower light",-.065f,metal,lower);
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        if (!existing)
        {
            var placed=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),lift.MovingPart);
            Undo.RegisterCreatedObjectUndo(placed,"Add elevator lights");
            placed.transform.localPosition=new Vector3(-1.20f,2.02f,-.75f);
            placed.transform.localRotation=Quaternion.Euler(0,-90,0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(placed.transform);
            existing=placed.GetComponent<ElevatorFloorIndicator>();
        }
        existing.Elevator=lift;existing.Lever=lever;
        PrefabUtility.RecordPrefabInstancePropertyModifications(existing);
        if(existing.GetComponentsInChildren<TextMesh>(true).Length!=0)throw new Exception("Unexpected text remains in indicator.");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Selection.activeGameObject=existing.gameObject;
        File.WriteAllText("Logs/ElevatorIndicatorResult.txt","PASS: two small round lights only; no text, numbers, arrows, or display panel. Existing lever controls floor indication. Existing scene placement preserved.");
    }
    static Material MakeMaterial(string name,string shader,Color color)
    {
        string path="Assets/Blockout/Discovery/Materials/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material)return material;
        material=new Material(Shader.Find(shader));material.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(material,path);return material;
    }
    static Renderer Circle(GameObject parent,string name,float y,Material bezel,Material lens)
    {
        Disc(parent,name+" rim",new Vector3(0,y,-.012f),.09f,.009f,bezel);
        return Disc(parent,name,new Vector3(0,y,-.025f),.068f,.007f,lens);
    }
    static Renderer Disc(GameObject parent,string name,Vector3 position,float diameter,float depth,Material material)
    {
        var shape=GameObject.CreatePrimitive(PrimitiveType.Cylinder);shape.name=name;shape.transform.SetParent(parent.transform,false);shape.transform.localPosition=position;
        shape.transform.localRotation=Quaternion.Euler(90,0,0);shape.transform.localScale=new Vector3(diameter,depth,diameter);
        UnityEngine.Object.DestroyImmediate(shape.GetComponent<Collider>());
        var renderer=shape.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return renderer;
    }
}
