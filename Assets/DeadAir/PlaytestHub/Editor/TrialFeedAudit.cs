using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadAir.Playtesting;
using Blockout.Discovery;

public static class TrialFeedAudit
{
    [MenuItem("Tools/Dead Air/Security/Wire Trial Desktop")]
    public static void Wire()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.path!="Assets/DeadAir/PlaytestHub/Scenes/Security Terminal Trial.unity")throw new Exception("Open Security Terminal Trial in edit mode.");
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var mounts=all.Where(t=>t.name.StartsWith("SecurityCamera",StringComparison.OrdinalIgnoreCase)&&t.Find("Camera")).OrderBy(t=>t.name).ToArray();
        var doors=all.Select(t=>t.GetComponent<PrototypeMover>()).Where(m=>m&&m.MovingPart&&(m.name.Contains("Door",StringComparison.OrdinalIgnoreCase)||m.name.StartsWith("BOTH OF THESE"))).ToArray();
        var host=all.Select(t=>t.GetComponent<SecurityTrialTerminal>()).Single(t=>t);
        var archive=AssetDatabase.LoadAssetAtPath<VirgoArchive>("Assets/Blockout/Discovery/Data/Virgo Archive.asset");
        if(mounts.Length==0||!archive)throw new Exception("Missing camera mounts or archive.");
        Physics.SyncTransforms();
        var matches=new List<(Transform mount,PrototypeMover door,Vector3 aim)>();
        foreach(var mount in mounts){
            var candidates=new List<(float cost,PrototypeMover mover,Vector3 aim)>();
            foreach(var m in doors){
                var meshes=m.MovingPart.GetComponentsInChildren<Renderer>();if(meshes.Length==0)continue;
                var bounds=meshes[0].bounds;foreach(var r in meshes)bounds.Encapsulate(r.bounds);
                // A jamb or wall can obscure the center while the upper/side of the leaf is visible.
                foreach(float h in new[]{0f,.7f,-.65f})foreach(float side in new[]{0f,-.7f,.7f}){
                    Vector3 aim=bounds.center+Vector3.up*(bounds.extents.y*h)+(bounds.extents.x>bounds.extents.z?Vector3.right*bounds.extents.x:Vector3.forward*bounds.extents.z)*side;
                    var dir=aim-mount.position;float dist=dir.magnitude;if(dist<.2f||dist>45)continue;
                    var hits=Physics.RaycastAll(mount.position+dir.normalized*.2f,dir.normalized,dist-.2f,~0,QueryTriggerInteraction.Ignore);
                    if(hits.Any(hit=>!hit.transform.IsChildOf(mount)&&!hit.transform.IsChildOf(m.transform)))continue;
                    candidates.Add((dist*(1+.25f*(1-Vector3.Dot(dir.normalized,mount.forward)))+.6f*(Mathf.Abs(h)+Mathf.Abs(side)),m,aim));
                }
            }
            if(candidates.Count==0){matches.Add((mount,null,mount.position+mount.forward*5));continue;}
            var best=candidates.OrderBy(x=>x.cost).First();matches.Add((mount,best.mover,best.aim));
        }
        string backup=Application.dataPath+"/../Logs/TrialBeforeDesktop-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";EditorSceneManager.SaveScene(scene,backup,true);
        var desktop=host.GetComponent<VirgoTerminal>();if(!desktop)desktop=Undo.AddComponent<VirgoTerminal>(host.gameObject);
        Undo.RecordObject(desktop,"Configure trial desktop");desktop.TrialDesktopExtras=true;desktop.Archive=archive;
        if(!desktop.DesktopWallpaper)desktop.DesktopWallpaper=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/hamterbackground.png");
        Undo.RecordObject(host,"Wire placed camera feeds");host.CameraFeeds=new Camera[matches.Count];host.FeedDoors=new SecurityPanelDoor[matches.Count];var report=new List<string>();
        for(int i=0;i<matches.Count;i++){
            var x=matches[i];string label="CAM "+(i+1).ToString("00");var existing=x.mount.Find(label);
            var lens=existing?existing.gameObject:new GameObject(label);if(!existing){Undo.RegisterCreatedObjectUndo(lens,"Add live lens");lens.transform.SetParent(x.mount,false);}
            Vector3 dir=(x.aim-x.mount.position).normalized;
            lens.transform.position=x.mount.position+dir*.20f;lens.transform.rotation=Quaternion.LookRotation(dir);var cam=lens.GetComponent<Camera>();if(!cam)cam=Undo.AddComponent<Camera>(lens);
            cam.enabled=false;cam.nearClipPlane=.08f;cam.fieldOfView=75;cam.cullingMask=~((1<<5)|(1<<7)|(1<<13));host.CameraFeeds[i]=cam;
            if(x.door){var guard=x.door.GetComponent<SecurityPanelDoor>();if(!guard)guard=Undo.AddComponent<SecurityPanelDoor>(x.door.gameObject);guard.Mover=x.door;guard.DoorLabel=x.door.name;host.FeedDoors[i]=guard;EditorUtility.SetDirty(guard);}
            report.Add(label+" on "+x.mount.name+" -> "+(x.door?x.door.name:"observation only; no clear door sightline"));
        }
        foreach(var c in new Component[]{desktop,host}){EditorUtility.SetDirty(c);PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
        EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed.");
        File.WriteAllLines(Application.dataPath+"/../Logs/TrialDesktopWiring.txt",report);
    }
}


