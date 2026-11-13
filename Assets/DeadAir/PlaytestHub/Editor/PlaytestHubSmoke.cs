using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UHFPS.Runtime;
using Blockout.Discovery;
using DeadAir.Playtesting;

[InitializeOnLoad]
public static class PlaytestHubSmoke
{
    const string Key="PlaytestHubSmoke.Active";
    static double began,step;static int phase;
    static readonly List<string> report=new List<string>();
    static readonly HashSet<string> runIds=new HashSet<string>();
    static string errors="";
    static PlaytestHubSmoke()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>
        {
            if(!SessionState.GetBool(Key,false))return;
            if(s==PlayModeStateChange.EnteredPlayMode){phase=0;began=EditorApplication.timeSinceStartup;report.Clear();runIds.Clear();errors="";Application.runInBackground=true;}
            if(s==PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Key,false);string previous=SessionState.GetString(Key+".Previous","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(previous)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
                // Preserve automated recordings separately so they do not appear as player data.
                string root=HubRunStore.Root,validation=System.IO.Path.Combine(root,"Validation");Directory.CreateDirectory(validation);
                foreach(string id in runIds.Where(id=>Guid.TryParseExact(id,"N",out _)))
                {string source=System.IO.Path.GetFullPath(System.IO.Path.Combine(root,id));if(source.StartsWith(System.IO.Path.GetFullPath(root)+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(source)&&!Directory.Exists(System.IO.Path.Combine(validation,id)))Directory.Move(source,System.IO.Path.Combine(validation,id));}
            }
        };
        Application.logMessageReceived+=(m,t,type)=>{if(SessionState.GetBool(Key,false)&&(type==LogType.Error||type==LogType.Exception)&&!m.StartsWith("Layout update failed"))errors+=m+"\n";};
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static void Mark(PlaytestHub hub){if(hub.CurrentRun!=null){runIds.Add(hub.CurrentRun.id);hub.CurrentRun.notes="AUTOMATED VALIDATION";}}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false))
        {
            if(!File.Exists("Logs/PlaytestHubSmoke.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete("Logs/PlaytestHubSmoke.request");SessionState.SetString(Key+".Previous",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/DeadAir/PlaytestHub/Scenes/Playtest Menu.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;return;
        }
        if(!EditorApplication.isPlaying||began==0||phase==99)return;
        try
        {
            if(EditorApplication.timeSinceStartup-began>180)throw new Exception("Smoke test timed out at phase "+phase);
            var hub=PlaytestHub.Instance;if(!hub||!hub.Experience)return;
            Mark(hub);
            if(phase==0)
            {
                Require(hub.Experience.Page==HubPage.Menu,"Menu did not boot.");Require(hub.Config.Experiences.Length==3,"Expected three experiences.");report.Add("PASS: custom menu boots with all three experiences.");step=EditorApplication.timeSinceStartup;phase=20;return;
            }
            if(phase==20){if(EditorApplication.timeSinceStartup-step<1)return;ScreenCapture.CaptureScreenshot("Logs/PlaytestHubMenu.png");step=EditorApplication.timeSinceStartup;phase=21;return;}
            if(phase==21){if(EditorApplication.timeSinceStartup-step<1)return;hub.ChooseExperience("lighting");phase=1;return;}
            if(phase==1)
            {
                if(hub.CurrentRun==null||hub.CurrentRun.experienceId!="lighting")return;
                var doors=UnityEngine.Object.FindObjectsByType<LightingTestDoor>();Require(doors.Length==3,"Lighting scene must have three doors.");
                Require(doors.Select(d=>d.Condition).OrderBy(v=>v).SequenceEqual(new[]{"Flickering white","Green","Red"}),"Wrong light conditions.");
                foreach(var door in doors.OrderBy(d=>d.Condition)){Require(door.Lever&&!door.Door.enabled,"Lighting doors must have levers and remain sealed.");door.Lever.InteractStart();Require(door.Door.MovingPart.localPosition==Vector3.zero,"Lighting door moved after lever use.");}report.Add("PASS: three respective levers record green/red/flickering-white choice order while all doors remain closed.");phase=2;return;
            }
            if(phase==2)
            {
                if(hub.Experience.Page!=HubPage.Feedback)return;
                Require(hub.FeedbackRun!=null&&hub.FeedbackRun.outcome=="completed"&&hub.FeedbackRun.lightingOrder.Count==3,"Lighting completion/feedback failed.");
                hub.FeedbackRun.clarity=4;hub.FeedbackRun.tension=3;hub.FeedbackRun.worked="AUTOMATED VALIDATION: feedback persistence";hub.SaveFeedback();phase=3;return;
            }
            if(phase==3)
            {
                if(hub.Experience.Page!=HubPage.Menu)return;
                var saved=HubRunStore.List().FirstOrDefault(r=>runIds.Contains(r.id)&&r.feedbackStatus=="submitted"&&r.clarity==4);Require(saved!=null,"Feedback not persisted.");report.Add("PASS: feedback saved with run ID and ratings, reloads from disk.");hub.ReviewFeedback(saved);phase=22;return;
            }
            if(phase==22){if(hub.Experience.Page!=HubPage.Feedback)return;hub.FeedbackRun.worked="UNSAVED EDIT";hub.SaveFeedback(true,"already_answered");phase=23;return;}
            if(phase==23){if(hub.Experience.Page!=HubPage.Results)return;Require(HubRunStore.List().Any(r=>runIds.Contains(r.id)&&r.feedbackStatus=="submitted"&&r.worked.Contains("feedback persistence")),"Skipping edited feedback overwrote the earlier submission.");report.Add("PASS: skipping already-submitted feedback preserves original answers.");hub.ChooseExperience("discovery");phase=4;return;}
            if(phase==4)
            {
                if(hub.CurrentRun==null||hub.CurrentRun.experienceId!="discovery")return;
                Require(hub.Experience.EnemyRoot,"Enemy slot missing.");
                var terminal=UnityEngine.Object.FindObjectsByType<VirgoTerminal>().First(t=>!t.PhoneOnly);Require(terminal.DesktopWallpaper&&terminal.DesktopWallpaper.name=="hamterbackground","Wallpaper not assigned.");
                var levers=UnityEngine.Object.FindObjectsByType<PrototypeLever>();Require(levers.All(l=>l.Handle&&l.JamRecoilAxis==Vector3.forward),"Not all levers upgraded.");
                var authored=levers.FirstOrDefault(l=>l.name.Equals("Lever_7",StringComparison.OrdinalIgnoreCase));if(authored)Require(authored.Targets.Any(t=>t)&&authored.GetComponent<Collider>(),"Lever_7 not wired/interactable.");
                terminal.Open();step=EditorApplication.timeSinceStartup;phase=14;return;
            }
            if(phase==14){if(EditorApplication.timeSinceStartup-step<1)return;ScreenCapture.CaptureScreenshot("Logs/PlaytestHubDesktop.png");step=EditorApplication.timeSinceStartup;phase=15;return;}
            if(phase==15)
            {
                if(EditorApplication.timeSinceStartup-step<1)return;if(VirgoTerminal.Active)VirgoTerminal.Active.Close();
                var panel=UnityEngine.Object.FindAnyObjectByType<FacilityPanelUse>();Require(panel,"Discovery fuse panel missing.");panel.InteractStart();
                foreach(var fuse in UnityEngine.Object.FindObjectsByType<InteractableItem>().Where(f=>f.name.StartsWith("Fuse - Optional Pickup"))){fuse.InteractWithObject();panel.InteractStart();}
                report.Add("PASS: native discovery fuse returns work with new lever handles and hamster desktop.");phase=5;return;
            }
            if(phase==5)
            {
                if(hub.Experience.Page!=HubPage.Feedback)return;
                Require(hub.FeedbackRun.fuseReturns==3&&hub.FeedbackRun.outcome=="completed","Discovery metrics/completion missing.");hub.SaveFeedback(true,"already_answered");phase=6;return;
            }
            if(phase==6)
            {
                if(hub.Experience.Page!=HubPage.Menu)return;hub.ChooseExperience("security");phase=7;return;
            }
            if(phase==7)
            {
                if(hub.CurrentRun==null||hub.CurrentRun.experienceId!="security")return;
                var terminal=UnityEngine.Object.FindAnyObjectByType<SecurityTrialTerminal>();Require(terminal,"Security terminal missing.");Require(terminal.CameraFeeds.Length==0,"Camera placements should be left to designer.");terminal.Open();Require(terminal.IsOpen&&Time.timeScale>0,"Security terminal paused world.");step=EditorApplication.timeSinceStartup;phase=8;return;
            }
            if(phase==8)
            {
                if(EditorApplication.timeSinceStartup-step<1.5)return;
                Require(hub.CurrentRun.terminalSeconds>1&&hub.CurrentRun.terminalUses==1,"Terminal usage not tracked.");hub.OpenSwitcher();Require(Time.timeScale==0,"Quick switch did not pause active time.");hub.ChooseExperience("lighting");phase=9;return;
            }
            if(phase==9)
            {
                if(hub.Experience.Page!=HubPage.Feedback)return;
                Require(hub.FeedbackRun.outcome=="switched","Unfinished switch was marked complete.");hub.SaveFeedback(true,"already_answered");report.Add("PASS: security operation is timed while world runs; switching saves an unfinished outcome and offers skippable feedback.");phase=10;return;
            }
            if(phase==10)
            {
                if(hub.CurrentRun==null||hub.CurrentRun.experienceId!="lighting")return;hub.SetDemoMode(true);hub.GoResults();phase=11;return;
            }
            if(phase==11)
            {
                if(hub.Experience.Page!=HubPage.Results)return;
                var runs=HubRunStore.List().Where(r=>runIds.Contains(r.id)).ToArray();Require(runs.Length>=4&&runs.Select(r=>r.id).Distinct().Count()==runs.Length,"Runs were overwritten or lost.");
                Require(runs.Any(r=>r.demo&&r.feedbackStatus=="skipped"),"Demo feedback bypass failed.");string csv=HubRunStore.Export();Require(File.Exists(csv)&&File.ReadAllText(csv).Contains("terminal_seconds"),"CSV export failed.");
                report.Add("PASS: demo bypass, separate results scene, distinct per-run pages, and CSV export.");step=EditorApplication.timeSinceStartup;phase=12;return;
            }
            if(phase==12){if(EditorApplication.timeSinceStartup-step<2)return;ScreenCapture.CaptureScreenshot("Logs/PlaytestHubResults.png");step=EditorApplication.timeSinceStartup;phase=13;return;}
            if(phase==13){if(EditorApplication.timeSinceStartup-step<2)return;Finish(string.IsNullOrWhiteSpace(errors)?"PASS":"FAIL: runtime errors");}
        }
        catch(Exception e){report.Add(e.ToString());Finish("FAIL");}
    }
    static void Finish(string result){phase=99;if(errors!="")report.Add(errors);File.WriteAllText("Logs/PlaytestHubSmoke.txt",result+"\n"+string.Join("\n",report));EditorApplication.isPlaying=false;}
}
