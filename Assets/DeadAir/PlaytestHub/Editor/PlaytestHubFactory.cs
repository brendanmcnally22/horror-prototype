using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Blockout.Discovery;
using DeadAir.Playtesting;
using UHFPS.Runtime;

public static partial class PlaytestHubSetup
{
    const string DiscoveryScene=Root+"/Scenes/AI Discovery Trial.unity";
    const string OriginalScene="Assets/Blockout/Discovery/Scenes/Discovery Blank.unity";
    const string MenuScene=Root+"/Scenes/Playtest Menu.unity";
    const string FeedbackScene=Root+"/Scenes/Playtest Feedback.unity";
    const string ResultsScene=Root+"/Scenes/Playtest Results.unity";
    const string SecurityScene=Root+"/Scenes/Security Terminal Trial.unity";
    const string LightingScene=Root+"/Scenes/Three Doors Lighting Trial.unity";
    static Texture2D wallpaper;
    static Material steel,trim,lens;
    [MenuItem("Tools/Dead Air/Set Up Playtest Hub")]
    public static void Apply()
    {
        var original=SceneManager.GetActiveScene();
        Directory.CreateDirectory("Logs/PlaytestHubSafety");
        foreach(var s in Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray())
            if(!string.IsNullOrEmpty(s.path))EditorSceneManager.SaveScene(s,"Logs/PlaytestHubSafety/"+s.name+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
        foreach(string folder in new[]{"Scenes","Resources","Prefabs","Materials"})Directory.CreateDirectory(Root+"/"+folder);
        AssetDatabase.Refresh();
        wallpaper=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/hamterbackground.png");
        if(!wallpaper)throw new Exception("hamterbackground.png is missing.");
        steel=Mat("Lever painted steel",new Color(.2f,.23f,.24f));trim=Mat("Door and lever trim",new Color(.08f,.1f,.11f));lens=Mat("Lever status lens",Color.white,true);
        UpgradeTechnologyPrefabs();
        int leverCount=0,crtCount=0;
        foreach(string path in new[]{OriginalScene}.Where(File.Exists))
        {
            WithScene(path,scene=>
            {
                var before=All(scene).Where(t=>t.name!="Desktop - Hamster Wallpaper").ToDictionary(t=>t,t=>(t.parent,t.localPosition,t.localRotation,t.localScale));
                leverCount+=UpgradeLevers(scene);crtCount+=UpgradeCRTInstances(scene);
                foreach(var t in before.Keys){var b=before[t];if(!t||t.parent!=b.Item1||t.localPosition!=b.Item2||t.localRotation!=b.Item3||t.localScale!=b.Item4)throw new Exception("An existing transform changed during upgrades: "+(t?t.name:"deleted"));}
                Save(scene);
            });
        }
        if(!File.Exists(DiscoveryScene)&&!AssetDatabase.CopyAsset(OriginalScene,DiscoveryScene))throw new Exception("Could not create AI + Discovery trial from the built facility.");
        WithScene(DiscoveryScene,scene=>{UpgradeCRTInstances(scene);ExperienceMarker(scene,"discovery",HubPage.Experience,"Find and return three fuses. Use F6 to change experience.");Save(scene);});
        if(!File.Exists(SecurityScene)&&!AssetDatabase.CopyAsset(DiscoveryScene,SecurityScene))throw new Exception("Could not create security trial scene.");
        else if(new FileInfo(SecurityScene).Length<50000)
            WithScene(DiscoveryScene,scene=>EditorSceneManager.SaveScene(scene,SecurityScene,true));
        WithScene(SecurityScene,scene=>
        {
            UpgradeCRTInstances(scene);
            ExperienceMarker(scene,"security",HubPage.Experience,"Operate the security terminal for 60 seconds. F6 switches experiences.");
            var terminal=All(scene).Select(t=>t.GetComponent<VirgoTerminal>()).FirstOrDefault(t=>t&&!t.PhoneOnly);
            if(!All(scene).Any(t=>t.GetComponent<SecurityTrialTerminal>()))
            {
                if(!terminal)
                {
                    var computer=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blockout/Discovery/Prefabs/Virgo Technology/Virgo Computer - Staff.prefab"),scene);
                    var hero=All(scene).Select(t=>t.GetComponent<PlayerManager>()).FirstOrDefault(p=>p);
                    computer.transform.position=hero?hero.transform.position+new Vector3(1.5f,0,1.5f):new Vector3(-90.52f,1.223f,5.281f);
                    terminal=computer.GetComponent<VirgoTerminal>();
                }
                var host=terminal.gameObject;UnityEngine.Object.DestroyImmediate(terminal);host.name="SECURITY TERMINAL - Assign Camera Feeds";host.AddComponent<SecurityTrialTerminal>();
            }
            foreach(var power in All(scene).Select(t=>t.GetComponent<FacilityPowerSequence>()).Where(p=>p)){power.ShowPanelMarker=false;power.ObjectiveDisplay=null;}
            foreach(var display in All(scene).Select(t=>t.GetComponent<DiscoveryObjectiveDisplay>()).Where(p=>p)){if(display.Text)display.Text.gameObject.SetActive(false);display.enabled=false;}
            foreach(var objective in All(scene).Select(t=>t.GetComponent<DiscoveryObjectiveTrigger>()).Where(p=>p))objective.enabled=false;
            Save(scene);
        });
        MakeShell(MenuScene,HubPage.Menu);MakeShell(FeedbackScene,HubPage.Feedback);MakeShell(ResultsScene,HubPage.Results);
        if(!File.Exists(LightingScene))MakeLightingScene();
        WithScene(LightingScene,scene=>{ConfigureLightingTrial(scene);Save(scene);});
        var config=AssetDatabase.LoadAssetAtPath<PlaytestHubConfig>(Root+"/Resources/DeadAirPlaytestHub.asset");
        if(!config){config=ScriptableObject.CreateInstance<PlaytestHubConfig>();AssetDatabase.CreateAsset(config,Root+"/Resources/DeadAirPlaytestHub.asset");}
        config.MenuScene=MenuScene;config.FeedbackScene=FeedbackScene;config.ResultsScene=ResultsScene;
        config.Experiences=new[]{
            new HubExperienceDefinition{Id="discovery",Title="AI + Discovery",Description="Explore the facility and return the three fuses.",ScenePath=DiscoveryScene,Kind=HubExperienceKind.Discovery},
            new HubExperienceDefinition{Id="security",Title="Security Terminal",Description="Operate security while the facility remains active.",ScenePath=SecurityScene,Kind=HubExperienceKind.Security},
            new HubExperienceDefinition{Id="lighting",Title="Three Doors",Description="Try three levers under green, red, and flickering white light.",ScenePath=LightingScene,Kind=HubExperienceKind.Lighting}
        };EditorUtility.SetDirty(config);
        var recording=AssetDatabase.LoadAssetAtPath<PlaytestSettings>("Assets/DeadAir/DeveloperTools/Resources/DeadAirPlaytestSettings.asset");
        if(recording){recording.Enabled=true;recording.AllowInReleaseBuilds=true;recording.AutoRecord=true;EditorUtility.SetDirty(recording);}
        var paths=new[]{MenuScene,DiscoveryScene,SecurityScene,LightingScene,FeedbackScene,ResultsScene};
        EditorBuildSettings.scenes=paths.Select(p=>new EditorBuildSettingsScene(p,true)).Concat(EditorBuildSettings.scenes.Where(s=>!paths.Contains(s.path))).ToArray();
        AssetDatabase.SaveAssets();SceneManager.SetActiveScene(original);
        File.WriteAllText("Logs/PlaytestHubSetupResult.txt","PASS: menu, feedback, results booklet, security trial and 3-door lighting trial created. Existing AI+Discovery scene connected. Enemy slots intentionally empty; cameras assigned by designer. Green/red/flickering-white doors. Upgraded "+leverCount+" scene levers and "+crtCount+" placed CRT monitors; original transforms preserved. Hamster wallpaper assigned. Menu is first enabled build scene.\n");
    }
    static Transform[] All(Scene scene)=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
    static void Save(Scene scene)
    {
        foreach(var c in All(scene).SelectMany(t=>t.GetComponents<Component>()).Where(c=>c)){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    static void WithScene(string path,Action<Scene> action)
    {
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);try{action(scene);}finally{SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
    static PlaytestExperience ExperienceMarker(Scene scene,string id,HubPage page,string objective="")
    {
        var marker=All(scene).Select(t=>t.GetComponent<PlaytestExperience>()).FirstOrDefault(t=>t);
        if(!marker)marker=new GameObject("PLAYTEST EXPERIENCE - Settings and Enemy Slot").AddComponent<PlaytestExperience>();
        marker.Page=page;marker.ExperienceId=id;marker.Objective=objective;
        if(page==HubPage.Experience&&id!="lighting"&&!marker.EnemyRoot)
        {
            var slot=new GameObject("ENEMY SLOT - Place your enemy here");slot.transform.SetParent(marker.transform,false);slot.transform.position=new Vector3(-125,.5f,10);marker.EnemyRoot=slot;
        }
        return marker;
    }
    static void MakeShell(string path,HubPage page)
    {
        if(File.Exists(path))return;var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try{ExperienceMarker(scene,"",page);var camera=new GameObject("Menu Camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.03f,.05f,.06f);camera.gameObject.AddComponent<AudioListener>();EditorSceneManager.SaveScene(scene,path);}
        finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
    }
    static Material Mat(string name,Color color,bool unlit=false)
    {
        string path=Root+"/Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material)return material;
        material=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(material,path);return material;
    }
    static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool collision=true)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;if(parent)go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.layer=11;go.GetComponent<Renderer>().sharedMaterial=material;
        if(!collision)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
    }
    static void MakeLightingScene()
    {
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try
        {
            ExperienceMarker(scene,"lighting",HubPage.Experience,"Open all three doors and compare how their lighting feels.");
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.045f,.05f,.06f);
            var floor=Mat("Lighting test floor",new Color(.16f,.17f,.18f));var wall=Mat("Lighting test wall",new Color(.3f,.31f,.32f));var door=Mat("Identical test doors",new Color(.18f,.2f,.2f));
            var root=new GameObject("THREE DOORS - Small Lighting Test").transform;
            Box(root,"Floor",new Vector3(0,-.15f,1),new Vector3(13,.3f,14),floor);
            Box(root,"Left wall",new Vector3(-6.4f,1.5f,1),new Vector3(.2f,3,14),wall);
            Box(root,"Right wall",new Vector3(6.4f,1.5f,1),new Vector3(.2f,3,14),wall);
            Box(root,"Front wall",new Vector3(0,1.5f,-5.9f),new Vector3(13,3,.2f),wall);
            Box(root,"Back wall",new Vector3(0,1.5f,7.8f),new Vector3(13,3,.2f),wall);
            Box(root,"Ceiling",new Vector3(0,3.1f,1),new Vector3(13,.2f,14),wall);
            string[] names={"Green","Red","Flickering white"};Color[] colors={new Color(.08f,1,.22f),new Color(1,.045f,.025f),Color.white};
            for(int i=0;i<3;i++)
            {
                float x=(i-1)*3.7f;var host=new GameObject("Lighting Door - "+names[i]);host.transform.SetParent(root,false);host.transform.localPosition=new Vector3(x,0,4);host.layer=6;
                var carriage=new GameObject("Carriage").transform;carriage.SetParent(host.transform,false);
                Box(carriage,"Door leaf",new Vector3(0,1.1f,0),new Vector3(1.4f,2.2f,.15f),door);
                Box(root,"Door header",new Vector3(x,2.6f,4),new Vector3(3.7f,.8f,.25f),wall);
                foreach(int side in new[]{-1,1})Box(root,"Door wall",new Vector3(x+side*1.275f,1.1f,4),new Vector3(1.1f,2.2f,.25f),wall);
                var mover=host.AddComponent<PrototypeMover>();mover.MovingPart=carriage;mover.Travel=new Vector3(1.6f,0,0);mover.Speed=1.2f;
                var use=host.AddComponent<BoxCollider>();use.isTrigger=true;use.center=new Vector3(0,1.1f,0);use.size=new Vector3(1.5f,2.2f,.35f);
                var interaction=host.AddComponent<LightingTestDoor>();interaction.Condition=names[i];interaction.Door=mover;
                var fixture=new GameObject(names[i]+" light");fixture.transform.SetParent(root,false);fixture.transform.localPosition=new Vector3(x,2.65f,3.4f);fixture.transform.localRotation=Quaternion.Euler(45,0,0);
                fixture.transform.localPosition=new Vector3(x,2.65f,2.8f);fixture.transform.localRotation=Quaternion.Euler(35,0,0);
                var light=fixture.AddComponent<Light>();light.type=LightType.Spot;light.spotAngle=55;light.innerSpotAngle=40;light.shadows=LightShadows.None;
                var flicker=fixture.AddComponent<PrototypeLight>();flicker.Color=colors[i];flicker.Intensity=3;flicker.Range=5;flicker.Flicker=i==2;flicker.MinimumBrightness=.05f;flicker.Interval=new Vector2(.08f,.4f);
                light.color=colors[i];light.range=5;light.intensity=3;
                Box(root,"Fixture",new Vector3(x,2.7f,3.85f),new Vector3(.55f,.09f,.18f),Mat(names[i]+" lens",colors[i],true),false);
            }
            foreach(float x in new[]{-1.85f,1.85f})Box(root,"Alcove divider",new Vector3(x,1.5f,6),new Vector3(.2f,3,3.8f),wall);
            var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blockout/Prefabs/Setup/Player Setup.prefab"),scene);
            var hero=player.GetComponentInChildren<PlayerManager>();hero.transform.position=new Vector3(0,.08f,-3.8f);hero.transform.rotation=Quaternion.identity;
            hero.gameObject.AddComponent<FacilityPlayerRules>();hero.gameObject.AddComponent<DesignPlayerStats>();
            EditorSceneManager.SaveScene(scene,LightingScene);
        }
        finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
    }
    static void ConfigureLightingTrial(Scene scene)
    {
        var marker=All(scene).Select(t=>t.GetComponent<PlaytestExperience>()).First(p=>p);
        marker.Objective="Try all three levers and compare the lights. Doors stay sealed.";
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Lever_7 - Interactive.prefab");
        foreach(var door in All(scene).Select(t=>t.GetComponent<LightingTestDoor>()).Where(d=>d))
        {
            if(door.Door){door.Door.enabled=false;door.Door.MovingPart.localPosition=Vector3.zero;}
            door.gameObject.layer=11;
            if(!door.Lever)
            {
                var lever=(GameObject)PrefabUtility.InstantiatePrefab(asset,door.transform);
                lever.name=door.Condition+" - Door Lever";lever.transform.localPosition=new Vector3(-1.05f,1.25f,-.24f);
                door.Lever=lever.GetComponent<PrototypeLever>();door.Lever.Targets=Array.Empty<PrototypeMover>();
                door.Lever.Label="Door lever";door.Lever.FollowTarget=false;
            }
            door.Lever.OffPrompt="Pull lever";door.Lever.OnPrompt="Return lever";
            if(!door.transform.Find("Opaque sealed backing"))Box(door.transform,"Opaque sealed backing",new Vector3(0,1.1f,.2f),new Vector3(1.5f,2.2f,.15f),steel);
        }
        var root=All(scene).First(t=>t.name=="THREE DOORS - Small Lighting Test");
        foreach(int side in new[]{-1,1})if(!root.Find("Sealed outer wall "+side))Box(root,"Sealed outer wall "+side,new Vector3(side*5.95f,1.5f,4),new Vector3(.9f,3,.25f),steel);
        var white=All(scene).FirstOrDefault(t=>t.name=="Flickering white light");
        if(white)
        {
            var fixture=All(scene).Where(t=>t.name=="Fixture"&&t.GetComponent<Renderer>()).OrderBy(t=>Vector3.Distance(t.position,white.position)).First();
            var flicker=fixture.GetComponent<LightingTestLens>();if(!flicker)flicker=fixture.gameObject.AddComponent<LightingTestLens>();flicker.Lens=fixture.GetComponent<Renderer>();flicker.Source=white.GetComponent<Light>();
        }
    }
}
