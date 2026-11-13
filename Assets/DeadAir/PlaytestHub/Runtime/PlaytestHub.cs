using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UHFPS.Runtime;
using Blockout.Discovery;

namespace DeadAir.Playtesting
{
    [DefaultExecutionOrder(30000)]
    public partial class PlaytestHub : MonoBehaviour
    {
        public static PlaytestHub Instance {get;private set;}
        public PlaytestHubConfig Config {get;private set;}
        public HubRun CurrentRun {get;private set;}
        public HubRun FeedbackRun {get;private set;}
        public PlaytestExperience Experience {get;private set;}
        public bool SwitcherOpen {get;private set;}
        public bool DemoMode;
        public string Message {get;private set;}="";
        public HubExperienceDefinition Definition=>Experience?Config.Experiences.FirstOrDefault(e=>e.Id==Experience.ExperienceId):null;
        PlaytestRecorder recorder;
        DiscoveryLoop discovery;
        string pendingDestination;
        bool loading;
        double nextSave;
        float lightingFinishedAt=-1;
        float previousScale;
        CursorLockMode previousCursor;
        bool previousVisible;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics(){Instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if(Instance)return;var config=Resources.Load<PlaytestHubConfig>("DeadAirPlaytestHub");if(!config)return;
            string path=SceneManager.GetActiveScene().path;
            if(path!=config.MenuScene&&path!=config.FeedbackScene&&path!=config.ResultsScene&&!config.Experiences.Any(e=>e.ScenePath==path))return;
            var go=new GameObject("Dead Air - Playtest Hub");DontDestroyOnLoad(go);go.AddComponent<PlaytestHub>();
        }
        void Awake()
        {
            if(Instance&&Instance!=this){Destroy(gameObject);return;}
            Instance=this;Config=Resources.Load<PlaytestHubConfig>("DeadAirPlaytestHub");SceneManager.sceneLoaded+=SceneLoaded;
            ConfigureScene();
        }
        void SceneLoaded(Scene scene,LoadSceneMode mode)
        {
            if(CurrentRun!=null)SealRun("restarted","scene_reloaded");
            loading=false;ConfigureScene();
        }
        void ConfigureScene()
        {
            Experience=FindAnyObjectByType<PlaytestExperience>();discovery=null;lightingFinishedAt=-1;
            if(Experience&&Experience.Page==HubPage.Experience)
            {
                foreach(var old in FindObjectsByType<DiscoveryPlaytestRestart>(FindObjectsSortMode.None))old.enabled=false;
                discovery=FindAnyObjectByType<DiscoveryLoop>();
            }
            else{Cursor.lockState=CursorLockMode.None;Cursor.visible=true;RefreshBook();}
        }
        void Update()
        {
            if(!Config||loading||!Experience)return;
            if(Experience.Page!=HubPage.Experience)return;
            if(Keyboard.current!=null&&Keyboard.current.f6Key.wasPressedThisFrame)
            {if(SwitcherOpen)CloseSwitcher();else if(!FindObjectsByType<DeveloperSettingsMenu>(FindObjectsSortMode.None).Any(d=>d.IsOpen))OpenSwitcher();}
            if(CurrentRun==null)
            {
                recorder=PlaytestRecorder.Instance;
                if(recorder&&recorder.Recording&&recorder.Metadata.scenePath==SceneManager.GetActiveScene().path&&Definition!=null)
                {
                    CurrentRun=new HubRun{id=recorder.Metadata.id,experienceId=Definition.Id,title=Definition.Title,scenePath=recorder.Metadata.scenePath,startedUtc=recorder.Metadata.startedUtc,version=recorder.Metadata.version,demo=DemoMode};
                    recorder.EventRecorded+=TrackEvent;nextSave=0;
                }
                return;
            }
            if(recorder)
            {
                CurrentRun.activeSeconds=recorder.Elapsed;
                CurrentRun.wallSeconds=(DateTime.UtcNow-DateTime.Parse(CurrentRun.startedUtc,null,System.Globalization.DateTimeStyles.RoundtripKind)).TotalSeconds;
            }
            if(!SwitcherOpen&&Time.timeScale>0&&!(GameManager.HasReference&&GameManager.Instance.IsPaused))
            {
                bool usingTerminal=Definition.Kind==HubExperienceKind.Security?SecurityTrialTerminal.Active!=null:SecurityTrialTerminal.Active!=null||(VirgoTerminal.Active&&!VirgoTerminal.Active.PhoneOnly);
                if(usingTerminal)CurrentRun.terminalSeconds+=Time.unscaledDeltaTime;
            }
            if(Time.realtimeSinceStartupAsDouble>=nextSave){nextSave=Time.realtimeSinceStartupAsDouble+5;HubRunStore.Save(CurrentRun);}
            if(GameManager.HasReference&&GameManager.Instance.PlayerDied){CurrentRun.deaths++;Finish("failed","player_death");return;}
            if(Definition.Kind==HubExperienceKind.Discovery&&discovery&&discovery.Returned>=discovery.RequiredFuses){Finish("completed","three_fuses_returned");return;}
            if(Definition.Kind==HubExperienceKind.Security&&CurrentRun.terminalSeconds>=Experience.SecurityOperatingSeconds){Finish("completed","security_operating_goal");}
            if(Definition.Kind==HubExperienceKind.Lighting&&lightingFinishedAt>=0&&Time.time>=lightingFinishedAt)Finish("completed","three_lighting_doors_tested");
        }
        void LateUpdate()
        {
            if(SwitcherOpen||(Experience&&Experience.Page!=HubPage.Experience)){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        }
        void TrackEvent(PlaytestEvent evt)
        {
            if(CurrentRun==null)return;
            if(evt.type=="fuse_returned")CurrentRun.fuseReturns=Mathf.Max(CurrentRun.fuseReturns,Mathf.RoundToInt(evt.value));
            if(evt.type=="terminal_opened"||evt.type=="security_terminal_opened")CurrentRun.terminalUses++;
            if(evt.type=="lever_used"||evt.type=="lighting_door_used"||evt.type=="security_door_used")CurrentRun.interactions++;
        }
        public void RecordLightingChoice(string condition)
        {
            if(CurrentRun==null||CurrentRun.lightingOrder.Contains(condition))return;
            CurrentRun.lightingOrder.Add(condition);CurrentRun.lightingTimes.Add((float)CurrentRun.activeSeconds);HubRunStore.Save(CurrentRun);
            if(CurrentRun.lightingOrder.Count>=3)lightingFinishedAt=Time.time+5;
        }
        public void OpenSwitcher()
        {
            if(SwitcherOpen)return;
            if(VirgoTerminal.Active)VirgoTerminal.Active.Close();if(SecurityTrialTerminal.Active)SecurityTrialTerminal.Active.Close();
            if(GameManager.HasReference&&GameManager.Instance.IsPaused)GameManager.Instance.ResumeGame();
            previousScale=Time.timeScale;previousCursor=Cursor.lockState;previousVisible=Cursor.visible;
            if(GameManager.HasReference)GameManager.Instance.FreezePlayer(true,true);
            Time.timeScale=0;SwitcherOpen=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        public void CloseSwitcher()
        {
            if(!SwitcherOpen)return;SwitcherOpen=false;Time.timeScale=previousScale;
            if(GameManager.HasReference)GameManager.Instance.FreezePlayer(false);
            Cursor.lockState=previousCursor;Cursor.visible=previousVisible;
        }
        public void ChooseExperience(string id)
        {
            var entry=Config.Experiences.FirstOrDefault(e=>e.Id==id);if(entry==null)return;
            if(!Application.CanStreamedLevelBeLoaded(entry.ScenePath)){Message="Experience scene is not included in this build.";return;}
            if(CurrentRun!=null)Finish(CurrentRun.experienceId==id?"restarted":"switched","experience_selected",entry.ScenePath);
            else Load(entry.ScenePath);
        }
        public void Finish(string outcome,string reason,string destination=null)
        {
            if(loading)return;
            if(VirgoTerminal.Active)VirgoTerminal.Active.Close();if(SecurityTrialTerminal.Active)SecurityTrialTerminal.Active.Close();
            CloseSwitcher();pendingDestination=destination??Config.MenuScene;
            FeedbackRun=SealRun(outcome,reason);
            if(FeedbackRun!=null&&FeedbackRun.feedbackStatus=="pending"&&!DemoMode)Load(Config.FeedbackScene);
            else Load(pendingDestination);
        }
        HubRun SealRun(string outcome,string reason)
        {
            if(CurrentRun==null)return null;var run=CurrentRun;
            if(recorder){run.activeSeconds=recorder.Elapsed;recorder.EventRecorded-=TrackEvent;if(recorder.Recording)recorder.Stop(outcome,reason);}
            run.endedUtc=DateTime.UtcNow.ToString("O");run.wallSeconds=Math.Max(0,(DateTime.UtcNow-DateTime.Parse(run.startedUtc,null,System.Globalization.DateTimeStyles.RoundtripKind)).TotalSeconds);
            run.outcome=outcome;run.reason=reason;
            if(DemoMode&&run.feedbackStatus=="pending"){run.feedbackStatus="skipped";run.skipReason="demo_mode";}
            HubRunStore.Save(run);CurrentRun=null;return run;
        }
        public void SaveFeedback(bool skip=false,string reason="skipped_by_player")
        {
            if(FeedbackRun!=null)
            {
                if(skip&&FeedbackRun.feedbackStatus=="submitted")
                {
                    var saved=HubRunStore.List().FirstOrDefault(r=>r.id==FeedbackRun.id&&r.feedbackStatus=="submitted");
                    if(saved!=null)FeedbackRun=saved;
                }
                if(FeedbackRun.feedbackStatus!="submitted"||!skip){FeedbackRun.feedbackStatus=skip?"skipped":"submitted";FeedbackRun.skipReason=skip?reason:"";}
                if(!HubRunStore.Save(FeedbackRun)){Message=HubRunStore.LastError;return;}
            }
            Load(pendingDestination??Config.ResultsScene);
        }
        public void ReviewFeedback(HubRun run){FeedbackRun=run;pendingDestination=Config.ResultsScene;Load(Config.FeedbackScene);}
        public void SetDemoMode(bool value){DemoMode=value;if(CurrentRun!=null)CurrentRun.demo=value;}
        public void GoMenu(){if(CurrentRun!=null)Finish("stopped","back_to_menu");else Load(Config.MenuScene);}
        public void GoResults(){if(CurrentRun!=null)Finish("stopped","review_results",Config.ResultsScene);else Load(Config.ResultsScene);}
        void Load(string path)
        {
            if(loading)return;
            if(string.IsNullOrWhiteSpace(path)||!Application.CanStreamedLevelBeLoaded(path)){Message="Destination is unavailable in this build.";return;}
            CloseSwitcher();if(VirgoTerminal.Active)VirgoTerminal.Active.Close();if(SecurityTrialTerminal.Active)SecurityTrialTerminal.Active.Close();
            foreach(var rider in FindObjectsByType<PrototypeRider>(FindObjectsSortMode.None))rider.ReleaseBeforeSceneChange();
            loading=true;Time.timeScale=1;SaveGameManager.ClearLoadType();SaveGameManager.ClearLoadName();SceneManager.LoadSceneAsync(path,LoadSceneMode.Single);
        }
        void OnApplicationQuit(){SealRun("exited","application_exit");}
        void OnDestroy(){SceneManager.sceneLoaded-=SceneLoaded;if(Instance==this){SealRun("interrupted","play_mode_ended");Instance=null;}}
    }
}
