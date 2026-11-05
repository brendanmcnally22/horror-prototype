using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using Blockout.Discovery;
using UHFPS.Runtime;

public static class HeavyLeverPrototypeSetup
{
    public const string ScenePath="Assets/Blockout/Discovery/Scenes/Heavy Lever Prototype.unity";
    const string Folder="Assets/Blockout/Discovery/Heavy Lever Prototype";
    static Material steel,dark,yellow,glove,concrete;
    [MenuItem("Tools/Dead Air/Preview Heavy Lever Prototype")]
    public static void Preview()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit play mode first.");
        var previous=SceneManager.GetActiveScene();var preview=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(preview);
        try
        {
            var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Heavy Two-Hand Lever.prefab"));g.transform.position=new Vector3(1000,0,0);
            var lever=g.GetComponent<HeavyTwoHandLever>();lever.HandlePivot.localRotation=Quaternion.identity;lever.PrototypeHands.SetActive(true);
            var light=new GameObject("Preview key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(35,-30,0);
            var camera=new GameObject("Preview camera").AddComponent<Camera>();camera.transform.position=new Vector3(1002.6f,2.4f,-4);camera.transform.LookAt(new Vector3(1000,1.4f,0));camera.fieldOfView=45;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.065f,.08f);
            foreach(var t in g.GetComponentsInChildren<TMP_Text>())t.ForceMeshUpdate(true,true);
            var target=new RenderTexture(1000,1000,24);camera.targetTexture=target;camera.Render();var old=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(1000,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1000,1000),0,0);image.Apply();File.WriteAllBytes(Application.dataPath+"/../Logs/HeavyLeverPreview.png",image.EncodeToPNG());
            RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);
        }
        finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(preview,true);}
    }
    static Material Mat(string name,Color color,float metallic=0)
    {
        string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
        m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=color;m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",.35f);AssetDatabase.CreateAsset(m,path);return m;
    }
    static GameObject Shape(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material,bool collision=true)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.layer=11;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;
        if(!collision)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static void Text(Transform parent,string value,Vector3 pos,float size=2.5f)
    {
        var g=new GameObject("Label - "+value);g.transform.SetParent(parent,false);g.transform.localPosition=pos;
        var text=g.AddComponent<TextMeshPro>();text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ThunderWire Studio/UHFPS/Content/Fonts/Roboto Condensed/TMP/RobotoCondensed-Bold SDF.asset");
        text.text=value;text.fontSize=size;text.rectTransform.sizeDelta=new Vector2(5,.9f);text.alignment=TextAlignmentOptions.Center;text.color=Color.white;
    }
    static AudioClip Loop()
    {
        string path=Folder+"/Mechanical ratchet loop.wav";
        if(!File.Exists(path))using(var writer=new BinaryWriter(File.Create(path)))
        {
            const int rate=22050,count=44100;writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
            var random=new System.Random(42);for(int i=0;i<count;i++){double t=(double)i/rate;double ratchet=Math.Exp(-(t*9%1)*24);double signal=.16*Math.Sin(2*Math.PI*70*t)+.09*Math.Sin(2*Math.PI*143*t)+ratchet*.35*(random.NextDouble()*2-1)+.07*(random.NextDouble()*2-1);writer.Write((short)(signal*26000));}
        }
        AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    [MenuItem("Tools/Dead Air/Create Heavy Lever Prototype Scene")]
    public static void Create()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit play mode first.");
        if(File.Exists(ScenePath)){Debug.Log("Heavy lever scene already exists; preserved.");return;}
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        steel=Mat("Worn steel",new Color(.24f,.29f,.3f),.75f);dark=Mat("Mechanism black",new Color(.035f,.045f,.05f),.5f);
        yellow=Mat("Warning ochre",new Color(.75f,.42f,.08f),.35f);glove=Mat("Prototype gloves",new Color(.12f,.13f,.14f));concrete=Mat("Test chamber concrete",new Color(.3f,.33f,.34f));
        var loop=Loop();var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try
        {
            var setup=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blockout/Prefabs/Setup/Player Setup.prefab");if(!setup)throw new Exception("Wired player setup missing.");
            var hero=(GameObject)PrefabUtility.InstantiatePrefab(setup,scene);var player=hero.GetComponentInChildren<PlayerManager>(true);player.transform.SetPositionAndRotation(new Vector3(0,.1f,-3.2f),Quaternion.identity);
            var room=new GameObject("TEST CHAMBER — isolated prototype");
            Shape(room.transform,"Floor",PrimitiveType.Cube,new Vector3(0,-.25f,2),new Vector3(18,.5f,18),concrete);
            Shape(room.transform,"Back wall",PrimitiveType.Cube,new Vector3(0,2,8),new Vector3(18,4,.4f),concrete);
            Shape(room.transform,"Left wall",PrimitiveType.Cube,new Vector3(-9,2,2),new Vector3(.4f,4,18),concrete);
            Shape(room.transform,"Right wall",PrimitiveType.Cube,new Vector3(9,2,2),new Vector3(.4f,4,18),concrete);
            var root=new GameObject("HEAVY TWO-HAND LEVER");root.layer=6;root.transform.position=new Vector3(0,0,1);
            var lever=root.AddComponent<HeavyTwoHandLever>();var hit=root.AddComponent<BoxCollider>();hit.center=new Vector3(0,1.35f,-.3f);hit.size=new Vector3(1.6f,2,.8f);hit.isTrigger=true;
            Shape(root.transform,"Wall pedestal",PrimitiveType.Cube,new Vector3(0,1.25f,.28f),new Vector3(2.2f,2.5f,.6f),concrete);
            Shape(root.transform,"Steel mounting plate",PrimitiveType.Cube,new Vector3(0,1.4f,-.06f),new Vector3(1.65f,1.9f,.15f),dark);
            foreach(int side in new[]{-1,1})
            {
                var drum=Shape(root.transform,"Pivot drum",PrimitiveType.Cylinder,new Vector3(side*.63f,1.3f,-.23f),new Vector3(.42f,.14f,.42f),steel,false);drum.transform.localRotation=Quaternion.Euler(0,0,90);
                foreach(float y in new[]{.65f,2.1f})Shape(root.transform,"Mounting bolt",PrimitiveType.Sphere,new Vector3(side*.7f,y,-.16f),Vector3.one*.09f,steel,false);
            }
            var pivot=new GameObject("Handle pivot").transform;pivot.SetParent(root.transform,false);pivot.localPosition=new Vector3(0,1.3f,-.28f);lever.HandlePivot=pivot;
            foreach(int side in new[]{-1,1})Shape(pivot,"Heavy side arm",PrimitiveType.Cube,new Vector3(side*.59f,0,-.37f),new Vector3(.1f,.13f,.8f),steel,false);
            var bar=Shape(pivot,"Two-hand crossbar",PrimitiveType.Cylinder,new Vector3(0,0,-.76f),new Vector3(.13f,.7f,.13f),yellow,false);bar.transform.localRotation=Quaternion.Euler(0,0,90);
            foreach(int side in new[]{-1,1}){var grip=Shape(pivot,"Grip sleeve",PrimitiveType.Cylinder,new Vector3(side*.36f,0,-.76f),new Vector3(.145f,.14f,.145f),dark,false);grip.transform.localRotation=Quaternion.Euler(0,0,90);}
            var hands=new GameObject("Prototype two-hand grip — replace with authored hand animation");hands.transform.SetParent(pivot,false);lever.PrototypeHands=hands;
            foreach(int side in new[]{-1,1})
            {
                Shape(hands.transform,side<0?"Left glove":"Right glove",PrimitiveType.Cube,new Vector3(side*.36f,-.015f,-.8f),new Vector3(.14f,.12f,.18f),glove,false);
                for(int f=0;f<4;f++)Shape(hands.transform,"Gripping finger",PrimitiveType.Capsule,new Vector3(side*.36f+(f-1.5f)*.032f,.045f,-.76f),new Vector3(.027f,.07f,.027f),glove,false);
                var arm=Shape(hands.transform,"Sleeve",PrimitiveType.Capsule,new Vector3(side*.4f,-.08f,-1.01f),new Vector3(.13f,.17f,.13f),glove,false);arm.transform.localRotation=Quaternion.Euler(65,0,side*12);
            }
            hands.SetActive(false);pivot.localRotation=Quaternion.Euler(lever.RaisedAngle,0,0);
            var audio=root.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=1;audio.volume=.8f;audio.minDistance=1.5f;audio.maxDistance=22;audio.rolloffMode=AudioRolloffMode.Linear;
            lever.MechanismAudio=audio;lever.PullLoop=loop;lever.LatchSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ThunderWire Studio/UHFPS/Content/Sounds/Interact/Lever/Lever_On_1.wav");
            Text(root.transform,"SECURITY OVERRIDE\nTWO-HAND MANUAL RELEASE",new Vector3(0,2.85f,-.1f),2.2f);
            PrefabUtility.SaveAsPrefabAsset(root,Folder+"/Heavy Two-Hand Lever.prefab");
            var shutter=new GameObject("TEST SHUTTER");shutter.transform.position=new Vector3(4,0,4);
            var mover=shutter.AddComponent<PrototypeMover>();mover.MovingPart=Shape(shutter.transform,"Moving leaf",PrimitiveType.Cube,new Vector3(0,1.5f,0),new Vector3(2.8f,3,.3f),steel).transform;mover.Travel=new Vector3(0,3.2f,0);mover.Speed=.65f;
            UnityEventTools.AddPersistentListener(lever.OnCompleted,mover.Open);UnityEventTools.AddPersistentListener(lever.OnReset,mover.Close);
            Text(shutter.transform,"TEST SHUTTER",new Vector3(0,3.6f,-.2f));
            var dummy=Shape(null,"HEARING DUMMY — friend's AI integration point",PrimitiveType.Capsule,new Vector3(-6,1,4),new Vector3(.65f,1,.65f),yellow,false);
            var hearing=dummy.AddComponent<HeavyLeverHearingReceiver>();var behaviour=dummy.AddComponent<HeavyLeverHearingDummy>();behaviour.Indicator=dummy.GetComponent<Renderer>();UnityEventTools.AddPersistentListener(hearing.OnInvestigatePosition,behaviour.Investigate);
            Text(dummy.transform,"HEARING TEST DUMMY\nNoise → investigate (no attacks)",new Vector3(0,1.6f,0),1.4f);
            var reset=Shape(null,"RESET STATION",PrimitiveType.Cube,new Vector3(-2,1,-.1f),new Vector3(.6f,1,.4f),yellow);reset.layer=6;
            var resetUse=reset.AddComponent<HeavyLeverResetStation>();resetUse.Lever=lever;resetUse.Dummy=behaviour;Text(reset.transform,"RESET TEST",new Vector3(0,1.1f,-.25f),1.8f);
            Text(room.transform,"HEAVY LEVER TEST\nHold INTERACT to pull for 5 seconds. Release to cancel.\nThe ratchet attracts the hearing dummy. Reset station is on the left.",new Vector3(0,3.7f,1.4f),2);
            var light=new GameObject("Test chamber key light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(45,-30,0);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.25f,.28f,.31f);
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            File.WriteAllText(Application.dataPath+"/../Logs/HeavyLeverPrototypeCreated.txt","Created isolated heavy lever scene and reusable prefab. Existing scene preserved. Five-second hold, early release, two prototype grip hands, positional ratchet sound, hearing adapter and dummy, test shutter and reset station.");
        }
        finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
    }
}
