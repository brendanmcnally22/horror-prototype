using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Blockout.Discovery;
using UHFPS.Runtime;

public static partial class FacilityIterationSetup
{
    const string Folder="Assets/Blockout/Discovery/Prefabs/Platform Safety";
    const string Metrics="Assets/Blockout/Discovery/Prefabs/Playtest Tracking";
    static Material railMat,gateMat;
    static void Apply()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!=DiscoverySetup.ScenePath)throw new Exception("Open Discovery Blank before applying.");
        var all=All();
        var jammed=all.Where(t=>t.name.Equals("JAMMED LEVER",StringComparison.OrdinalIgnoreCase)||t.name.Equals("JAMMED LEVER 2",StringComparison.OrdinalIgnoreCase)).Select(t=>t.GetComponent<PrototypeLever>()).Where(l=>l).ToArray();
        if(jammed.Length!=2)throw new Exception("Expected exactly both named jammed levers.");
        var panel=all.Select(t=>t.GetComponent<FuseboxPuzzle>()).Single(p=>p);
        var loop=all.Select(t=>t.GetComponent<DiscoveryLoop>()).Single(p=>p);
        var hero=all.Select(t=>t.GetComponent<PlayerManager>()).Single(p=>p);
        var elevator=all.Single(t=>t.name=="STARTING ELEVATOR").GetComponent<PrototypeMover>();
        var fuses=all.Where(t=>t.name.StartsWith("Fuse - Optional Pickup")).Select(t=>t.GetComponent<InteractableItem>()).Where(t=>t).ToArray();
        if(fuses.Length!=3||!elevator.MovingPart)throw new Exception("Expected three placed fuses and a valid starting elevator.");
        var cover=panel.transform.Find("FuseBox/Door");if(!cover)throw new Exception("Panel cover missing.");
        var before=all.ToDictionary(t=>t,t=>(t.parent,t.localPosition,t.localRotation,t.localScale,t.gameObject.activeSelf));
        Directory.CreateDirectory("Logs/FacilityIterationSafety");EditorSceneManager.SaveScene(scene,"Logs/FacilityIterationSafety/Before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Facility interactions and elevator doors");
        foreach(var root in scene.GetRootGameObjects())Undo.RegisterFullObjectHierarchyUndo(root,"Facility interactions");
        try
        {
            CreatePrefabs();
            foreach(var lever in jammed){lever.Jammed=true;lever.JamMessage="JAMMED — The mechanism is seized. Find another route.";}
            var rules=hero.GetComponent<FacilityPlayerRules>();if(!rules)rules=Undo.AddComponent<FacilityPlayerRules>(hero.gameObject);rules.DisableJumping=true;
            if(!hero.GetComponent<DesignPlayerStats>())Undo.AddComponent<DesignPlayerStats>(hero.gameObject);
            // Restore the native socket references by hierarchy path, then explicitly reconnect progress.
            string[] letters={"A","B","C","D"};panel.Fuses.Clear();
            for(int i=0;i<4;i++)
            {
                var fuse=panel.transform.Find("Fuses/Fuse_"+letters[i]);var indicator=panel.transform.Find("Indicators/Indicator_"+letters[i]);
                if(!fuse||!indicator)throw new Exception("Panel socket hierarchy incomplete.");
                panel.Fuses.Add(new FuseboxPuzzle.FuseElement{FuseObject=fuse.gameObject,FuseLight=indicator.GetComponentInChildren<Light>(),LightRenderer=indicator.GetComponent<MeshRenderer>(),IsInserted=i==3});
            }
            panel.FuseItem=fuses[0].PickupItem;panel.UseInteract=true;
            for(int i=panel.OnFuseConnected.GetPersistentEventCount()-1;i>=0;i--)UnityEventTools.RemovePersistentListener(panel.OnFuseConnected,i);
            UnityEventTools.AddPersistentListener(panel.OnFuseConnected,loop.RecordPackFuse);
            for(int i=panel.OnAllFusesConnected.GetPersistentEventCount()-1;i>=0;i--)if(!panel.OnAllFusesConnected.GetPersistentTarget(i))UnityEventTools.RemovePersistentListener(panel.OnAllFusesConnected,i);
            var power=all.Select(t=>t.GetComponent<FacilityPowerSequence>()).Single(p=>p);
            loop.RequiredFuses=3;loop.UsePackInventory=true;
            if(!Enumerable.Range(0,loop.OnCompleted.GetPersistentEventCount()).Any(i=>loop.OnCompleted.GetPersistentTarget(i)==power))UnityEventTools.AddPersistentListener(loop.OnCompleted,power.RestorePower);
            if(!Enumerable.Range(0,loop.OnFirstReturned.GetPersistentEventCount()).Any(i=>loop.OnFirstReturned.GetPersistentTarget(i)==power))UnityEventTools.AddPersistentListener(loop.OnFirstReturned,power.RestoreLocalPower);
            var dynamic=cover.GetComponent<DynamicObject>();if(dynamic)Undo.DestroyObjectImmediate(dynamic);
            cover.gameObject.layer=11;
            panel.GetComponent<BoxCollider>().enabled=false;
            var interaction=panel.transform.Find("Panel interaction - Open then Insert");
            if(!interaction){var g=new GameObject("Panel interaction - Open then Insert");Undo.RegisterCreatedObjectUndo(g,"Panel interaction");g.transform.SetParent(panel.transform,false);interaction=g.transform;}
            interaction.gameObject.layer=6;
            var box=interaction.GetComponent<BoxCollider>();if(!box)box=interaction.gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,.05f,.085f);box.size=new Vector3(.29f,.42f,.07f);box.isTrigger=true;
            var use=interaction.GetComponent<FacilityPanelUse>();if(!use)use=interaction.gameObject.AddComponent<FacilityPanelUse>();use.Panel=panel;use.Cover=cover;
            for(int i=0;i<fuses.Length;i++){fuses[i].gameObject.layer=6;fuses[i].Quantity=1;var metric=fuses[i].GetComponent<DesignPickupMetric>();if(!metric)metric=Undo.AddComponent<DesignPickupMetric>(fuses[i].gameObject);metric.PickupId="fuse_"+(i+1);if(!Enumerable.Range(0,fuses[i].OnTakeEvent.GetPersistentEventCount()).Any(n=>fuses[i].OnTakeEvent.GetPersistentTarget(n)==metric))UnityEventTools.AddPersistentListener(fuses[i].OnTakeEvent,metric.RecordPickup);}
            if(!elevator.MovingPart.GetComponentInChildren<PlatformSafetyGate>())
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Starting Elevator - Sliding Doors 2.55m.prefab");
                var doors=(GameObject)PrefabUtility.InstantiatePrefab(prefab,elevator.MovingPart);Undo.RegisterCreatedObjectUndo(doors,"Starting elevator doors");
                doors.transform.localPosition=Vector3.zero;doors.transform.localRotation=Quaternion.identity;doors.GetComponent<PlatformSafetyGate>().Mover=elevator;
            }
            foreach(var t in all){var b=before[t];if(!t||t.parent!=b.Item1||t.localPosition!=b.Item2||t.localRotation!=b.Item3||t.localScale!=b.Item4||t.gameObject.activeSelf!=b.Item5)throw new Exception("Existing layout changed: "+(t?t.name:"deleted"));}
            foreach(var c in All().SelectMany(t=>t.GetComponents<Component>()).Where(c=>c)){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Undo.CollapseUndoOperations(undo);
            File.WriteAllText("Logs/FacilityIterationResult.txt","PASS: both named levers jammed; player Jump state disabled; existing fusebox repaired with open/insert interaction and native inventory; three existing fuse pickups tracked; start-elevator sliding doors added under Carriage. All "+all.Length+" preexisting transforms/active states preserved. 3m safety cage and tracking prefabs generated.\n");
        }
        catch{Undo.RevertAllDownToGroup(undo);throw;}
    }
    static Material Material(string name,Color color)
    {
        string folder="Assets/Blockout/Discovery/Materials/Platform Safety";Directory.CreateDirectory(folder);AssetDatabase.Refresh();string path=folder+"/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat)return mat;mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=color;AssetDatabase.CreateAsset(mat,path);return mat;
    }
    static GameObject Cube(GameObject parent,string name,Vector3 pos,Vector3 scale,Material material,bool visible=true)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.layer=11;g.transform.SetParent(parent.transform,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;g.GetComponent<Renderer>().enabled=visible;g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;return g;
    }
    static void CreatePrefabs()
    {
        Directory.CreateDirectory(Folder);Directory.CreateDirectory(Metrics);AssetDatabase.Refresh();
        var previous=SceneManager.GetActiveScene();var temp=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(temp);
        try
        {
            railMat=Material("Safety yellow",new Color(.85f,.55f,.08f));gateMat=Material("Steel gate",new Color(.19f,.24f,.26f));
            var cage=new GameObject("Platform Safety Cage - 3m - Entry minus Z");
            foreach(int side in new[]{-1,1})
            {
                Cube(cage,"Side collision",new Vector3(side*1.46f,.8f,0),new Vector3(.08f,1.6f,3),gateMat,false);
                foreach(float h in new[]{.25f,.8f,1.4f})Cube(cage,"Side rail",new Vector3(side*1.46f,h,0),new Vector3(.08f,.08f,3),railMat);
                foreach(float z in new[]{-1.46f,0,1.46f})Cube(cage,"Upright",new Vector3(side*1.46f,.75f,z),new Vector3(.09f,1.5f,.09f),gateMat);
            }
            Cube(cage,"Back collision",new Vector3(0,.8f,1.46f),new Vector3(3,1.6f,.08f),gateMat,false);
            foreach(float h in new[]{.25f,.8f,1.4f})Cube(cage,"Back rail",new Vector3(0,h,1.46f),new Vector3(3,.08f,.08f),railMat);
            AddGate(cage,2.92f,1.5f,-1.46f,true);SavePrefab(cage,Folder);
            var doors=new GameObject("Starting Elevator - Sliding Doors 2.55m");AddGate(doors,2.55f,2.8f,-1.44f,false);SavePrefab(doors,Folder);
            var zone=new GameObject("Tracking - Zone Visit and Dwell");var collider=zone.AddComponent<BoxCollider>();collider.isTrigger=true;collider.size=new Vector3(4,3,4);collider.center=Vector3.up*1.5f;zone.AddComponent<DesignMetricTrigger>();SavePrefab(zone,Metrics);
            var milestone=new GameObject("Tracking - First Visit Milestone");collider=milestone.AddComponent<BoxCollider>();collider.isTrigger=true;collider.size=new Vector3(3,3,1);collider.center=Vector3.up*1.5f;var trigger=milestone.AddComponent<DesignMetricTrigger>();trigger.FirstVisitOnly=true;trigger.LogExitAndDuration=false;trigger.EnterEvent="milestone";SavePrefab(milestone,Metrics);
            var logger=new GameObject("Tracking - Session Logger");logger.AddComponent<DesignSession>();SavePrefab(logger,Metrics);
        }
        finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(temp,true);}
    }
    static void AddGate(GameObject root,float width,float height,float z,bool openStart)
    {
        var gate=root.AddComponent<PlatformSafetyGate>();gate.LeftClosed=new Vector3(-width/4,height/2,z);gate.RightClosed=new Vector3(width/4,height/2,z);gate.SlideDistance=width/2+.04f;
        gate.LeftLeaf=Cube(root,"Left sliding leaf",gate.LeftClosed,new Vector3(width/2,height,.09f),gateMat).transform;
        gate.RightLeaf=Cube(root,"Right sliding leaf",gate.RightClosed,new Vector3(width/2,height,.09f),gateMat).transform;
        gate.EntranceCenter=new Vector3(0,height/2,z);gate.EntranceSize=new Vector3(width,height,.55f);gate.OpenAtStart=openStart;
        Cube(root,"Header rail",new Vector3(0,height+.07f,z),new Vector3(width+.18f,.14f,.16f),railMat);
    }
    static void SavePrefab(GameObject g,string folder){string path=folder+"/"+g.name+".prefab";if(!AssetDatabase.LoadAssetAtPath<GameObject>(path))PrefabUtility.SaveAsPrefabAsset(g,path);UnityEngine.Object.DestroyImmediate(g);}
    [MenuItem("Tools/Blockout/Export Playtest Logs")]
    public static void ExportLogs()
    {
        string source=System.IO.Path.Combine(Application.persistentDataPath,"PlaytestLogs");string destination=System.IO.Path.GetFullPath("PlaytestExports");Directory.CreateDirectory(destination);
        if(Directory.Exists(source))foreach(string file in Directory.GetFiles(source,"*.csv"))File.Copy(file,System.IO.Path.Combine(destination,System.IO.Path.GetFileName(file)),true);
        Debug.Log("Playtest CSV copies exported to "+destination);
    }
}
