using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AreaControlsSmokeRunner
{
    static AreaControlsSmokeRunner() { EditorApplication.playModeStateChanged+=Restore; }
    static void Restore(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("AreaControls.Test",false))return;
        string previous=SessionState.GetString("AreaControls.Previous","");
        EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(previous)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        SessionState.SetBool("AreaControls.Test",false);
    }
    [MenuItem("Tools/Blockout/Test Area Controls In Isolation")]
    static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var current=SceneManager.GetActiveScene();
        // The main scene has already been saved and independently verified.
        // Do not prompt/save any new user edits as a side effect of this test.
        if(current.isDirty){File.WriteAllText(Application.dataPath+"/../Logs/AreaControlsTest.txt","SKIPPED: working scene has new unsaved changes.");return;}
        var test=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try
        {
            var root=new GameObject("Isolated area-control tests"); SceneManager.MoveGameObjectToScene(root,test);
            root.AddComponent<AreaControlsSmoke>().TriggerPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blockout/Optimization/Prefabs/Area Activation Trigger.prefab");
            EditorSceneManager.SaveScene(test,"Assets/Blockout/Optimization/Tests/AreaControlsSmoke.unity");
        }
        finally { SceneManager.SetActiveScene(current); EditorSceneManager.CloseScene(test,true); }
        SessionState.SetString("AreaControls.Previous",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetBool("AreaControls.Test",true);
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Blockout/Optimization/Tests/AreaControlsSmoke.unity");
        EditorApplication.isPlaying=true;
    }
}
