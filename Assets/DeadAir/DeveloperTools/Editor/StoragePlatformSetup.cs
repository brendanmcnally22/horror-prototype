using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Blockout.Discovery;
public static class StoragePlatformSetup
{
    [MenuItem("Tools/Dead Air/Enable Storage Platform Return")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!=DiscoverySetup.ScenePath)throw new Exception("Open Discovery Blank.");
        var mover=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrototypeMover>(true)).Single(m=>m.name=="STORAGE PLATFORM");
        var rider=mover.GetComponentInChildren<PrototypeRider>(true);var zone=rider?rider.GetComponent<BoxCollider>():null;
        if(!zone)throw new Exception("Storage rider zone missing; no changes made.");
        EditorSceneManager.SaveScene(scene,Application.dataPath+"/../Logs/DiscoveryBeforeStorageReturn.unity",true);
        var component=mover.GetComponent<PlatformReturnAfterExit>();if(!component)component=Undo.AddComponent<PlatformReturnAfterExit>(mover.gameObject);
        Undo.RecordObject(component,"Storage return settings");component.Mover=mover;component.RiderZone=zone;component.ReturnDelay=2;
        EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Application.dataPath+"/../Logs/StorageReturnInstalled.txt","Storage platform waits until empty at the far stop, then returns after 2 seconds. Existing transforms unchanged.");
    }
}
