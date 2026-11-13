using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Blockout.Discovery;
using DeadAir.Playtesting;

public static partial class PlaytestHubSetup
{
    static Bounds LocalBounds(GameObject root)
    {
        bool first=true;var bounds=new Bounds();
        foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var b=renderer.bounds;
            for(int i=0;i<8;i++)
            {
                var corner=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var point=root.transform.InverseTransformPoint(corner);if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
            }
        }
        return bounds;
    }
    static GameObject Model(string path,Transform parent)
    {
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);
        PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        foreach(var light in root.GetComponentsInChildren<Light>(true))UnityEngine.Object.DestroyImmediate(light.gameObject);
        foreach(var camera in root.GetComponentsInChildren<Camera>(true))UnityEngine.Object.DestroyImmediate(camera.gameObject);
        foreach(var collider in root.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
        return root;
    }
    static void AssignWallpaper(GameObject root)
    {
        var overlay=root.transform.Find("Desktop - Hamster Wallpaper");if(overlay)UnityEngine.Object.DestroyImmediate(overlay.gameObject);
        int assigned=0;
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++)
            {
                var original=materials[i];if(!original)continue;
                Color color=original.HasProperty("_BaseColor")?original.GetColor("_BaseColor"):original.HasProperty("_Color")?original.GetColor("_Color"):Color.white;
                bool screen=(color.b>.35f&&color.b>color.r*1.8f&&color.b>color.g*1.8f)||original.shader.name=="Dead Air/CRT Screen";
                if(!screen)continue;
                var shader=Shader.Find("Dead Air/CRT Screen");if(!shader)throw new Exception("CRT screen shader has not imported yet.");
                string path=Root+"/Materials/CRT Screen - Hamster.mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
                material.shader=shader;material.SetTexture("_BaseMap",wallpaper);
                ProjectScreen(root.transform,renderer,Mathf.Min(i,renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount-1),material);
                EditorUtility.SetDirty(material);materials[i]=material;assigned++;
            }
            renderer.sharedMaterials=materials;
        }
        if(assigned==0)throw new Exception("Could not identify the CRT model's actual blue screen surface.");
    }
    static void ProjectScreen(Transform root,MeshRenderer renderer,int submesh,Material material)
    {
        var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
        var acquire=typeof(MeshUtility).GetMethods(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static).FirstOrDefault(m=>m.Name=="AcquireReadOnlyMeshData"&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType==typeof(Mesh));
        if(acquire==null)throw new Exception("Editor mesh read API is unavailable; cannot fit desktop to CRT screen.");
        using(var data=(Mesh.MeshDataArray)acquire.Invoke(null,new object[]{mesh}))
        using(var vertices=new Unity.Collections.NativeArray<Vector3>(mesh.vertexCount,Unity.Collections.Allocator.Temp))
        {
            var source=data[0];source.GetVertices(vertices);var part=source.GetSubMesh(submesh);
            int Index(int i)=>(source.indexFormat==UnityEngine.Rendering.IndexFormat.UInt32?(int)source.GetIndexData<uint>()[part.indexStart+i]:source.GetIndexData<ushort>()[part.indexStart+i])+part.baseVertex;
            Vector3 normal=Vector3.zero;
            for(int i=0;i+2<part.indexCount;i+=3)normal+=Vector3.Cross(vertices[Index(i+1)]-vertices[Index(i)],vertices[Index(i+2)]-vertices[Index(i)]);
            var face=root.InverseTransformDirection(renderer.transform.TransformDirection(normal.normalized));
            face=Mathf.Abs(face.x)>Mathf.Abs(face.z)?new Vector3(Mathf.Sign(face.x),0,0):new Vector3(0,0,Mathf.Sign(face.z));
            var right=renderer.transform.InverseTransformDirection(root.TransformDirection(Vector3.Cross(face,Vector3.up))).normalized;
            var up=renderer.transform.InverseTransformDirection(root.TransformDirection(Vector3.up)).normalized;
            float minU=float.PositiveInfinity,maxU=float.NegativeInfinity,minV=float.PositiveInfinity,maxV=float.NegativeInfinity;
            for(int i=0;i<part.indexCount;i++){var p=vertices[Index(i)];float u=Vector3.Dot(p,right),v=Vector3.Dot(p,up);minU=Mathf.Min(minU,u);maxU=Mathf.Max(maxU,u);minV=Mathf.Min(minV,v);maxV=Mathf.Max(maxV,v);}
            float width=Mathf.Max(.0001f,maxU-minU),height=Mathf.Max(.0001f,maxV-minV);
            material.SetVector("_ScreenU",new Vector4(right.x/width,right.y/width,right.z/width,-minU/width));
            material.SetVector("_ScreenV",new Vector4(up.x/height,up.y/height,up.z/height,-minV/height));
        }
    }
    static void UpgradeTechnologyPrefabs()
    {
        string folder="Assets/Blockout/Discovery/Prefabs/Virgo Technology";
        foreach(string path in AssetDatabase.FindAssets("t:Prefab",new[]{folder}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var terminal=root.GetComponent<VirgoTerminal>();if(!terminal||terminal.PhoneOnly)continue;
                terminal.DesktopWallpaper=wallpaper;
                if(!root.transform.Find("CRT Monitor - Authored Model"))
                {
                    foreach(string name in new[]{"Monitor stand","Deep CRT shell","Screen recess","Phosphor screen"})
                    {var part=root.transform.Find(name);if(part&&part.GetComponent<Renderer>())part.GetComponent<Renderer>().enabled=false;}
                    var monitor=Model("Assets/CRTMonitor.fbx",root.transform);monitor.name="CRT Monitor - Authored Model";
                    var bounds=LocalBounds(monitor);float size=.57f/Mathf.Max(.01f,bounds.size.x);
                    monitor.transform.localScale=Vector3.one*size;monitor.transform.localPosition=new Vector3(0,.5f,.14f)-bounds.center*size;
                    AssignWallpaper(monitor);
                }
                else AssignWallpaper(root.transform.Find("CRT Monitor - Authored Model").gameObject);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        MakeLeverPrefab();
    }
    static int UpgradeCRTInstances(Scene scene)
    {
        int count=0;
        foreach(var terminal in All(scene).Select(t=>t.GetComponent<VirgoTerminal>()).Where(t=>t&&!t.PhoneOnly))terminal.DesktopWallpaper=wallpaper;
        foreach(var transform in All(scene).Where(t=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)=="Assets/CRTMonitor.fbx"&&PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)).ToArray())
        {
            if(transform.GetComponent<SecurityTrialTerminal>()){AssignWallpaper(transform.gameObject);count++;continue;}
            var terminal=transform.GetComponent<VirgoTerminal>();if(!terminal)terminal=transform.gameObject.AddComponent<VirgoTerminal>();
            terminal.Archive=AssetDatabase.LoadAssetAtPath<VirgoArchive>("Assets/Blockout/Discovery/Data/Virgo Archive.asset");terminal.Clearance=1;terminal.StationName="VIRGO-CRT";terminal.DesktopWallpaper=wallpaper;
            var box=transform.GetComponent<BoxCollider>();if(!box)box=transform.gameObject.AddComponent<BoxCollider>();
            var bounds=LocalBounds(transform.gameObject);box.center=bounds.center;box.size=bounds.size;box.isTrigger=true;transform.gameObject.layer=6;
            AssignWallpaper(transform.gameObject);count++;
        }
        return count;
    }
    static void ConfigureLeverModel(GameObject model,PrototypeLever lever,bool authored=false)
    {
        var handle=model.transform.Find("Lever");if(!handle)throw new Exception("Authored lever model is missing its Lever mesh.");
        lever.Handle=handle;lever.OffAngle=new Vector3(0,authored?180:0,-25);lever.OnAngle=new Vector3(0,authored?180:0,25);lever.JamRecoilAxis=Vector3.forward;
        var indicators=model.GetComponent<LeverStatusLights>();if(!indicators)indicators=model.AddComponent<LeverStatusLights>();indicators.Lever=lever;
        var upper=model.transform.Find("Light Upper");var lower=model.transform.Find("Light Lower");
        indicators.Upper=upper?upper.GetComponent<Renderer>():null;indicators.Lower=lower?lower.GetComponent<Renderer>():null;
        if(indicators.Upper)indicators.Upper.sharedMaterial=lens;if(indicators.Lower)indicators.Lower.sharedMaterial=lens;
    }
    static void MakeLeverPrefab()
    {
        string path=Root+"/Prefabs/Lever_7 - Interactive.prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(path))return;
        var go=new GameObject("Lever_7 - Interactive");
        try
        {
            var lever=go.AddComponent<PrototypeLever>();go.layer=6;
            var model=Model("Assets/Lever.fbx",go.transform);model.name="Lever_7 visual";model.transform.localRotation=Quaternion.Euler(0,-90,0);model.transform.localScale=Vector3.one*.18134f;
            ConfigureLeverModel(model,lever);
            var bounds=LocalBounds(go);var collider=go.AddComponent<BoxCollider>();collider.center=bounds.center;collider.size=bounds.size+Vector3.one*.06f;collider.isTrigger=true;
            PrefabUtility.SaveAsPrefabAsset(go,path);
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    static int UpgradeLevers(Scene scene)
    {
        var levers=All(scene).Select(t=>t.GetComponent<PrototypeLever>()).Where(l=>l).ToArray();int count=0;
        foreach(var lever in levers)
        {
            if(lever.transform.Find("Base")&&lever.transform.Find("Lever"))
            {
                if(lever.Targets.Length==0)
                {
                    var source=levers.Where(l=>l!=lever&&l.Targets.Any(t=>t)&&!l.Jammed).OrderBy(l=>Vector3.Distance(l.Handle?l.Handle.position:l.transform.position,lever.transform.position)).FirstOrDefault();
                    if(source&&Vector3.Distance(source.Handle?source.Handle.position:source.transform.position,lever.transform.position)<2)
                    {lever.Targets=source.Targets.ToArray();lever.Label=source.Label;lever.FollowTarget=source.FollowTarget;}
                }
                ConfigureLeverModel(lever.gameObject,lever,true);
                if(!lever.GetComponent<Collider>()){var b=LocalBounds(lever.gameObject);var c=lever.gameObject.AddComponent<BoxCollider>();c.center=b.center;c.size=b.size+Vector3.one*.1f;c.isTrigger=true;}
                lever.gameObject.layer=6;count++;continue;
            }
            if(lever.transform.Find("Lever_7 visual")){ConfigureLeverModel(lever.transform.Find("Lever_7 visual").gameObject,lever);count++;continue;}
            var old=lever.GetComponentsInChildren<Renderer>(true).ToArray();
            Vector3 position=lever.Handle?lever.transform.InverseTransformPoint(lever.Handle.position):Vector3.up;
            var visual=Model("Assets/Lever.fbx",lever.transform);visual.name="Lever_7 visual";visual.transform.localPosition=position;visual.transform.localRotation=Quaternion.Euler(0,-90,0);
            visual.transform.localScale=Vector3.one*(.18134f/Mathf.Max(.01f,Mathf.Abs(lever.transform.lossyScale.y)));
            foreach(var renderer in old)renderer.enabled=false;
            ConfigureLeverModel(visual,lever);count++;
        }
        return count;
    }
}
