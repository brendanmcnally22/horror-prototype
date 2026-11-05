using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Blockout.Discovery;

[InitializeOnLoad]
public static class ElevatorWindowSetup
{
    const string DoorPath = "Assets/Blockout/Discovery/Prefabs/Platform Safety/Starting Elevator - Sliding Doors 2.55m.prefab";
    static ElevatorWindowSetup() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (!File.Exists("Logs/ElevatorWindow.request") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete("Logs/ElevatorWindow.request");
        try { Apply(); } catch (Exception e) { File.WriteAllText("Logs/ElevatorWindowResult.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Tools/Blockout/Add Starting Elevator Window")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != DiscoverySetup.ScenePath) throw new Exception("Open Discovery Blank first.");
        var lift = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PrototypeMover>(true)).Single(m => m.name == "STARTING ELEVATOR");
        var prefab = PrefabUtility.LoadPrefabContents(DoorPath);
        try
        {
            var gate = prefab.GetComponent<PlatformSafetyGate>();
            var leaf = gate.RightLeaf;
            if (!leaf.Find("Small observation window"))
            {
                var original = leaf.GetComponent<MeshRenderer>();
                var steel = original.sharedMaterial;
                original.enabled = false;
                float wx = .58f / leaf.localScale.x, wy = .42f / leaf.localScale.y;
                float cy = (1.62f - leaf.localPosition.y) / leaf.localScale.y;
                float bottom = cy - wy / 2, top = cy + wy / 2;
                // Four solid panels leave an actual visible opening in the leaf.
                Part(leaf,"Lower door panel",new Vector3(0,(-.5f+bottom)/2,0),new Vector3(1,bottom+.5f,1),steel);
                Part(leaf,"Upper door panel",new Vector3(0,(.5f+top)/2,0),new Vector3(1,.5f-top,1),steel);
                foreach (int side in new[]{-1,1})
                    Part(leaf,"Side door panel",new Vector3(side*(.5f+wx/2)/2,cy,0),new Vector3((1-wx)/2,wy,1),steel);
                var window = new GameObject("Small observation window"); window.transform.SetParent(leaf,false); window.transform.localPosition = new Vector3(0,cy,0);
                var trim = AssetDatabase.LoadAssetAtPath<Material>("Assets/Blockout/Discovery/Materials/Facility Window Trim.mat");
                var glass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Blockout/Discovery/Materials/Facility Window Glass.mat");
                Part(window.transform,"Glass",Vector3.zero,new Vector3(wx,wy,.12f),glass);
                float tx=.028f/leaf.localScale.x, ty=.028f/leaf.localScale.y;
                foreach(int side in new[]{-1,1})
                {
                    Part(window.transform,"Window side frame",new Vector3(side*wx/2,0,.57f),new Vector3(tx,wy+ty, .18f),trim);
                    Part(window.transform,"Window top or bottom frame",new Vector3(0,side*wy/2,.57f),new Vector3(wx+tx,ty,.18f),trim);
                }
                // Keep the original leaf collider solid, including the glass opening.
                PrefabUtility.SaveAsPrefabAsset(prefab,DoorPath);
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        if (!lift.transform.Find("Shaft - descent reference marks"))
        {
            var marks = new GameObject("Shaft - descent reference marks"); Undo.RegisterCreatedObjectUndo(marks,"Elevator window reference marks"); marks.transform.SetParent(lift.transform,false);
            string path="Assets/Blockout/Discovery/Materials/Elevator Shaft Markings.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",new Color(.5f,.55f,.48f));AssetDatabase.CreateAsset(material,path);}
            float low=Mathf.Min(0,lift.Travel.y)-.5f, high=Mathf.Max(0,lift.Travel.y)+3;
            for(float y=low;y<=high;y+=.85f)Part(marks.transform,"Fixed shaft tick",new Vector3(.6375f,y,-1.72f),new Vector3(.72f,.045f,.025f),material);
            foreach(float x in new[]{.23f,1.045f})Part(marks.transform,"Fixed shaft rail",new Vector3(x,(low+high)/2,-1.75f),new Vector3(.025f,high-low,.025f),material);
        }
        var installed=lift.GetComponentInChildren<PlatformSafetyGate>();
        if(!installed || !installed.RightLeaf.Find("Small observation window"))throw new Exception("Window did not propagate to starting elevator.");
        installed.RightLeaf.GetComponent<MeshRenderer>().enabled=false;
        PrefabUtility.RecordPrefabInstancePropertyModifications(installed.RightLeaf.GetComponent<MeshRenderer>());
        if(!installed.RightLeaf.GetComponent<Collider>().enabled)throw new Exception("Door collision must remain enabled.");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Selection.activeGameObject=installed.RightLeaf.Find("Small observation window").gameObject;
        File.WriteAllText("Logs/ElevatorWindowResult.txt","PASS: 58 x 42 cm glazed window added at 1.62 m height in right starting-elevator sliding door. Door collision and gate controls preserved. Fixed non-colliding shaft markings stay outside carriage to make descent visible. Existing layout preserved.");
        Preview(lift,installed.RightLeaf.Find("Small observation window"));
    }
    static void Preview(PrototypeMover lift,Transform window)
    {
        var go=new GameObject("Temporary elevator window preview");go.hideFlags=HideFlags.HideAndDontSave;
        var camera=go.AddComponent<Camera>();camera.nearClipPlane=.03f;camera.fieldOfView=65;
        camera.transform.position=lift.MovingPart.TransformPoint(new Vector3(0,1.65f,-.15f));
        camera.transform.LookAt(window.position);camera.clearFlags=CameraClearFlags.Skybox;
        var target=RenderTexture.GetTemporary(720,720,24);var previous=RenderTexture.active;
        try
        {
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var picture=new Texture2D(720,720,TextureFormat.RGB24,false);picture.ReadPixels(new Rect(0,0,720,720),0,0);picture.Apply();
            File.WriteAllBytes("Logs/ElevatorWindowPreview.png",picture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(picture);
        }
        finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(go);}
    }
    static GameObject Part(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        var r=g.GetComponent<Renderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return g;
    }
}
