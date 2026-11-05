using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using UnityEditor.SceneManagement;
using Blockout.Discovery;
using UHFPS.Runtime;

[InitializeOnLoad]
public static class HeavyLeverPrototypeCheck
{
    const string Key="HeavyLeverPrototypeCheck";
    static double start,step;
    static int phase;
    static HeavyTwoHandLever lever;
    static HeavyLeverHearingReceiver receiver;
    static PlayerManager player;
    static readonly List<string> report=new List<string>(),errors=new List<string>();
    static HeavyLeverPrototypeCheck()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>
        {
            if(!SessionState.GetBool(Key,false))return;
            if(s==PlayModeStateChange.EnteredPlayMode){start=EditorApplication.timeSinceStartup;phase=0;report.Clear();errors.Clear();}
            if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);string path=SessionState.GetString(Key+".previous","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(path)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(path);}
        };
        Application.logMessageReceived+=(m,t,type)=>{if(SessionState.GetBool(Key,false)&&(type==LogType.Error||type==LogType.Exception)&&!m.StartsWith("Layout update failed"))errors.Add(m);};
    }
    [MenuItem("Tools/Dead Air/Test Heavy Lever Prototype")]
    static void Begin()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save current scene edits first; nothing discarded.");
        SessionState.SetString(Key+".previous",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(HeavyLeverPrototypeSetup.ScenePath);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static void Hold(bool value)=>InputSystem.QueueStateEvent(Keyboard.current,value?new KeyboardState(UnityEngine.InputSystem.Key.E):new KeyboardState());
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||start==0||phase==99)return;
        try
        {
            double now=EditorApplication.timeSinceStartup;if(now-start>50)throw new Exception("Prototype test timed out at phase "+phase);
            if(phase==0)
            {
                if(now-start<6)return;
                lever=UnityEngine.Object.FindAnyObjectByType<HeavyTwoHandLever>();receiver=UnityEngine.Object.FindAnyObjectByType<HeavyLeverHearingReceiver>();player=PlayerManager.Instance;
                Require(lever&&receiver&&player,"Prototype references missing.");
                player.PlayerCollider.enabled=false;player.transform.position=lever.transform.position+new Vector3(0,.1f,-1.4f);player.PlayerCollider.enabled=true;Physics.SyncTransforms();
                Hold(true);Require(lever.BeginPull(player),"Could not begin pull.");step=now;phase=1;return;
            }
            if(phase==1)
            {
                if(now-step<1)return;
                Require(lever.Pulling&&lever.Progress>0&&lever.Progress<.5f,"Slow hold progression failed.");
                Require(!player.PlayerStateMachine.enabled&&!player.PlayerItems.IsItemsUsable&&lever.PrototypeHands.activeSelf,"Two-hand commitment not active.");
                Require(lever.MechanismAudio.isPlaying&&receiver.HeardCount>0,"Audio/noise receiver failed.");
                report.Add("PASS: held interaction progresses over time, shows both prototype hands, prevents walking/item use, plays positional audio and alerts hearing receiver.");
                Hold(false);step=now;phase=2;return;
            }
            if(phase==2)
            {
                if(now-step<2.5)return;
                Require(!lever.Pulling&&!lever.Completed&&lever.Progress<.001f,"Release failed to cancel/return.");
                Require(player.PlayerStateMachine.enabled&&player.PlayerItems.IsItemsUsable&&!lever.PrototypeHands.activeSelf,"Player controls not restored.");
                report.Add("PASS: release cancels, handle returns, and player controls recover.");
                Hold(true);Require(lever.BeginPull(player),"Second pull failed.");step=now;phase=3;return;
            }
            if(phase==3)
            {
                if(!lever.Completed)return;Hold(false);
                Require(now-step>=4.8,"Lever completed too early.");
                var shutter=UnityEngine.Object.FindObjectsByType<PrototypeMover>(FindObjectsSortMode.None).Single(m=>m.name=="TEST SHUTTER");
                Require(shutter.TargetEnd&&!lever.Pulling&&player.PlayerStateMachine.enabled,"Completion/shutter/release failed.");
                report.Add("PASS: full five-second pull latches once and opens test shutter.");
                lever.ResetLever();Require(!lever.Completed&&lever.Progress==0&&!shutter.TargetEnd,"Reset failed.");
                report.Add("PASS: lever and shutter reset for repeated tests.");
                Require(errors.Count==0,string.Join("\n",errors));Finish("PASS");
            }
        }
        catch(Exception e){report.Add(e.ToString());Finish("FAIL");}
    }
    static void Finish(string status){phase=99;Hold(false);File.WriteAllText(Application.dataPath+"/../Logs/HeavyLeverPrototypeCheck.txt",status+"\n"+string.Join("\n",report));EditorApplication.isPlaying=false;}
}
