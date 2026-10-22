using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using DeadAir.Playtesting;

public static class PlaytestToolsSetup
{
    public const string Root="Assets/DeadAir/DeveloperTools";
    [MenuItem("Tools/Dead Air/Install or Select Developer Settings")]
    public static void Install()
    {
        Directory.CreateDirectory(Root+"/Resources");AssetDatabase.Refresh();
        string path=Root+"/Resources/DeadAirPlaytestSettings.asset";
        var settings=AssetDatabase.LoadAssetAtPath<PlaytestSettings>(path);
        if(!settings){settings=ScriptableObject.CreateInstance<PlaytestSettings>();AssetDatabase.CreateAsset(settings,path);}
        AssetDatabase.SaveAssets();Selection.activeObject=settings;
        File.WriteAllText(Application.dataPath+"/../Logs/DeadAirInstall.txt","Settings installed. F8 developer menu; automatic gameplay recording; public release builds disabled. Existing scenes unchanged.");
    }
    [MenuItem("Tools/Dead Air/Open Recording Folder")]
    public static void OpenFolder(){Directory.CreateDirectory(PlaytestStorage.Root);EditorUtility.RevealInFinder(PlaytestStorage.Root);}
}
