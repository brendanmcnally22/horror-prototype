using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Blockout.Discovery;
using DeadAir.Playtesting;

[InitializeOnLoad]
public static partial class PlaytestHubSetup
{
    const string Root="Assets/DeadAir/PlaytestHub";
    static PlaytestHubSetup(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(!File.Exists("Logs/PlaytestHubSetup.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode)return;
        string action=File.ReadAllText("Logs/PlaytestHubSetup.request").Trim();File.Delete("Logs/PlaytestHubSetup.request");
        try{if(action=="apply")Apply();else if(action=="capture")CaptureVisuals();else Inspect();}catch(Exception e){File.WriteAllText("Logs/PlaytestHubSetupError.txt",e.ToString());Debug.LogException(e);}
    }
    static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
    static object V(Vector3 v)=>new{x=v.x,y=v.y,z=v.z};
    static void Inspect()
    {
        var scene=SceneManager.GetActiveScene();var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var models=new[]{"Assets/CRTMonitor.fbx","Assets/Lever.fbx"}.Select(path=>new{path,objects=AssetDatabase.LoadAssetAtPath<GameObject>(path)?.GetComponentsInChildren<Transform>(true).Select(t=>new{path=PathOf(t),position=V(t.localPosition),scale=V(t.localScale),mesh=t.GetComponent<MeshFilter>()?t.GetComponent<MeshFilter>().sharedMesh.name:null,bounds=t.GetComponent<Renderer>()?new{center=V(t.GetComponent<Renderer>().bounds.center),size=V(t.GetComponent<Renderer>().bounds.size)}:null}).ToArray()}).ToArray();
        var objects=all.Where(t=>t.GetComponent<PrototypeLever>()||t.GetComponent<VirgoTerminal>()||t.name.Equals("lever_7",StringComparison.OrdinalIgnoreCase)||t.name.ToLower().Contains("crt")).Select(t=>new{path=PathOf(t),position=V(t.position),rotation=V(t.eulerAngles),scale=V(t.lossyScale),prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject),components=t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name).ToArray(),children=t.GetComponentsInChildren<Transform>(true).Select(c=>new{path=PathOf(c),local=V(c.localPosition),rotation=V(c.localEulerAngles),scale=V(c.localScale),mesh=c.GetComponent<MeshFilter>()?c.GetComponent<MeshFilter>().sharedMesh.name:null}).ToArray()}).ToArray();
        File.WriteAllText("Logs/PlaytestHubInspect.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=scene.path,dirty=scene.isDirty,models,objects},Newtonsoft.Json.Formatting.Indented));
    }
}
