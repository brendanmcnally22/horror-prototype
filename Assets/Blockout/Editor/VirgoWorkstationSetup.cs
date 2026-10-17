using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Blockout.Discovery;

[InitializeOnLoad]
public static class VirgoWorkstationSetup
{
    const string Root="Assets/Blockout/Discovery";
    const string Folder=Root+"/Prefabs/Virgo Technology";
    static Material beige,dark,screen,keys;
    static VirgoWorkstationSetup(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(!File.Exists("Logs/VirgoSetup.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete("Logs/VirgoSetup.request");try{Create();}catch(Exception e){File.WriteAllText("Logs/VirgoSetup.txt",e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Tools/Blockout/Create Virgo Workstations and Telephone")]
    public static void Create()
    {
        var original=SceneManager.GetActiveScene();
        var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
        var dirty=scenes.Select(s=>s.isDirty).ToArray();
        var before=scenes.SelectMany(s=>s.GetRootGameObjects()).SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToDictionary(t=>t,t=>(t.parent,t.localPosition,t.localRotation,t.localScale));
        Directory.CreateDirectory(Folder);Directory.CreateDirectory(Root+"/Data");Directory.CreateDirectory(Root+"/Materials/Technology");AssetDatabase.Refresh();
        var temp=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(temp);
        try
        {
            beige=Mat("Aged beige",new Color(.64f,.61f,.5f));dark=Mat("CRT graphite",new Color(.055f,.065f,.065f));keys=Mat("Key caps",new Color(.8f,.78f,.68f));screen=Mat("Virgo phosphor",new Color(.04f,.3f,.28f));
            var archive=AssetDatabase.LoadAssetAtPath<VirgoArchive>(Root+"/Data/Virgo Archive.asset");
            if(!archive)
            {
                archive=ScriptableObject.CreateInstance<VirgoArchive>();
                archive.Records=new[]{
                    Record("01 / Welcome to Virgo",0,"VIRGO CORPORATION\nPersonnel orientation / revised 1995\n\nOur work began with a government medical research contract in the 1950s. Our mandate: help patients whose bodies reject transplanted tissue.\n\nEvery restored life begins with careful work. Record your observations. Maintain confidentiality. Respect restricted areas."),
                    Record("02 / Emergency power",0,"FACILITIES NOTICE\n\nAmber illumination means the facility is operating on reserve power. It is not a total blackout.\n\nThe electrical panel by the entry route requires THREE replacement fuses. Insert each fuse as you find it; do not wait to carry all three. The first restores a local circuit. The third restores normal facility lighting.\n\nDo not bypass missing fuses. Report damaged equipment to extension 204."),
                    Record("03 / Carrier maintenance",0,"LINE MONITOR / SERVICE CARD\n\nThe lower maintenance carrier is 96.40 MHz. Tune the workstation receiver, hold a stable signal for two seconds, then decode.\n\nThe carrier carries a repeating service message. A strong signal alone is not confirmation; wait for the lock indicator.\n\nPlease stop leaving the receiver at full volume."),
                    Record("04 / Staff notices",0,"BREAK ROOM BULLETIN\n\nRelay Break is approved for meal periods. It is not an electrical training simulator.\n\nPlease return mugs to their owners. Property left in lockers is to remain there until Personnel collects it.\n\nNight staff: some extensions route to old recorded messages. A ringing line does not establish that the department is occupied."),
                    Record("05 / Research milestones",1,"ARCHIVE ABSTRACT / MEDICAL DIVISION\n\nEarly studies addressed the rejection of transplanted biological tissue. Later reports describe successful integration across previously incompatible donor and recipient pairs.\n\nThe archive describes a treatment referred to only as the serum. Its production records are absent from this terminal.\n\nA handwritten note on the scanned cover reads: 'Acceptance is not the same as adaptation.'"),
                    Record("06 / Personnel discrepancy",1,"PERSONNEL RECONCILIATION\n\nSeveral station employees are listed as having stopped attending work. Their personal property remains on site.\n\nThe dates on the departure forms do not match the final internal access entries. Each amendment was approved by P. Dutton.\n\nStatus: reconciliation suspended by Director's Office."),
                    Record("07 / Dutton private notes",2,"DIRECTOR'S LOCAL COPY\n\nWe learned to replace what was lost. Why did we decide that restoration was the limit?\n\nThe old committees measured success against an ordinary body. Their standard was the flaw.\n\nAttachment references: observation ward; candidate assessments. The source files are held physically in this office."),
                    Record("08 / Candidate intake",2,"INTAKE CROSS-REFERENCE\n\nRadio station applications have been assigned internal assessment numbers. The criteria extend far beyond employment requirements.\n\nEmergency contacts, unexplained absence risk, and physical observations appear beside interview notes.\n\nThe newest entry is marked NIGHT SHIFT / ARRIVAL CONFIRMED. Photograph retained in the paper file.")};
                archive.Extensions=new[]{
                    Extension("204","Maintenance","RECORDED SERVICE MESSAGE: Reserve supply active. Replace the three missing panel fuses. Normal lighting returns once all circuits are connected."),
                    Extension("101","Reception","The desk is unattended. Visitors must remain in reception until collected."),
                    Extension("309","Records","Your request has been logged. Personnel amendments require approval from the Director's Office."),
                    Extension("000","Operator","[A relay clicks. There is breathing on the line. The connection closes.]")};
                AssetDatabase.CreateAsset(archive,Root+"/Data/Virgo Archive.asset");
            }
            Computer("Virgo Computer - Reception",0,archive);Computer("Virgo Computer - Staff",1,archive);Computer("Virgo Computer - Director",2,archive);Phone(archive);
            AssetDatabase.SaveAssets();
            foreach(var p in before)if(!p.Key||p.Key.parent!=p.Value.Item1||p.Key.localPosition!=p.Value.Item2||p.Key.localRotation!=p.Value.Item3||p.Key.localScale!=p.Value.Item4)throw new Exception("Scene layout changed unexpectedly.");
            for(int i=0;i<scenes.Length;i++)if(scenes[i].isDirty!=dirty[i])throw new Exception("Scene dirty state changed.");
            File.WriteAllText("Logs/VirgoSetup.txt","PASS: three clearance-specific computer prefabs, staff telephone and editable archive created. Existing scene transforms and dirty states preserved. Nothing placed in the map.\n");
        }
        finally{SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(temp,true);}
        Selection.activeObject=AssetDatabase.LoadAssetAtPath<DefaultAsset>(Folder);EditorGUIUtility.PingObject(Selection.activeObject);
    }
    static VirgoArchive.Record Record(string title,int level,string body)=>new VirgoArchive.Record{Title=title,Clearance=level,Body=body};
    static VirgoArchive.Extension Extension(string number,string department,string transcript)=>new VirgoArchive.Extension{Number=number,Department=department,Transcript=transcript};
    static Material Mat(string name,Color color)
    {
        string path=Root+"/Materials/Technology/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
        m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.2f);
        if(name=="Virgo phosphor"){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.8f);}AssetDatabase.CreateAsset(m,path);return m;
    }
    static void Box(GameObject root,string name,Vector3 pos,Vector3 size,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root.transform,false);g.transform.localPosition=pos;g.transform.localScale=size;
        g.GetComponent<Renderer>().sharedMaterial=material;g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
    }
    static void Computer(string name,int clearance,VirgoArchive archive)
    {
        var g=new GameObject(name);g.layer=6;
        var terminal=g.AddComponent<VirgoTerminal>();terminal.Archive=archive;terminal.Clearance=clearance;terminal.StationName=new[]{"RECEPTION-01","RECORDS-02","DIRECTOR-01"}[clearance];
        var collider=g.AddComponent<BoxCollider>();collider.center=new Vector3(0,.37f,0);collider.size=new Vector3(.78f,.74f,.8f);
        Box(g,"Desktop chassis",new Vector3(0,.1f,.09f),new Vector3(.65f,.2f,.49f),beige);
        Box(g,"Monitor stand",new Vector3(0,.24f,.12f),new Vector3(.25f,.08f,.25f),beige);
        Box(g,"Deep CRT shell",new Vector3(0,.5f,.14f),new Vector3(.55f,.44f,.43f),beige);
        Box(g,"Screen recess",new Vector3(0,.51f,-.08f),new Vector3(.46f,.34f,.018f),dark);
        Box(g,"Phosphor screen",new Vector3(0,.51f,-.094f),new Vector3(.415f,.29f,.014f),screen);
        Box(g,"Floppy drive",new Vector3(.15f,.12f,-.16f),new Vector3(.22f,.025f,.015f),dark);
        Box(g,"Power LED",new Vector3(-.25f,.1f,-.16f),new Vector3(.015f,.015f,.015f),screen);
        Box(g,"Keyboard",new Vector3(-.04f,.035f,-.32f),new Vector3(.57f,.055f,.19f),beige);
        for(int row=0;row<4;row++)for(int col=0;col<13;col++)Box(g,"Key",new Vector3(-.3f+col*.041f,.07f,-.38f+row*.038f),new Vector3(.034f,.025f,.029f),keys);
        Box(g,"Mouse",new Vector3(.35f,.04f,-.33f),new Vector3(.085f,.055f,.13f),beige);
        Save(g);
    }
    static void Phone(VirgoArchive archive)
    {
        var g=new GameObject("Virgo Staff Telephone");g.layer=6;var t=g.AddComponent<VirgoTerminal>();t.Archive=archive;t.PhoneOnly=true;t.StationName="EXTENSION";
        var box=g.AddComponent<BoxCollider>();box.center=new Vector3(0,.075f,0);box.size=new Vector3(.26f,.15f,.25f);
        Box(g,"Phone base",new Vector3(0,.035f,0),new Vector3(.24f,.07f,.22f),beige);
        Box(g,"Handset",new Vector3(0,.105f,.055f),new Vector3(.26f,.06f,.075f),dark);
        for(int i=0;i<12;i++)Box(g,"Dial key",new Vector3((i%3-1)*.038f,.08f,-.085f+i/3*.029f),new Vector3(.03f,.018f,.022f),keys);
        Save(g);
    }
    static void Save(GameObject g){string path=Folder+"/"+g.name+".prefab";if(!AssetDatabase.LoadAssetAtPath<GameObject>(path))PrefabUtility.SaveAsPrefabAsset(g,path);UnityEngine.Object.DestroyImmediate(g);}
}
