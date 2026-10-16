using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Blockout.Discovery;
using UHFPS.Runtime;

[InitializeOnLoad]
public static partial class FacilityDiscoverySetup
{
    const string Request = "Logs/FacilityDiscovery.request";
    static FacilityDiscoverySetup() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string action = File.ReadAllText(Request).Trim(); File.Delete(Request);
        try { if(action == "apply") Apply(); else Inspect(); }
        catch (Exception e) { File.WriteAllText("Logs/FacilityDiscoveryError.txt", e.ToString()); Debug.LogException(e); }
    }
    static Scene Scene()
    {
        var scene = SceneManager.GetSceneByPath(DiscoverySetup.ScenePath);
        if (!scene.isLoaded) throw new Exception("Open Discovery Blank first. No scene was changed.");
        return scene;
    }
    static string Path(Transform t) => t.parent ? Path(t.parent) + "/" + t.name : t.name;
    static object Vec(Vector3 v) => new { x=v.x,y=v.y,z=v.z };
    static void Inspect()
    {
        var scene = Scene();
        var transforms = scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
        var rows=transforms.Where(t=>!t.GetComponentInParent<PlayerManager>() && !Path(t).Contains("GAMEMANAGER") &&
            (t.GetComponent<PrototypeMover>() || t.GetComponent<PrototypeLever>() || t.GetComponent<Light>() || t.GetComponent<DiscoveryLoop>() ||
             t.GetComponent<DiscoveryObjectiveDisplay>() || t.GetComponent<DiscoveryObjectiveTrigger>() ||
             t.name.ToLowerInvariant().Contains("frame") || t.name.ToLowerInvariant().Contains("storage") || t.name.ToLowerInvariant().Contains("security")))
            .Select(t=>new {
                path=Path(t), id=GlobalObjectId.GetGlobalObjectIdSlow(t).ToString(), position=Vec(t.position), localPosition=Vec(t.localPosition), scale=Vec(t.localScale),
                components=t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name).ToArray(),
                children=t.Cast<Transform>().Select(c=>new {name=c.name,position=Vec(c.localPosition),scale=Vec(c.localScale),
                    renderer=c.GetComponent<Renderer>() ? new {center=Vec(c.GetComponent<Renderer>().bounds.center),size=Vec(c.GetComponent<Renderer>().bounds.size)}:null}).ToArray(),
                targets=t.GetComponent<PrototypeLever>() ? t.GetComponent<PrototypeLever>().Targets.Where(x=>x).Select(x=>Path(x.transform)).ToArray():null,
                travel=t.GetComponent<PrototypeMover>() ? Vec(t.GetComponent<PrototypeMover>().Travel):null
            }).ToArray();
        File.WriteAllText("Logs/FacilityDiscoveryInspect.json",JsonConvert.SerializeObject(new{scene=scene.path,dirty=scene.isDirty,transforms=transforms.Length,objects=rows},Formatting.Indented));
    }
}
