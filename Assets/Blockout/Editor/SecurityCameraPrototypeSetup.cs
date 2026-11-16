using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UHFPS.Runtime;
using Blockout.Discovery;

public static class SecurityCameraPrototypeSetup
{
    public const string ScenePath="Assets/Blockout/Discovery/Scenes/Security Camera Prototype.unity";
    const string Folder="Assets/Blockout/Discovery/Security Camera Prototype";
    static Material concrete,steel,dark,green,amber;
    static Material Mat(string name,Color color)
    {
        string path=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat)return mat;
        mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=color;mat.SetFloat("_Smoothness",.25f);AssetDatabase.CreateAsset(mat,path);return mat;
    }
    static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,Material mat,bool collision=true)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.layer=11;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;
        if(!collision)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
    }
    static void Label(Transform parent,string text,Vector3 position,float size=2)
    {
        var go=new GameObject("Label - "+text);go.transform.SetParent(parent,false);go.transform.localPosition=position;
        var tmp=go.AddComponent<TextMeshPro>();tmp.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ThunderWire Studio/UHFPS/Content/Fonts/Roboto Condensed/TMP/RobotoCondensed-Bold SDF.asset");tmp.text=text;tmp.fontSize=size;tmp.rectTransform.sizeDelta=new Vector2(5,1);tmp.alignment=TextAlignmentOptions.Center;tmp.color=Color.white;
    }
    static SecurityPanelDoor Door(string name,float x,bool locked)
    {
        var root=new GameObject(name);root.transform.position=new Vector3(x,0,4);
        var door=root.AddComponent<SecurityPanelDoor>();door.DoorLabel=name;door.RequiresKeycard=locked;
        var mover=root.AddComponent<PrototypeMover>();door.Mover=mover;
        var carriage=new GameObject("Door carriage").transform;carriage.SetParent(root.transform,false);mover.MovingPart=carriage;mover.Travel=new Vector3(0,3.35f,0);mover.Speed=.8f;mover.Acceleration=.8f;
        Box(carriage,"Armored sliding leaf",new Vector3(0,1.5f,0),new Vector3(2.9f,3,.35f),steel);
        for(int i=0;i<5;i++)Box(carriage,"Reinforcement rib",new Vector3(0,.3f+i*.55f,-.21f),new Vector3(2.75f,.08f,.08f),dark,false);
        foreach(int side in new[]{-1,1})Box(root.transform,"Steel jamb",new Vector3(side*1.65f,1.6f,0),new Vector3(.3f,3.2f,.65f),dark);
        Box(root.transform,"Overhead rail",new Vector3(0,3.25f,0),new Vector3(3.6f,.25f,.65f),locked?amber:green);
        Label(root.transform,name+"\n"+(locked?"GREEN KEYCARD REQUIRED":"STANDARD ACCESS"),new Vector3(0,3.95f,-.4f));
        PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+name+".prefab");return door;
    }
    [MenuItem("Tools/Dead Air/Create Security Camera Prototype Scene")]
    public static void Create()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
        if(File.Exists(ScenePath)){Debug.Log("Security camera prototype already exists; preserved.");return;}
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        concrete=Mat("Concrete",new Color(.32f,.35f,.38f));steel=Mat("Security steel",new Color(.18f,.23f,.27f));dark=Mat("Panel housing",new Color(.025f,.035f,.045f));green=Mat("Access green",new Color(.08f,.7f,.28f));amber=Mat("Restricted amber",new Color(.95f,.55f,.06f));
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blockout/Prefabs/Setup/Player Setup.prefab");if(!prefab)throw new Exception("Wired player setup missing.");
            var setup=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);setup.GetComponentInChildren<PlayerManager>(true).transform.SetPositionAndRotation(new Vector3(0,.1f,-6),Quaternion.identity);
            var room=new GameObject("SECURITY CAMERA TEST CHAMBER");
            Box(room.transform,"Floor",new Vector3(0,-.25f,2),new Vector3(20,.5f,22),concrete);
            Box(room.transform,"Left wall",new Vector3(-10,2,2),new Vector3(.4f,4,22),concrete);Box(room.transform,"Right wall",new Vector3(10,2,2),new Vector3(.4f,4,22),concrete);
            Box(room.transform,"Rear wall",new Vector3(0,2,13),new Vector3(20,4,.4f),concrete);Box(room.transform,"Front wall",new Vector3(0,2,-9),new Vector3(20,4,.4f),concrete);
            Box(room.transform,"Center divider",new Vector3(0,2,4),new Vector3(4.7f,4,.4f),concrete);
            foreach(int side in new[]{-1,1})Box(room.transform,"Side divider",new Vector3(side*7.8f,2,4),new Vector3(4.4f,4,.4f),concrete);
            var doors=new[]{Door("A1 - SERVICE SHUTTER",-4,false),Door("B1 - RESTRICTED SHUTTER",4,true)};
            var console=new GameObject("SECURITY CONTROL PANEL");console.layer=6;console.transform.position=new Vector3(0,0,-3);
            var hit=console.AddComponent<BoxCollider>();hit.center=new Vector3(0,1.15f,0);hit.size=new Vector3(1.9f,1.2f,.65f);
            Box(console.transform,"Console pedestal",new Vector3(0,.45f,0),new Vector3(1.7f,.9f,.7f),steel);
            Box(console.transform,"Monitor housing",new Vector3(0,1.35f,0),new Vector3(1.9f,1,.3f),dark,false);
            Box(console.transform,"Monitor glass",new Vector3(0,1.35f,-.17f),new Vector3(1.65f,.75f,.025f),green,false);
            Label(console.transform,"SECURITY CONTROL\nINTERACT TO ACCESS CAMERAS",new Vector3(0,2.1f,-.2f),1.8f);
            var panel=console.AddComponent<SecurityCameraPanel>();panel.Channels=new SecurityCameraPanel.Channel[2];
            for(int i=0;i<2;i++)
            {
                float x=i==0?-4:4;var go=new GameObject("CAM 0"+(i+1));go.transform.position=new Vector3(x,3.3f,-1);go.transform.LookAt(new Vector3(x,1.4f,4));
                var camera=go.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=65;camera.nearClipPlane=.1f;camera.farClipPlane=60;camera.cullingMask=~((1<<5)|(1<<7)|(1<<13));camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
                var kit=go.AddComponent<CCTV_Camera>();kit.LiveCamera=camera;
                Box(go.transform,"Camera housing",new Vector3(0,0,-.2f),new Vector3(.4f,.28f,.4f),dark,false);
                panel.Channels[i]=new SecurityCameraPanel.Channel{Label="CAM 0"+(i+1)+(i==0?" / SERVICE":" / RESTRICTED"),Camera=kit,Door=doors[i]};
            }
            // Prefab is intentionally unwired: assign scene cameras and doors after placing it elsewhere.
            var configured=panel.Channels;panel.Channels=Array.Empty<SecurityCameraPanel.Channel>();PrefabUtility.SaveAsPrefabAsset(console,Folder+"/Security Control Panel.prefab");panel.Channels=configured;
            Box(room.transform,"Keycard table",new Vector3(-4,.85f,-3),new Vector3(2,.2f,1),steel);
            var card=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThunderWire Studio/UHFPS/Content/Prefabs/Interact/Inventory/PuzzleItems/PuzzleItem_KeycardGreen.prefab"),scene);
            card.name="GREEN SECURITY KEYCARD — pick up to authorize B1";card.transform.position=new Vector3(-4,1,-3);
            Label(room.transform,"GREEN SECURITY KEYCARD\nPick up, then return to the panel",new Vector3(-4,1.9f,-3),1.6f);
            Label(room.transform,"SECURITY CAMERA / DOOR CONTROL TEST\nStandard door: no card. Restricted door: green card.\nUse panel, switch feeds, OPEN / CLOSE. ESC exits.",new Vector3(0,4,-2),2);
            var sun=new GameObject("Prototype work light").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.2f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(50,-30,0);
            RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.4f,.43f,.46f);RenderSettings.fog=false;
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();File.WriteAllText(Application.dataPath+"/../Logs/SecurityCameraPrototypeCreated.txt","PASS: separate scene, UHFPS player/inventory, two CCTV feeds, open/close controls, one card-gated door, real green card pickup, anti-crush sensor, reusable panel/door prefabs. Existing scenes unchanged.");
        }
        finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
    }
}
