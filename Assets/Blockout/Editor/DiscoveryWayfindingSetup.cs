using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Blockout.Discovery;
using UHFPS.Runtime;

public static partial class DiscoveryWayfindingSetup
{
    const string ScenePath="Assets/Blockout/Discovery/Scenes/Discovery Blank.unity";
    static string Log(string name)=>Application.dataPath+"/../Logs/"+name;
    static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
    static string Hierarchy(Transform t)=>t.parent?Hierarchy(t.parent)+"/"+t.name:t.name;
    [MenuItem("Tools/Blockout/Audit Discovery Wayfinding")]
    public static void Audit()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!=ScenePath)throw new Exception("Open Discovery Blank first.");
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        File.WriteAllText(Log("WayfindingAudit.json"),JsonConvert.SerializeObject(all.Where(t=>
            t.GetComponent<PrototypeMover>()||t.GetComponent<PlatformSafetyGate>()||t.GetComponent<FlashlightItem>()||
            t.GetComponent<PlayerItemsManager>()||t.GetComponent<PlayerManager>()||
            (!t.GetComponentInParent<PlayerManager>()&&!Hierarchy(t).Contains("GAMEMANAGER")&&
            (t.name.ToLower().Contains("room")||t.name.ToLower().Contains("hall")||t.name.ToLower().Contains("shuttle")||
            t.name.ToLower().Contains("admin")||t.name.ToLower().Contains("storage")||t.name.ToLower().Contains("door")||t.name.ToLower().Contains("sign"))))
            .Select(t=>new{path=Hierarchy(t),position=V(t.position),rotation=V(t.eulerAngles),scale=V(t.lossyScale),
                components=t.GetComponents<Component>().Where(c=>c&&!(c is Transform)&&!(c is MeshFilter)&&!(c is Renderer)).Select(c=>new{type=c.GetType().Name,json=EditorJsonUtility.ToJson(c)}),
                children=t.Cast<Transform>().Select(c=>new{name=c.name,position=V(c.localPosition),scale=V(c.localScale)})}),Formatting.Indented,
                new JsonSerializerSettings{ReferenceLoopHandling=ReferenceLoopHandling.Ignore}));
    }
}

