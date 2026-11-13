using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static partial class PlaytestHubSetup
{
    static void CaptureVisuals()
    {
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try
        {
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.4f,.4f,.4f);
            var sun=new GameObject("Preview light").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.6f;sun.transform.rotation=Quaternion.Euler(35,-35,0);
            var camera=new GameObject("Preview camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.09f,.1f);camera.fieldOfView=42;camera.nearClipPlane=.01f;
            foreach(string path in new[]{"Assets/Blockout/Discovery/Prefabs/Virgo Technology/Virgo Computer - Staff.prefab",Root+"/Prefabs/Lever_7 - Interactive.prefab"})
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),scene);go.transform.position=Vector3.zero;
                foreach(var transform in go.GetComponentsInChildren<Transform>(true))transform.gameObject.layer=30;camera.cullingMask=1<<30;
                var b=LocalBounds(go);float distance=Mathf.Max(b.size.x,b.size.y,b.size.z)*2.4f;
                camera.transform.position=b.center+new Vector3(.3f,.25f,-1).normalized*distance;camera.transform.LookAt(b.center);
                foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>())
                foreach(var material in renderer.sharedMaterials)
                {
                    if(!material||material.shader.name!="Dead Air/CRT Screen")continue;
                    var u=material.GetVector("_ScreenU");var v=material.GetVector("_ScreenV");
                    var right=new Vector3(u.x,u.y,u.z).normalized;var up=new Vector3(v.x,v.y,v.z).normalized;
                    var normal=renderer.transform.TransformDirection(Vector3.Cross(up,right)).normalized;
                    var focus=renderer.bounds.center;
                    camera.transform.position=focus+normal*distance+renderer.transform.TransformDirection(up)*distance*.15f;
                    camera.transform.LookAt(focus,renderer.transform.TransformDirection(up));
                }
                var target=RenderTexture.GetTemporary(720,540,24);var old=RenderTexture.active;
                try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;var picture=new Texture2D(720,540,TextureFormat.RGB24,false);picture.ReadPixels(new Rect(0,0,720,540),0,0);picture.Apply();File.WriteAllBytes("Logs/"+(path.Contains("Computer")?"PlaytestHubCRT":"PlaytestHubLever")+".png",picture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(picture);}
                finally{camera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(go);}
            }
        }
        finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
    }
}
