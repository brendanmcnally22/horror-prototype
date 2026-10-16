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
        [Tooltip("Keep multiple controls for one platform in sync.")]
        public bool FollowTarget = true;
        public PrototypeMover[] Targets = new PrototypeMover[0];
        public UnityEvent<bool> OnChanged = new UnityEvent<bool>();
        void Start() { UpdateHandle(); }
        void Update()
        {
            if (FollowTarget && Targets.Length > 0 && Targets[0]) IsOn = Targets[0].TargetEnd;
            UpdateHandle();
        }
        void UpdateHandle() { if (Handle) Handle.localEulerAngles = IsOn ? OnAngle : OffAngle; }
        public void InteractStart()
        {
            if (FollowTarget && Targets.Length > 0 && Targets[0]) IsOn = Targets[0].TargetEnd;
            IsOn = !IsOn; Apply(); OnChanged.Invoke(IsOn);
        }
        void Apply()
        {
            if (Handle) Handle.localEulerAngles = IsOn ? OnAngle : OffAngle;
            foreach (var target in Targets) if (target) target.SetEnd(IsOn);
        }
        public TitleParams InteractTitle() => new TitleParams { title = Label, button1 = IsOn ? "Switch off / return" : "Switch on / move" };
    }
}
