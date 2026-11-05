using UnityEngine;
using UnityEngine.Events;
using UHFPS.Runtime;
using UHFPS.Input;
namespace Blockout.Discovery
{
    public sealed class HeavyTwoHandLever : MonoBehaviour,IInteractStart,IInteractTitle
    {
        public Transform HandlePivot;
        public GameObject PrototypeHands;
        [Min(.2f)]public float PullSeconds=5;
        [Min(.1f)]public float ReturnSeconds=2;
        public float RaisedAngle=55,LoweredAngle=-55,UseDistance=2.3f;
        public float NoiseRadius=18,NoisePulseInterval=.65f;
        public AudioSource MechanismAudio;
        public AudioClip PullLoop,LatchSound;
        public UnityEvent OnPullStarted=new UnityEvent(),OnPullCancelled=new UnityEvent(),OnCompleted=new UnityEvent(),OnReset=new UnityEvent();
        public UnityEvent<float> OnProgress=new UnityEvent<float>();
        public float Progress {get;private set;}
        public bool Pulling {get;private set;}
        public bool Completed {get;private set;}
        PlayerManager player;
        bool movementWasEnabled,itemsWereUsable,hadItem;
        float nextNoise;
        void Start(){if(PrototypeHands)PrototypeHands.SetActive(false);ApplyPose();}
        public TitleParams InteractTitle()=>new TitleParams{title="HEAVY SECURITY LEVER",button1=Completed?"Latched — use reset station":"Hold to pull — both hands / loud"};
        public void InteractStart(){if(PlayerManager.Instance)BeginPull(PlayerManager.Instance);}
        public bool BeginPull(PlayerManager candidate)
        {
            if(Pulling||Completed||!candidate||Time.timeScale<=0)return false;
            if(Vector3.Distance(candidate.transform.position,transform.position)>UseDistance)return false;
            if(GameManager.HasReference&&(!GameManager.Instance.PlayerPresence.IsUnlockedAndCamera||GameManager.Instance.IsPaused))return false;
            player=candidate;movementWasEnabled=player.PlayerStateMachine.enabled;itemsWereUsable=player.PlayerItems.IsItemsUsable;hadItem=player.PlayerItems.IsAnyEquipped;
            player.PlayerItems.DeactivateCurrentItem();player.PlayerItems.IsItemsUsable=false;player.PlayerStateMachine.enabled=false;
            Pulling=true;nextNoise=0;if(PrototypeHands)PrototypeHands.SetActive(true);
            if(MechanismAudio&&PullLoop){MechanismAudio.clip=PullLoop;MechanismAudio.loop=true;MechanismAudio.Play();}
            OnPullStarted.Invoke();DesignSession.Record("heavy_lever_started",name,transform.position);return true;
        }
        void Update()
        {
            if(Pulling)
            {
                if(Time.timeScale<=0){if(MechanismAudio)MechanismAudio.Pause();return;}
                if(!player||!InputManager.ReadButton(Controls.USE)||Vector3.Distance(player.transform.position,transform.position)>UseDistance||
                    (player.GetComponent<PlayerHealth>()&&player.GetComponent<PlayerHealth>().IsDead)||
                    (GameManager.HasReference&&(GameManager.Instance.IsPaused||GameManager.Instance.IsInventoryShown)))
                {CancelPull();return;}
                if(MechanismAudio&&!MechanismAudio.isPlaying)MechanismAudio.UnPause();
                AdvancePull(Time.deltaTime);
            }
            else if(!Completed&&Progress>0){Progress=Mathf.MoveTowards(Progress,0,Time.deltaTime/Mathf.Max(.1f,ReturnSeconds));ApplyPose();}
        }
        public void AdvancePull(float seconds)
        {
            if(!Pulling)return;
            Progress=Mathf.Clamp01(Progress+Mathf.Max(0,seconds)/Mathf.Max(.2f,PullSeconds));ApplyPose();OnProgress.Invoke(Progress);
            if(Time.time>=nextNoise){HeavyLeverNoise.Emit(transform.position,NoiseRadius,.8f,gameObject);nextNoise=Time.time+Mathf.Max(.1f,NoisePulseInterval);}
            if(Progress<1)return;
            Completed=true;ReleasePlayer();
            if(MechanismAudio&&LatchSound)MechanismAudio.PlayOneShot(LatchSound,.9f);
            HeavyLeverNoise.Emit(transform.position,NoiseRadius*1.25f,1,gameObject);
            DesignSession.Record("heavy_lever_completed",name,transform.position);OnCompleted.Invoke();
        }
        void ApplyPose(){if(HandlePivot)HandlePivot.localRotation=Quaternion.Euler(Mathf.Lerp(RaisedAngle,LoweredAngle,Mathf.SmoothStep(0,1,Progress)),0,0);}
        public void CancelPull(){if(!Pulling)return;ReleasePlayer();OnPullCancelled.Invoke();DesignSession.Record("heavy_lever_cancelled",name,transform.position,Progress);}
        void ReleasePlayer()
        {
            Pulling=false;if(MechanismAudio)MechanismAudio.Stop();if(PrototypeHands)PrototypeHands.SetActive(false);
            if(player){player.PlayerStateMachine.enabled=movementWasEnabled;player.PlayerItems.IsItemsUsable=itemsWereUsable;if(hadItem)player.PlayerItems.ActivatePreviouslyDeactivatedItem();}player=null;
        }
        public void ResetLever(){if(Pulling)ReleasePlayer();Completed=false;Progress=0;ApplyPose();OnReset.Invoke();}
        void OnDisable(){if(Pulling)ReleasePlayer();}
        void OnGUI()
        {
            if(!Pulling)return;
            float w=360,x=(Screen.width-w)/2,y=Screen.height-135;
            GUI.Box(new Rect(x,y,w,70),"PULLING — BOTH HANDS OCCUPIED\nRelease interaction to let go");
            var old=GUI.color;GUI.color=new Color(1,.65f,.1f);GUI.DrawTexture(new Rect(x+12,y+52,(w-24)*Progress,7),Texture2D.whiteTexture);GUI.color=old;
        }
    }
}
