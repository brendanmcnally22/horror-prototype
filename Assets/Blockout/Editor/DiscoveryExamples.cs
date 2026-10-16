using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Blockout.Discovery;
using UHFPS.Runtime;

[InitializeOnLoad]
public static class DiscoveryExamples
{
    const string Root = "Assets/Blockout/Discovery";
    const string Marker = "Logs/DiscoveryExamples.request";
    static Material steel, dark, yellow;
    static DiscoveryExamples() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (!File.Exists(Marker) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Marker);
        try { Build(); } catch (Exception e) { File.WriteAllText("Logs/DiscoveryExamples.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Tools/Blockout/Add Wired Discovery Examples")]
    public static void Build()
    {
        var scene = SceneManager.GetSceneByPath(DiscoverySetup.ScenePath);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(DiscoverySetup.ScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        if (scene.GetRootGameObjects().Any(x => x.name == "EXAMPLES - Elevator Door Fuses")) return;
        steel = Mat("Security Steel", new Color(.22f,.28f,.32f));
        dark = Mat("Security Frame", new Color(.07f,.09f,.11f));
        yellow = Mat("Safety Yellow", new Color(1,.65f,.07f));
        var examples = new GameObject("EXAMPLES - Elevator Door Fuses");
        Undo.RegisterCreatedObjectUndo(examples, "Add discovery examples");

        var doorRoot = new GameObject("Security Door - Heavy Horizontal Slide");
        var door = doorRoot.AddComponent<PrototypeMover>();
        var carriage = new GameObject("Carriage - Door Leaf").transform; carriage.SetParent(doorRoot.transform, false);
        door.MovingPart = carriage; door.Travel = new Vector3(3.4f,0,0); door.Speed = .55f; door.Acceleration = .35f;
        Box(carriage,"Armored leaf - 55cm thick",new Vector3(3,3.2f,.55f),new Vector3(0,1.6f,0),steel);
        for(int i=0;i<3;i++) Box(carriage,"Reinforcement rib",new Vector3(2.8f,.16f,.7f),new Vector3(0,.55f+i*1.05f,0),dark);
        Box(carriage,"Leading hazard edge",new Vector3(.12f,3.1f,.6f),new Vector3(-1.4f,1.6f,0),yellow);
        Box(doorRoot.transform,"Left armored jamb",new Vector3(.4f,3.7f,1),new Vector3(-1.75f,1.85f,0),dark);
        Box(doorRoot.transform,"Right armored jamb",new Vector3(.4f,3.7f,1),new Vector3(1.75f,1.85f,0),dark);
        Box(doorRoot.transform,"Overhead drive rail",new Vector3(7.4f,.4f,1),new Vector3(1.7f,3.5f,0),dark);
        Box(doorRoot.transform,"Pocket rear",new Vector3(3.5f,3.3f,.2f),new Vector3(3.65f,1.65f,.55f),dark);
        Box(doorRoot.transform,"Pocket front",new Vector3(3.5f,3.3f,.2f),new Vector3(3.65f,1.65f,-.55f),dark);
        Box(doorRoot.transform,"Pocket end",new Vector3(.2f,3.3f,1.1f),new Vector3(5.35f,1.65f,0),dark);
        PrefabUtility.SaveAsPrefabAssetAndConnect(doorRoot,Root+"/Prefabs/Security Door - Heavy Slide.prefab",InteractionMode.AutomatedAction);
        doorRoot.transform.SetParent(examples.transform); doorRoot.transform.position = new Vector3(5,0,8);
        Lever(examples.transform,new Vector3(2.5f,0,6.8f),door,"SECURITY DOOR — OPEN / CLOSE");
        Lever(examples.transform,new Vector3(2.5f,0,9.2f),door,"SECURITY DOOR — OPEN / CLOSE");
        Sign(examples.transform,"HEAVY SECURITY DOOR\nLever: open / close • slow horizontal slide",new Vector3(5,4.1f,7.5f));

        var lift = Spawn<PrototypeMover>("Platform - Elevator",examples.transform,new Vector3(-5,0,7));
        lift.name="Elevator - 3m travel"; lift.Speed=.9f; lift.Acceleration=.8f;
        Box(lift.MovingPart,"Left guard rail",new Vector3(.1f,.85f,3),new Vector3(-1.45f,.75f,0),yellow);
        Box(lift.MovingPart,"Right guard rail",new Vector3(.1f,.85f,3),new Vector3(1.45f,.75f,0),yellow);
        Lever(lift.MovingPart,new Vector3(-4.1f,0,7.5f),lift,"ELEVATOR — UP / DOWN");
        Lever(examples.transform,new Vector3(-7.1f,0,5.8f),lift,"CALL ELEVATOR — UP / DOWN");
        Box(examples.transform,"Upper landing",new Vector3(4,.3f,3),new Vector3(-5,2.85f,10),steel);
        Box(examples.transform,"Landing back rail",new Vector3(4,1,.12f),new Vector3(-5,3.5f,11.45f),yellow);
        Box(examples.transform,"Landing left rail",new Vector3(.12f,1,3),new Vector3(-6.95f,3.5f,10),yellow);
        Box(examples.transform,"Landing right rail",new Vector3(.12f,1,3),new Vector3(-3.05f,3.5f,10),yellow);
        Lever(examples.transform,new Vector3(-6.4f,3,9.3f),lift,"CALL ELEVATOR — UP / DOWN");
        Sign(examples.transform,"ELEVATOR TEST\nBoard platform, then use its lever",new Vector3(-5,4.6f,10));

        // Native pack inventory pickup, mesh, material, examine behavior and item ID.
        string nativePath="Assets/ThunderWire Studio/UHFPS/Content/Prefabs/Interact/Inventory/PuzzleItems/PuzzleItem_Fuse.prefab";
        var native=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(nativePath));
        native.name="Fuse - Horror Pack Inventory";
        PrefabUtility.SaveAsPrefabAsset(native,Root+"/Prefabs/Fuse - Optional Pickup.prefab");
        UnityEngine.Object.DestroyImmediate(native);
        var panelAsset=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/Fuse Return Panel - One per Map.prefab");
        panelAsset.GetComponent<DiscoveryLoop>().UsePackInventory=true;
        PrefabUtility.SaveAsPrefabAsset(panelAsset,Root+"/Prefabs/Fuse Return Panel - One per Map.prefab");
        PrefabUtility.UnloadPrefabContents(panelAsset);
        var panel=Spawn<PrototypeFusePanel>("Fuse Return Panel - One per Map",examples.transform,new Vector3(0,1.3f,4));
        Box(examples.transform,"Panel stand",new Vector3(.6f,.8f,.5f),new Vector3(0,.4f,4),dark);
        var lamp=Spawn<PrototypeLight>("Light - Red Flicker",examples.transform,new Vector3(0,3,4));
        panel.Loop.FirstReturnLights=new[]{lamp};
        for(int i=0;i<3;i++)
        {
            Box(examples.transform,"Fuse sample plinth "+(i+1),new Vector3(.7f,.9f,.7f),new Vector3(-1.5f+i*1.5f,.45f,6),steel);
            var fuse=Spawn<InteractableItem>("Fuse - Optional Pickup",examples.transform,new Vector3(-1.5f+i*1.5f,.94f,6));
            fuse.name="Horror pack fuse "+(i+1);
        }
        Sign(examples.transform,"FUSE RETURN PANEL\nTake a pack fuse from the plinths; insert one at a time\nFirst return restores the red light to white",new Vector3(0,2.6f,4));

        var hud=new GameObject("Objective Display",typeof(Canvas),typeof(CanvasScaler));
        hud.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay; hud.GetComponent<Canvas>().sortingOrder=10;
        var scaler=hud.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080);
        var display=hud.AddComponent<DiscoveryObjectiveDisplay>();
        var textGO=new GameObject("Objective Text",typeof(RectTransform)); textGO.transform.SetParent(hud.transform,false);
        var text=textGO.AddComponent<TextMeshProUGUI>(); text.fontSize=26; text.color=Color.white; text.raycastTarget=false;
        var rect=text.rectTransform; rect.anchorMin=rect.anchorMax=new Vector2(0,1); rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(45,-125); rect.sizeDelta=new Vector2(650,130);
        display.Text=text; textGO.SetActive(false);
        PrefabUtility.SaveAsPrefabAssetAndConnect(hud,Root+"/Prefabs/Objective Display.prefab",InteractionMode.AutomatedAction);
        hud.transform.SetParent(examples.transform,false); display.Loop=panel.Loop;
        var zone=new GameObject("Objective Trigger - Walk Through",typeof(BoxCollider),typeof(Rigidbody),typeof(DiscoveryObjectiveTrigger));
        zone.GetComponent<BoxCollider>().isTrigger=true; zone.GetComponent<BoxCollider>().size=new Vector3(8,3,1);
        zone.GetComponent<Rigidbody>().isKinematic=true; zone.GetComponent<Rigidbody>().useGravity=false;
        PrefabUtility.SaveAsPrefabAssetAndConnect(zone,Root+"/Prefabs/Objective Trigger - Walk Through.prefab",InteractionMode.AutomatedAction);
        zone.transform.SetParent(examples.transform); zone.transform.position=new Vector3(0,1.5f,2);
        zone.GetComponent<DiscoveryObjectiveTrigger>().Display=display;
        foreach(var component in examples.GetComponentsInChildren<Component>(true))
            if(component && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        File.WriteAllText("Logs/DiscoveryExamples.txt","PASS: examples saved in Discovery Blank. Security door with two synchronized levers; elevator with onboard and landing controls; three native inventory fuses; return panel; objective display and invisible trigger.\n");
        Selection.activeGameObject=examples;
    }
    static T Spawn<T>(string name,Transform parent,Vector3 position) where T:Component
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+name+".prefab"));
        go.transform.SetParent(parent); go.transform.position=position; return go.GetComponent<T>();
    }
    static void Lever(Transform parent,Vector3 position,PrototypeMover target,string label)
    {
        var lever=Spawn<PrototypeLever>("Lever",parent,position); lever.Label=label; lever.Targets=new[]{target}; lever.FollowTarget=true;
    }
    static void Box(Transform parent,string name,Vector3 size,Vector3 position,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.layer=11;
        go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=size; go.GetComponent<Renderer>().sharedMaterial=material;
    }
    static void Sign(Transform parent,string content,Vector3 position)
    {
        var go=new GameObject("Instructions",typeof(TextMeshPro)); go.transform.SetParent(parent); go.transform.position=position;
        var text=go.GetComponent<TextMeshPro>(); text.text=content; text.fontSize=2.2f; text.alignment=TextAlignmentOptions.Center; text.rectTransform.sizeDelta=new Vector2(7,1.3f);
    }
    static Material Mat(string name,Color color)
    {
        string path=Root+"/Materials/"+name+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path); if(m)return m;
        m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor",color); m.SetFloat("_Metallic",.7f);m.SetFloat("_Smoothness",.3f);AssetDatabase.CreateAsset(m,path);return m;
    }
}
