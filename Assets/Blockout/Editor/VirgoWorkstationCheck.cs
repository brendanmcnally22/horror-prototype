using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Blockout.Discovery;
using UHFPS.Runtime;

[InitializeOnLoad]
public static class VirgoWorkstationCheck
{
    const string Key="VirgoCheck.Running";
    static double started,step;
    static int phase;
    static VirgoTerminal terminal;
    static FacilityPowerSequence power;
    static FacilityLamp white;
    static string errors="";
    static VirgoWorkstationCheck()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>{
            if(!SessionState.GetBool(Key,false))return;
            if(s==PlayModeStateChange.EnteredPlayMode){started=EditorApplication.timeSinceStartup;phase=0;errors="";Application.runInBackground=true;}
            if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);string previous=SessionState.GetString(Key+".Previous","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(previous)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);}
        };
        Application.logMessageReceived+=(message,trace,type)=>{if(SessionState.GetBool(Key,false)&&(type==LogType.Error||type==LogType.Exception)&&!message.StartsWith("Layout update failed"))errors+=message+"\n";};
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false))
        {
            if(!File.Exists("Logs/VirgoCheck.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete("Logs/VirgoCheck.request");SessionState.SetString(Key+".Previous",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));EditorSceneManager.playModeStartScene=null;
            SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;return;
        }
        if(!EditorApplication.isPlaying||started==0||phase==9)return;
        try
        {
            if(EditorApplication.timeSinceStartup-started>180)throw new Exception("Workstation check timeout.");
            if(phase==0)
            {
                if(EditorApplication.timeSinceStartup-started<8)return;
                power=UnityEngine.Object.FindAnyObjectByType<FacilityPowerSequence>();
                Require(power&&power.PowerCut&&!power.PowerRestored,"Emergency power did not start on arrival.");
                Require(power.ObjectiveDisplay.Text.gameObject.activeSelf&&power.ObjectiveDisplay.Text.text.Contains("RESTORE FACILITY POWER"),"First objective missing.");
                Require(power.ObjectivePanel,"Panel marker target missing.");
                var light=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blockout/Discovery/Prefabs/Facility Light - White Steady.prefab"));white=light.GetComponent<FacilityLamp>();
                var computer=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blockout/Discovery/Prefabs/Virgo Technology/Virgo Computer - Reception.prefab"));terminal=computer.GetComponent<VirgoTerminal>();
                computer.transform.position=new Vector3(1000,0,1000);light.transform.position=new Vector3(1000,4,1000);
                Require(terminal.Archive.Records.Length>=8,"Archive missing.");Require(!terminal.CanRead(terminal.Archive.Records.Last()),"Director records exposed at reception.");
                Require(terminal.Dial("204").Contains("three"),"Phone directory failed.");Require(terminal.Dial("999").Contains("NO CONNECTION"),"Unknown extension failed.");
                foreach(int i in new[]{0,3,5,10,12,15})terminal.ToggleCell(i);Require(terminal.CircuitSolved,"Circuit puzzle not solvable.");terminal.ResetCircuit();Require(!terminal.CircuitSolved,"Puzzle reset failed.");
                terminal.Open();terminal.OpenApplication(1);terminal.Frequency=terminal.Archive.HiddenFrequency;
                Require(terminal.IsOpen&&Cursor.visible&&Cursor.lockState==CursorLockMode.None,"Terminal cursor capture failed.");
                Require(!terminal.TryDecode(),"Signal decoded without lock dwell.");step=EditorApplication.timeSinceStartup;phase=1;return;
            }
            if(phase==1)
            {
                if(EditorApplication.timeSinceStartup-step<3)return;
                Require(white.Sources.All(l=>l.enabled&&l.color.r>.9f&&l.color.b<.2f),"White fixture did not turn amber.");
                Require(terminal.TryDecode(),"Frequency lock/decode failed.");
                var loop=power.ObjectiveDisplay.Loop;loop.RecordPackFuse(100);Require(loop.Returned==1&&!power.PowerRestored,"Power restored before third fuse.");
                loop.RecordPackFuse(101);loop.RecordPackFuse(102);Require(power.PowerRestored,"Third fuse failed to restore lighting.");
                Require(white.Sources.All(l=>l.enabled&&l.color.b>.65f),"Original white lighting not restored.");
                terminal.Close();Require(!VirgoTerminal.Active,"Terminal did not release control.");
                terminal.Open();terminal.OpenApplication(0);
                File.WriteAllText("Logs/VirgoCheck.txt","CHECKS PASSED; preview open for 90 seconds. Arrival objective, panel marker, amber lighting, third fuse restoration, clearance filtering, telephone, circuit solve/reset, frequency lock/decode, cursor release.\n");
                step=EditorApplication.timeSinceStartup;phase=2;return;
            }
            if(phase==2&&(EditorApplication.timeSinceStartup-step>90||File.Exists("Logs/VirgoCheckFinish.request")))
            {
                if(File.Exists("Logs/VirgoCheckFinish.request"))File.Delete("Logs/VirgoCheckFinish.request");
                terminal.Close();Require(string.IsNullOrEmpty(errors),errors);
                Finish("PASS: arrival objective + panel marker; white lights remain enabled amber; third fuse restores original white; clearance filtering; phone directory + unknown extension; solvable circuit game + reset; frequency lock + decode; terminal opens and releases cursor/player control. No captured gameplay errors.\n");
            }
        }
        catch(Exception e){Finish("FAIL: "+e+"\n"+errors);}
    }
    static void Finish(string result){phase=9;if(terminal)terminal.Close();File.WriteAllText("Logs/VirgoCheck.txt",result);EditorApplication.isPlaying=false;}
}
