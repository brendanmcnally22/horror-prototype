using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Blockout.Discovery;

public static partial class FacilityDiscoverySetup
{
    const string AssetsRoot="Assets/Blockout/Discovery";
    static Material Material(string name,Color color,bool emission=false)
    {
        string path=AssetsRoot+"/Materials/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material)return material;
        material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.35f);
        if(emission){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*1.5f);material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;}
        AssetDatabase.CreateAsset(material,path);return material;
    }
    static GameObject Part(Transform parent,string name,Vector3 size,Vector3 position,Material material,bool collision)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.layer=11;
        go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;
        var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        if(!collision)Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    static void CreateAssets()
    {
        var housing=Material("Facility Graphite",new Color(.08f,.1f,.12f));
        var trim=Material("Facility Window Trim",new Color(.27f,.31f,.34f));
        var colors=new[]{new Color(1,.06f,.035f),new Color(1,.65f,.05f),new Color(.12f,1,.3f),new Color(.88f,.94f,1)};
        var names=new[]{"Red","Yellow","Green","White"};
        for(int i=0;i<4;i++)
        {
            string name="Facility Light - "+names[i]+" Steady";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(AssetsRoot+"/Prefabs/"+name+".prefab"))continue;
            var root=new GameObject(name);
            Part(root.transform,"Housing",new Vector3(2,.14f,.35f),new Vector3(0,-.07f,0),housing,false);
            var diffuser=Part(root.transform,"Diffuser",new Vector3(1.8f,.025f,.22f),new Vector3(0,-.15f,0),Material("Facility Diffuser "+names[i],colors[i],true),false);
            var source=new GameObject("Downlight - No Shadows").AddComponent<Light>();source.transform.SetParent(root.transform,false);
            source.transform.localPosition=new Vector3(0,-.19f,0);source.transform.localRotation=Quaternion.Euler(90,0,0);
            source.type=LightType.Spot;source.spotAngle=115;source.innerSpotAngle=75;source.color=colors[i];source.intensity=2.5f;source.range=7;source.shadows=LightShadows.None;
            var lamp=root.AddComponent<FacilityLamp>();lamp.Color=colors[i];lamp.Intensity=2.5f;lamp.Range=7;lamp.EmergencySupply=i!=3;
            lamp.Sources=new[]{source};lamp.Diffusers=new[]{diffuser.GetComponent<Renderer>()};
            PrefabUtility.SaveAsPrefabAsset(root,AssetsRoot+"/Prefabs/"+name+".prefab");Object.DestroyImmediate(root);
        }
        var glass=Material("Facility Window Glass",new Color(.25f,.42f,.46f,.23f));
        glass.SetFloat("_Surface",1);glass.SetFloat("_Blend",0);glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        glass.SetFloat("_ZWrite",0);glass.SetFloat("_Smoothness",.75f);glass.SetFloat("_Cull",0);
        glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.SetOverrideTag("RenderType","Transparent");glass.renderQueue=3000;glass.SetShaderPassEnabled("ShadowCaster",false);
        EditorUtility.SetDirty(glass);
        foreach(float width in new[]{4f,8f,12f})
        {
            string name="Facility Window - "+width+"m Observation";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(AssetsRoot+"/Prefabs/"+name+".prefab"))continue;
            var root=new GameObject(name);
            Part(root.transform,"Glass - collision",new Vector3(width-.24f,1.36f,.04f),new Vector3(0,.8f,0),glass,true);
            Part(root.transform,"Sill",new Vector3(width,.12f,.3f),new Vector3(0,.06f,0),housing,true);
            Part(root.transform,"Header",new Vector3(width,.12f,.3f),new Vector3(0,1.54f,0),housing,true);
            foreach(float side in new[]{-1f,1f})Part(root.transform,"Side frame",new Vector3(.12f,1.36f,.3f),new Vector3(side*(width/2-.06f),.8f,0),housing,true);
            for(float x=-width/2+2;x<width/2-.1f;x+=2)Part(root.transform,"Reinforced mullion",new Vector3(.07f,1.36f,.18f),new Vector3(x,.8f,0),trim,true);
            PrefabUtility.SaveAsPrefabAsset(root,AssetsRoot+"/Prefabs/"+name+".prefab");Object.DestroyImmediate(root);
        }
    }
}
