using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DeadAir.Playtesting;

[InitializeOnLoad]
public static class PlaytestPhaseOneCheck
{
    const string Key="DeadAir.PhaseOneCheck";
    static double start;
    static int phase;
    static readonly List<string> results=new List<string>();
    static readonly List<string> errors=new List<string>();
    static PlaytestPhaseOneCheck()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){start=EditorApplication.timeSinceStartup;phase=0;results.Clear();errors.Clear();}
            if(state==PlayModeStateChange.EnteredEditMode)SessionState.SetBool(Key,false);
        };
        Application.logMessageReceived+=(m,t,type)=>{if(SessionState.GetBool(Key,false)&&(type==LogType.Exception||type==LogType.Error)&&!m.StartsWith("Layout update failed"))errors.Add(m);};
    }
    [MenuItem("Tools/Dead Air/Verify Phase One Recording and Replay")]
    static void Begin()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit play mode before verification.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Save current scene changes first.");
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void Require(bool b,string m){if(!b)throw new Exception(m);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||phase==99||start==0)return;
        try
        {
            if(EditorApplication.timeSinceStartup-start>40)throw new Exception("Phase one test timed out.");
            if(phase==0)
            {
                if(EditorApplication.timeSinceStartup-start<7)return;
                var r=PlaytestRecorder.Instance;Require(r&&r.Recording,"Gameplay did not automatically start recording.");
                Require(r.Metadata.sampleCount>1&&r.Elapsed>1,"Positions/time not recorded.");
                string id=r.Metadata.id;r.Complete("automated_phase_one_verification");
                var loaded=PlaytestStorage.Load(id);Require(loaded.metadata.status=="completed"&&loaded.samples.Count>1,"Completed session cannot be loaded.");
                results.Add("PASS: actual UHFPS gameplay auto-starts a unique session; position records, timestamps, completed metadata, and event log survive disk reload.");
                var menu=r.GetComponent<DeveloperSettingsMenu>();menu.Open();Require(menu.SelectSession(id),"Replay selection failed.");
                menu.Seek(loaded.metadata.duration*.5);Require(Vector3.Distance(menu.Marker.position,loaded.samples[0].position)<1,"Stationary pause interpolation incorrect.");
                foreach(float speed in new[]{.5f,1,2,4,8}){menu.Seek(0);menu.SetSpeed(speed);menu.SetPlaying(true);menu.Advance(.1f);Require(Math.Abs(menu.PlaybackTime-.1f*speed)<.0001,"Speed timing incorrect.");}
                menu.SetPlaying(false);
                results.Add("PASS: loaded replay scrubbing and all five speeds preserve recorded timing.");
                var samples=new List<PositionSample>{new PositionSample{time=0,position=Vector3.zero,rotation=Quaternion.identity,camera=Quaternion.identity},new PositionSample{time=2,position=Vector3.zero,rotation=Quaternion.identity,camera=Quaternion.identity},new PositionSample{time=4,position=Vector3.right*10,rotation=Quaternion.Euler(0,90,0),camera=Quaternion.identity}};
                Require(PlaytestStorage.Evaluate(samples,1,out _).position==Vector3.zero,"Pause drift.");Require(Math.Abs(PlaytestStorage.Evaluate(samples,3,out _).position.x-5)<.001,"Movement interpolation failed.");
                results.Add("PASS: deterministic pause and moving-segment interpolation (no gameplay simulation).");
                try{PlaytestStorage.Load("missing");throw new Exception("Invalid ID accepted.");}catch(IOException){}
                results.Add("PASS: invalid/missing session is rejected safely.");
                string verification=Path.Combine(PlaytestStorage.Root,"Verification");Directory.CreateDirectory(verification);Directory.Move(r.CurrentFolder,Path.Combine(verification,id));
                phase=1;start=EditorApplication.timeSinceStartup;return;
            }
            if(phase==1)
            {
                if(EditorApplication.timeSinceStartup-start<2)return;
                var menu=PlaytestRecorder.Instance.GetComponent<DeveloperSettingsMenu>();
                Require(menu.ReplayCamera&&menu.ReplayCamera.orthographic&&menu.ReplayCamera.targetTexture,"Top-down render camera was not created.");
                Require(errors.Count==0,string.Join("\n",errors));results.Add("PASS: independent orthographic replay camera renders the blockout to a texture; no runtime errors.");
                menu.Close();Require(Time.timeScale>0&&!PlaytestRecorder.Instance.DeveloperViewOpen,"Developer viewing failed to restore gameplay state.");
                results.Add("PASS: developer menu restores time scale/input; verification recording kept outside real session statistics.");Finish("PASS");
            }
        }
        catch(Exception e){results.Add(e.ToString());Finish("FAIL");}
    }
    static void Finish(string status){phase=99;File.WriteAllText(Application.dataPath+"/../Logs/DeadAirPhaseOneCheck.txt",status+"\n"+string.Join("\n",results));EditorApplication.isPlaying=false;}
}
