using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Blockout.Discovery;
using UHFPS.Runtime;

[InitializeOnLoad]
public static partial class FacilityIterationSetup
{
    static FacilityIterationSetup(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(!File.Exists("Logs/FacilityIteration.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        string action=File.ReadAllText("Logs/FacilityIteration.request").Trim();File.Delete("Logs/FacilityIteration.request");
        try{if(action=="apply")Apply();else Inspect();}catch(Exception e){File.WriteAllText("Logs/FacilityIterationError.txt",e.ToString());Debug.LogException(e);}
    }
    static string Path(Transform t)=>t.parent?Path(t.parent)+"/"+t.name:t.name;
    static object V(Vector3 v)=>new{x=v.x,y=v.y,z=v.z};
    static Transform[] All()=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
    static void Inspect()
    {
        var all=All();var player=all.FirstOrDefault(t=>t.GetComponent<PlayerManager>());
        var relevant=all.Where(t=>t.GetComponent<PrototypeLever>()||t.GetComponent<PrototypeMover>()||t.GetComponent<FuseboxPuzzle>()||t.name.Contains("JAMMED")||t.name.StartsWith("Fuse - Optional")||t.name=="HEROPLAYER").ToArray();
        File.WriteAllText("Logs/FacilityIterationInspect.json",JsonConvert.SerializeObject(new{scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,player=player?V(player.position):null,
            objects=relevant.Select(t=>new{path=Path(t),position=V(t.position),rotation=V(t.eulerAngles),scale=V(t.lossyScale),
                components=t.GetComponents<Component>().Where(c=>c).Select(c=>new{type=c.GetType().Name,json=EditorJsonUtility.ToJson(c)}).ToArray(),
                children=t.GetComponentsInChildren<Transform>(true).Select(c=>new{path=Path(c),position=V(c.position),local=V(c.localPosition),scale=V(c.localScale),active=c.gameObject.activeSelf,layer=c.gameObject.layer,
                    components=c.GetComponents<Component>().Where(a=>a).Select(a=>a.GetType().Name).ToArray(),colliders=c.GetComponents<Collider>().Select(a=>new{enabled=a.enabled,trigger=a.isTrigger,center=V(a.bounds.center),size=V(a.bounds.size)}).ToArray()}).ToArray()})},Formatting.Indented));
    }
}
