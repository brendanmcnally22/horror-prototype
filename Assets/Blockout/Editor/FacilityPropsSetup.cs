using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UHFPS.Runtime;
using Blockout.Discovery;

[InitializeOnLoad]
public static class FacilityPropsSetup
{
    const string Root="Assets/Blockout/Discovery";
    const string Folder=Root+"/Prefabs/Facility Props";
    static Material metal,dark,white,teal,yellow,rubber,concrete;
    static int created;
    static FacilityPropsSetup(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(!File.Exists("Logs/FacilityProps.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete("Logs/FacilityProps.request");
        try{Create();}catch(Exception e){File.WriteAllText("Logs/FacilityPropsResult.txt",e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Tools/Blockout/Create Facility Furniture and Debris Prefabs")]
    public static void Create()
    {
        var previous=SceneManager.GetActiveScene();
        var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
        var before=scenes.SelectMany(s=>s.GetRootGameObjects()).SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
            .ToDictionary(t=>t,t=>(parent:t.parent,position:t.localPosition,rotation:t.localRotation,scale:t.localScale,active:t.gameObject.activeSelf));
        var dirty=scenes.Select(s=>s.isDirty).ToArray();
        Directory.CreateDirectory(Folder);Directory.CreateDirectory(Root+"/Materials/Props");AssetDatabase.Refresh();
        var work=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(work);
        try
        {
            metal=Mat("Steel",new Color(.39f,.46f,.5f));dark=Mat("Graphite",new Color(.09f,.13f,.16f));white=Mat("Lab enamel",new Color(.72f,.77f,.73f));
            teal=Mat("Containment teal",new Color(.12f,.37f,.35f));yellow=Mat("Caution yellow",new Color(.95f,.62f,.08f));rubber=Mat("Rubber",new Color(.035f,.045f,.05f));concrete=Mat("Concrete",new Color(.34f,.32f,.28f));
            created=0;
            var g=New("Lab Bench - 2m");Table(g,2,.8f,.9f,white);Box(g,"Backboard",V(2,.35f,.06f),V(0,1.05f,.37f),teal);Save(g);
            g=New("Specimen Examination Table");Table(g,2.2f,.85f,.85f,metal);Box(g,"Inset pad",V(1.95f,.05f,.62f),V(0,.9f,0),teal);Box(g,"Base cabinet",V(1.3f,.65f,.55f),V(0,.325f,0),white);Save(g);
            g=New("Office Desk");Table(g,1.6f,.75f,.75f,white);Box(g,"Drawer pedestal",V(.4f,.68f,.65f),V(-.53f,.34f,0),dark);Screen(g,V(.2f,.99f,.16f));Save(g);
            g=New("Lab Chair");Box(g,"Seat",V(.48f,.09f,.48f),V(0,.46f,0),teal);Box(g,"Backrest",V(.48f,.48f,.09f),V(0,.75f,.2f),teal);Legs(g,.4f,.4f,.42f);Save(g);
            g=New("Storage Shelving - 2m");Rack(g,2,.65f,2.1f);Save(g);
            g=New("Locker Bank - Three");Box(g,"Housing",V(1.5f,1.9f,.55f),V(0,.95f,0),metal);for(int i=0;i<3;i++){float x=(i-1)*.49f;Box(g,"Door",V(.46f,1.8f,.035f),V(x,.95f,-.295f),white,false);Box(g,"Handle",V(.035f,.22f,.055f),V(x+.15f,1,-.33f),dark,false);}Save(g);
            g=New("Sealed Equipment Cabinet");Box(g,"Body",V(1.1f,1.8f,.6f),V(0,.9f,0),white);Box(g,"Door",V(.96f,1.65f,.04f),V(0,.9f,-.33f),teal,false);Box(g,"Handle",V(.05f,.35f,.08f),V(.35f,.9f,-.38f),metal,false);Save(g);
            g=New("Cold Storage Freezer");Box(g,"Insulated cabinet",V(1.4f,2.2f,.85f),V(0,1.1f,0),white);Box(g,"Heavy door",V(1.24f,2.02f,.09f),V(0,1.1f,-.46f),metal,false);Box(g,"Seal",V(.95f,.07f,.04f),V(0,1.6f,-.53f),yellow,false);Save(g);
            g=New("Containment Pod - Opaque Blockout");Cylinder(g,"Tank",V(1,1.1f,1),V(0,1.25f,0),teal);Cylinder(g,"Base",V(1.15f,.12f,1.15f),V(0,.12f,0),dark);Cylinder(g,"Top",V(1.15f,.1f,1.15f),V(0,2.4f,0),metal);Box(g,"Status panel",V(.4f,.55f,.12f),V(0,1.2f,-.52f),dark,false);Save(g);
            g=New("Control Console");Box(g,"Body",V(1.6f,.9f,.75f),V(0,.45f,0),metal);Screen(g,V(-.38f,1.15f,.1f));Screen(g,V(.38f,1.15f,.1f));Save(g);
            g=New("Supply Crate");Crate(g,V(0,.35f,0),V(.9f,.7f,.65f));Save(g);
            g=New("Waste Bin - Sealed");Box(g,"Container",V(.55f,.8f,.55f),V(0,.4f,0),teal);Box(g,"Lid",V(.6f,.07f,.6f),V(0,.835f,0),yellow);Save(g);
            g=New("Mobile Privacy Divider - Static");Box(g,"Screen",V(1.8f,1.4f,.09f),V(0,1.1f,0),teal);for(int i=-1;i<=1;i+=2){Box(g,"Post",V(.06f,1.8f,.06f),V(i*.86f,.9f,0),metal);Box(g,"Foot",V(.18f,.08f,.6f),V(i*.86f,.04f,0),dark);}Save(g);
            g=New("Gurney - Static");Table(g,2,.75f,.85f,metal);Box(g,"Mattress",V(1.9f,.12f,.68f),V(0,.95f,0),teal);Wheels(g,1.7f,.6f);Save(g);
            Cart("Supply Cart - HOLD TO MOVE",false);Cart("Equipment Cart - HOLD TO MOVE",true);
            g=New("Debris - Concrete Rubble");for(int i=0;i<9;i++){float x=(i%3-1)*.42f,z=(i/3-1)*.35f;var p=Box(g,"Rubble",V(.3f+(i%2)*.16f,.12f+(i%3)*.05f,.28f),V(x,.09f,z),concrete,false);p.transform.localRotation=Quaternion.Euler(0,i*47,0);}Footprint(g,V(1.35f,.25f,1.1f),V(0,.125f,0));Save(g);
            g=New("Debris - Broken Wall Panels");for(int i=0;i<4;i++){var p=Box(g,"Broken panel",V(.8f,.06f,.45f),V((i%2-.5f)*.6f,.04f+i*.035f,(i/2-.5f)*.35f),white,false);p.transform.localRotation=Quaternion.Euler(0,i*31,0);}Footprint(g,V(1.6f,.18f,1.1f),V(0,.09f,0));Save(g);
            g=New("Debris - Fallen Shelf");Box(g,"Bent shelf",V(1.8f,.09f,.65f),V(0,.18f,0),metal);for(int i=-1;i<=1;i+=2)Box(g,"Broken support",V(.08f,.45f,.08f),V(i*.85f,.225f,.25f),dark);Save(g);
            g=New("Debris - Scattered Crates");Crate(g,V(-.35f,.25f,0),V(.6f,.5f,.5f));Crate(g,V(.4f,.18f,.3f),V(.5f,.36f,.45f));Save(g);
            g=New("Debris - Cable Bundle");for(int i=0;i<5;i++){var p=Box(g,"Cable",V(1.5f,.035f,.035f),V(0,.025f+i*.008f,(i-2)*.1f),rubber,false);p.transform.localRotation=Quaternion.Euler(0,i*9-18,0);}Save(g);
            g=New("Debris - Floor Grate");for(int i=0;i<8;i++)Box(g,"Grate slat",V(1.2f,.045f,.04f),V(0,.03f,(i-3.5f)*.1f),metal,false);Footprint(g,V(1.2f,.06f,.8f),V(0,.03f,0));Save(g);
            AssetDatabase.SaveAssets();
            foreach(var pair in before){var t=pair.Key;var b=pair.Value;if(!t||t.parent!=b.parent||t.localPosition!=b.position||t.localRotation!=b.rotation||t.localScale!=b.scale||t.gameObject.activeSelf!=b.active)throw new Exception("Existing scene layout changed unexpectedly.");}
            for(int i=0;i<scenes.Length;i++)if(scenes[i].isDirty!=dirty[i])throw new Exception("Existing scene dirty state changed unexpectedly.");
            File.WriteAllText("Logs/FacilityPropsResult.txt","PASS: "+created+" new prefabs created; existing scene transforms and dirty states unchanged. Props have floor pivots and shared materials. No scene placement.\n");
        }
        finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(work,true);}
        Selection.activeObject=AssetDatabase.LoadAssetAtPath<DefaultAsset>(Folder);EditorGUIUtility.PingObject(Selection.activeObject);
    }
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static GameObject New(string name)=>new GameObject(name);
    static Material Mat(string name,Color color){string path=Root+"/Materials/Props/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.25f);AssetDatabase.CreateAsset(m,path);return m;}
    static GameObject Box(GameObject root,string name,Vector3 size,Vector3 pos,Material mat,bool collision=true)
    {
        var p=GameObject.CreatePrimitive(PrimitiveType.Cube);p.name=name;p.layer=11;p.transform.SetParent(root.transform,false);p.transform.localPosition=pos;p.transform.localScale=size;p.GetComponent<Renderer>().sharedMaterial=mat;p.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;if(!collision)UnityEngine.Object.DestroyImmediate(p.GetComponent<Collider>());return p;
    }
    static void Cylinder(GameObject root,string name,Vector3 size,Vector3 pos,Material mat){var p=GameObject.CreatePrimitive(PrimitiveType.Cylinder);p.name=name;p.layer=11;p.transform.SetParent(root.transform,false);p.transform.localPosition=pos;p.transform.localScale=size;p.GetComponent<Renderer>().sharedMaterial=mat;p.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;}
    static void Legs(GameObject g,float width,float depth,float height){foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Box(g,"Leg",V(.07f,height,.07f),V(x*(width/2-.035f),height/2,z*(depth/2-.035f)),metal);}
    static void Table(GameObject g,float w,float d,float h,Material mat){Box(g,"Work surface",V(w,.08f,d),V(0,h-.04f,0),mat);Legs(g,w-.12f,d-.12f,h-.08f);}
    static void Rack(GameObject g,float w,float d,float h){Legs(g,w,d,h);for(int i=0;i<4;i++)Box(g,"Shelf",V(w,.06f,d),V(0,.15f+i*(h-.2f)/3,0),metal);}
    static void Screen(GameObject g,Vector3 pos){Box(g,"Monitor stand",V(.08f,.2f,.08f),pos-V(0,.13f,0),dark,false);Box(g,"Monitor",V(.6f,.4f,.07f),pos, dark,false);Box(g,"Blank display",V(.53f,.32f,.01f),pos+V(0,0,-.04f),teal,false);}
    static void Crate(GameObject g,Vector3 pos,Vector3 size){Box(g,"Crate body",size,pos,metal);Box(g,"Caution band",V(size.x+.01f,.08f,size.z+.01f),pos,yellow,false);}
    static void Footprint(GameObject g,Vector3 size,Vector3 center){g.layer=11;var box=g.AddComponent<BoxCollider>();box.size=size;box.center=center;}
    static void Wheels(GameObject g,float w,float d){foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1}){var p=Box(g,"Caster",V(.1f,.18f,.16f),V(x*w/2,.09f,z*d/2),rubber,false);}}
    static void Cart(string name,bool cabinet)
    {
        var root=New(name);
        var body=GameObject.CreatePrimitive(PrimitiveType.Cube);body.name="Cart Body - Push Settings";body.layer=6;body.transform.SetParent(root.transform,false);body.transform.localPosition=V(0,.65f,0);
        body.GetComponent<Renderer>().enabled=false;
        var rigid=body.AddComponent<Rigidbody>();rigid.isKinematic=true;rigid.useGravity=false;rigid.interpolation=RigidbodyInterpolation.Interpolate;
        var audio=body.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=1;
        var settings=New("Movement adapter");settings.transform.SetParent(body.transform,false);settings.layer=2;
        var movement=settings.AddComponent<MovableObject>();movement.Rigidbody=rigid;movement.AudioSource=audio;movement.ForwardAxis=Axis.Z;
        movement.MoveDirection=MovableObject.MoveDirectionEnum.ForwardBackward;movement.AllowRotation=false;movement.HoldDistance=1.25f;movement.ObjectWeight=60;movement.WalkMultiplier=.55f;movement.LookMultiplier=.4f;movement.PlayerFeetOffset=.35f;movement.CollisionMask=LayerMask.GetMask("Default","Ground","Slope","NPC");
        movement.SlideVolume=0;movement.VolumeFadeSpeed=2;
        var handle=body.AddComponent<FacilityCartHandle>();handle.Movement=movement;handle.Label=name.Replace(" - HOLD TO MOVE","");
        var visual=New("Editable cart geometry");visual.transform.SetParent(body.transform,false);visual.transform.localPosition=V(0,-.65f,0);
        Box(visual,"Lower tray",V(.95f,.06f,.95f),V(0,.22f,0),metal,false);Box(visual,"Upper tray",V(.95f,.06f,.95f),V(0,1.05f,0),metal,false);
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Box(visual,"Corner upright",V(.05f,.8f,.05f),V(x*.43f,.63f,z*.43f),metal,false);
        Box(visual,"Yellow push handle",V(.85f,.06f,.07f),V(0,.95f,-.48f),yellow,false);
        if(cabinet)Box(visual,"Equipment case",V(.72f,.65f,.72f),V(0,.62f,0),teal,false);
        Wheels(visual,.85f,.85f);Save(root);
    }
    static void Save(GameObject g)
    {
        string path=Folder+"/"+g.name+".prefab";
        if(!AssetDatabase.LoadAssetAtPath<GameObject>(path))
        {
            if(g.transform.localScale!=Vector3.one||g.GetComponentsInChildren<Renderer>(true).Length==0)throw new Exception("Invalid prefab "+g.name);
            PrefabUtility.SaveAsPrefabAsset(g,path);created++;
        }
        UnityEngine.Object.DestroyImmediate(g);
    }
}
