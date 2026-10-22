using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Blockout.Discovery;
using UHFPS.Runtime;

public static partial class DiscoveryWayfindingSetup
{
    const string Signs="Assets/Blockout/Discovery/Prefabs/Wayfinding";
    const string Art="Assets/Blockout/Discovery/Materials/Wayfinding";
    static Material SignMat(string name,Color color)
    {
        string path=Art+"/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(mat,path);}
        return mat;
    }
    static GameObject Panel(Transform parent,string name,Vector3 pos,Vector3 size,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
        go.transform.localPosition=pos;go.transform.localScale=size;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        return go;
    }
    static TextMeshPro Text(Transform parent,string name,Vector3 position,Vector2 size,float fontSize,Color color,TMP_FontAsset font)
    {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;
        var text=g.AddComponent<TextMeshPro>();text.font=font;text.fontSize=fontSize;text.color=color;
        text.rectTransform.sizeDelta=size;text.alignment=TextAlignmentOptions.MidlineLeft;
        text.enableAutoSizing=true;text.fontSizeMin=fontSize*.65f;text.fontSizeMax=fontSize;
        text.textWrappingMode=TextWrappingModes.NoWrap;
        return text;
    }
    static void CreateSigns()
    {
        Directory.CreateDirectory(Signs);Directory.CreateDirectory(Art);AssetDatabase.Refresh();
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ThunderWire Studio/UHFPS/Content/Fonts/Roboto Condensed/TMP/RobotoCondensed-Bold SDF.asset");
        if(!font)throw new Exception("No TMP font available.");
        var housing=SignMat("Charcoal housing",new Color(.035f,.045f,.06f));
        var face=SignMat("Graphite face",new Color(.065f,.085f,.11f));
        var white=SignMat("Route tint base",Color.white);
        var arrow=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/Arrow.asset");
        if(!arrow)
        {
            arrow=new Mesh{name="Wayfinding arrow"};
            arrow.vertices=new[]{new Vector3(-.28f,-.07f,0),new Vector3(-.28f,.07f,0),new Vector3(.05f,.07f,0),new Vector3(.05f,-.07f,0),
                new Vector3(0,-.21f,0),new Vector3(0,.21f,0),new Vector3(.29f,0,0)};
            arrow.triangles=new[]{0,1,2,0,2,3,4,5,6};arrow.RecalculateNormals();arrow.RecalculateBounds();AssetDatabase.CreateAsset(arrow,Art+"/Arrow.asset");
        }
        string[] labels={"STORAGE ROOM","EXPERIMENT HALL","ADMINISTRATION","CROSS FACILITY SHUTTLE","PULL THIS TO OPEN SECURITY DOORS"};
        string[] codes={"01 / STORAGE","02 / RESEARCH","03 / OFFICES","04 / TRANSIT","SECURITY / MANUAL RELEASE"};
        Color[] colors={new Color(1,.78f,.03f),new Color(.12f,.42f,1),new Color(.72f,.25f,1),new Color(.1f,.9f,.3f),new Color(1,.78f,.03f)};
        for(int i=0;i<labels.Length;i++)
        {
            string path=Signs+"/"+labels[i]+".prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(path))continue;
            var root=new GameObject(labels[i]);var sign=root.AddComponent<FacilityWayfindingSign>();
            Panel(root.transform,"Housing",Vector3.zero,new Vector3(3.2f,.72f,.065f),housing);
            Panel(root.transform,"Face",new Vector3(0,0,-.037f),new Vector3(3.1f,.63f,.012f),face);
            sign.ColorBand=Panel(root.transform,"Route color",new Vector3(-1.5f,0,-.047f),new Vector3(.065f,.62f,.01f),white).GetComponent<Renderer>();
            sign.LabelText=Text(root.transform,"Destination",new Vector3(-.28f,-.075f,-.056f),new Vector2(2.15f,.32f),i==4?1.9f:2.4f,Color.white,font);
            sign.CodeText=Text(root.transform,"Zone code",new Vector3(-.28f,.185f,-.056f),new Vector2(2.15f,.13f),.95f,new Color(.65f,.72f,.8f),font);
            var ar=new GameObject("Direction arrow");ar.transform.SetParent(root.transform,false);ar.transform.localPosition=new Vector3(1.13f,0,-.057f);
            ar.AddComponent<MeshFilter>().sharedMesh=arrow;sign.ArrowRenderer=ar.AddComponent<MeshRenderer>();sign.ArrowRenderer.sharedMaterial=white;sign.ArrowRenderer.shadowCastingMode=ShadowCastingMode.Off;
            sign.Arrow=ar.transform;sign.Label=labels[i];sign.ZoneCode=codes[i];sign.RouteColor=colors[i];sign.Direction=i==4?FacilityWayfindingSign.ArrowDirection.Back:FacilityWayfindingSign.ArrowDirection.Right;sign.Refresh();
            PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);
        }
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Tools/Blockout/Apply Discovery Signs Elevator and Flashlight")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
        var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath)throw new Exception("Open Discovery Blank first.");
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var player=all.Select(t=>t.GetComponent<PlayerManager>()).Single(x=>x);
        var flashlight=player.GetComponentInChildren<FlashlightItem>(true);
        var items=player.GetComponentInChildren<PlayerItemsManager>(true);
        var elevator=all.Single(t=>t.name=="STARTING ELEVATOR").GetComponent<PrototypeMover>();
        var gate=elevator.GetComponentInChildren<PlatformSafetyGate>(true);
        if(!gate||!gate.LeftLeaf||!gate.RightLeaf||!elevator.MovingPart||!flashlight||!flashlight.FlashlightLight||!flashlight.ItemObject||!items.PlayerItems.Contains(flashlight))
            throw new Exception("Existing references incomplete; scene not changed.");
        var before=all.ToDictionary(t=>t,t=>(t.parent,t.localPosition,t.localRotation,t.localScale,t.gameObject.activeSelf));
        EditorSceneManager.SaveScene(scene,Log("DiscoveryBeforeWayfinding.unity"),true);
        CreateSigns();
        Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Discovery elevator and flashlight");
        Undo.RecordObject(gate,"Elevator safety");gate.Mover=elevator;gate.OpenAtStart=false;gate.OpenAtEnd=true;
        foreach(var leaf in new[]{gate.LeftLeaf,gate.RightLeaf})
        {
            var col=leaf.GetComponent<BoxCollider>();if(!col)throw new Exception("Gate leaf collider missing.");
            Undo.RecordObject(col,"Gate collision");col.enabled=true;col.isTrigger=false;
            if(Physics.GetIgnoreLayerCollision(player.gameObject.layer,leaf.gameObject.layer))throw new Exception("Gate layer ignores player; inspect before continuing.");
            EditorUtility.SetDirty(col);PrefabUtility.RecordPrefabInstancePropertyModifications(col);
        }
        Undo.RecordObject(flashlight,"Flashlight appearance and F control");
        flashlight.EnableUVFlashlight=false;flashlight.EnableHintControls=false;
        flashlight.InfiniteBattery=true;flashlight.LightIntensity=3.5f;flashlight.NormalFlashlightColor=new Color(1,.96f,.88f);
        var light=flashlight.FlashlightLight;Undo.RecordObject(light,"Flashlight beam");
        light.type=LightType.Spot;light.range=20;light.spotAngle=44;light.innerSpotAngle=25;light.intensity=3.5f;
        light.color=flashlight.NormalFlashlightColor;light.shadows=LightShadows.Soft;light.shadowStrength=1;light.shadowBias=.025f;light.shadowNormalBias=.15f;
        // Keep the horror pack's existing beam cookie, hand model, animations and audio.
        var control=player.GetComponent<FacilityFlashlightControl>();if(!control)control=Undo.AddComponent<FacilityFlashlightControl>(player.gameObject);
        control.Items=items;control.Flashlight=flashlight;
        var inv=all.Select(t=>t.GetComponent<Inventory>()).Single(x=>x);
        const string flashlightId="ba53a6ad76bb4513ba434a3b7542345a";
        if(!inv.startingItems.Any(x=>x.GUID==flashlightId))
        {Undo.RecordObject(inv,"Starting flashlight");inv.startingItems.Add(new Inventory.StartingItem{GUID=flashlightId,title="Flashlight",quantity=1});EditorUtility.SetDirty(inv);}
        var beam=player.GetComponent<FacilityFlashlightBeam>();if(!beam)beam=Undo.AddComponent<FacilityFlashlightBeam>(player.gameObject);
        if(!beam.Volume)
        {
            var volume=GameObject.CreatePrimitive(PrimitiveType.Cube);volume.name="Flashlight - Atmospheric Beam";Undo.RegisterCreatedObjectUndo(volume,"Flashlight beam");
            volume.transform.SetParent(player.transform,false);volume.layer=0;UnityEngine.Object.DestroyImmediate(volume.GetComponent<Collider>());
            var renderer=volume.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.enabled=false;
            string materialPath=Art+"/Flashlight atmosphere.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!mat){mat=new Material(Shader.Find("Blockout/Facility Flashlight Volume"));AssetDatabase.CreateAsset(mat,materialPath);}
            renderer.sharedMaterial=mat;beam.Volume=renderer;
        }
        beam.Source=light;beam.PlayerRoot=player.transform;
        foreach(var camera in player.GetComponentsInChildren<Camera>(true))
        {
            var data=camera.GetUniversalAdditionalCameraData();
            if(data.renderType==CameraRenderType.Base){Undo.RecordObject(data,"Beam depth occlusion");data.requiresDepthTexture=true;EditorUtility.SetDirty(data);}
        }
        foreach(var t in all){var b=before[t];if(!t||t.parent!=b.Item1||t.localPosition!=b.Item2||t.localRotation!=b.Item3||t.localScale!=b.Item4||t.gameObject.activeSelf!=b.Item5)throw new Exception("Existing layout changed: "+(t?t.name:"deleted"));}
        foreach(var c in new Component[]{gate,flashlight,light,control,beam,inv})
        {EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText(Log("WayfindingApplied.txt"),"PASS: five adjustable sign prefabs; starting elevator closed until bottom; F toggles native flashlight; infinite prototype battery, warm cookie spotlight, depth-tested atmospheric beam. All "+all.Length+" existing transforms and active states preserved.");
        PreviewSigns();
    }
    [MenuItem("Tools/Blockout/Preview Discovery Signs")]
    public static void PreviewSigns()
    {
        var previous=SceneManager.GetActiveScene();var preview=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(preview);
        try
        {
            string[] labels={"STORAGE ROOM","EXPERIMENT HALL","ADMINISTRATION","CROSS FACILITY SHUTTLE","PULL THIS TO OPEN SECURITY DOORS"};
            for(int i=0;i<labels.Length;i++)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Signs+"/"+labels[i]+".prefab"));
                go.transform.position=new Vector3(1000,1.8f-i*.9f,0);go.GetComponent<FacilityWayfindingSign>().Refresh();
                foreach(var text in go.GetComponentsInChildren<TMP_Text>())text.ForceMeshUpdate(true,true);
            }
            var camera=new GameObject("Preview camera").AddComponent<Camera>();camera.transform.position=new Vector3(1000,0,-8);
            camera.orthographic=true;camera.orthographicSize=2.6f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.015f,.02f,.03f);
            camera.cameraType=CameraType.Preview;
            var target=new RenderTexture(1100,1300,24);camera.targetTexture=target;camera.Render();
            var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1100,1300,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1100,1300),0,0);image.Apply();File.WriteAllBytes(Log("DiscoverySignsPreview.png"),image.EncodeToPNG());
            RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);
        }
        finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(preview,true);}
    }
    [MenuItem("Tools/Blockout/Finish Discovery Sign Typography")]
    public static void FinishTypography()
    {
        var bold=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ThunderWire Studio/UHFPS/Content/Fonts/Roboto Condensed/TMP/RobotoCondensed-Bold SDF.asset");
        var regular=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ThunderWire Studio/UHFPS/Content/Fonts/Roboto Condensed/TMP/RobotoCondensed-Regular SDF.asset");
        foreach(var path in Directory.GetFiles(Signs,"*.prefab"))
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var sign=root.GetComponent<FacilityWayfindingSign>();sign.LabelText.font=bold;sign.CodeText.font=regular;
                if(root.name.StartsWith("PULL"))
                {
                    sign.Label="PULL THIS TO OPEN\nSECURITY DOORS";
                    sign.LabelText.rectTransform.sizeDelta=new Vector2(2.15f,.39f);
                    sign.LabelText.fontSizeMin=1.7f;sign.LabelText.fontSizeMax=2f;
                }
                sign.Refresh();foreach(var text in root.GetComponentsInChildren<TMP_Text>())text.ForceMeshUpdate(true,true);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        PreviewSigns();
    }
}
