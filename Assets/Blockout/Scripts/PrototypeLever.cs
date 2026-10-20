using UnityEngine;
using UnityEngine.Events;
using UHFPS.Runtime;

namespace Blockout.Discovery
{
    public class PrototypeLever : MonoBehaviour, IInteractStart, IInteractTitle
    {
        public string Label = "Lever";
        public Transform Handle;
        public Vector3 OffAngle = new Vector3(-35, 0, 0);
        public Vector3 OnAngle = new Vector3(35, 0, 0);
        public bool IsOn;
        public bool Jammed;
        [TextArea] public string JamMessage = "JAMMED — The mechanism won't move.";
        public AudioClip JamSound;
        float jamUntil;
        AudioSource jamAudio;
        [Tooltip("Keep multiple controls for one platform in sync.")]
        public bool FollowTarget = true;
        public PrototypeMover[] Targets = new PrototypeMover[0];
        public UnityEvent<bool> OnChanged = new UnityEvent<bool>();
        void Start() { UpdateHandle(); }
        void Update()
        {
            if (!Jammed && FollowTarget && Targets.Length > 0 && Targets[0]) IsOn = Targets[0].TargetEnd;
            UpdateHandle();
        }
        void UpdateHandle() { if (Handle) Handle.localEulerAngles = (IsOn ? OnAngle : OffAngle) + Vector3.right * (Time.time < jamUntil ? Mathf.Sin((jamUntil-Time.time)*45)*3 : 0); }
        public void InteractStart()
        {
            if(Jammed)
            {
                if(Time.time<jamUntil)return;
                jamUntil=Time.time+.4f;DiscoveryLoop.Hint(JamMessage);
                if(JamSound){if(!jamAudio)jamAudio=gameObject.AddComponent<AudioSource>();jamAudio.spatialBlend=1;jamAudio.PlayOneShot(JamSound,.65f);}
                DesignSession.Record("jammed_lever",name,transform.position);return;
            }
            if (FollowTarget && Targets.Length > 0 && Targets[0]) IsOn = Targets[0].TargetEnd;
            IsOn = !IsOn; Apply(); OnChanged.Invoke(IsOn);
            DesignSession.Record("lever_used",name,transform.position,IsOn?1:0);
        }
        void Apply()
        {
            if (Handle) Handle.localEulerAngles = IsOn ? OnAngle : OffAngle;
            foreach (var target in Targets) if (target) target.SetEnd(IsOn);
        }
        public TitleParams InteractTitle() => new TitleParams { title = Jammed ? "JAMMED LEVER" : Label, button1 = Jammed ? "Stuck — cannot operate" : IsOn ? "Switch off / return" : "Switch on / move" };
    }
}
