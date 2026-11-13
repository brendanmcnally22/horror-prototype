using UnityEngine;
using UHFPS.Runtime;
using Blockout.Discovery;

namespace DeadAir.Playtesting
{
    public class LightingTestDoor : MonoBehaviour,IInteractStart,IInteractTitle
    {
        public string Condition;
        public PrototypeMover Door;
        public PrototypeLever Lever;
        public TitleParams InteractTitle()=>new TitleParams{title="Sealed door",button1="Use its lever"};
        void Start(){if(Lever)Lever.OnChanged.AddListener(LeverChanged);}
        void OnDestroy(){if(Lever)Lever.OnChanged.RemoveListener(LeverChanged);}
        public void InteractStart()
        {
            DiscoveryLoop.Hint("This door stays sealed for the lighting test. Try its lever.");
        }
        public void LeverChanged(bool value)
        {
            DesignSession.Record("lighting_door_used",Condition,transform.position);
            if(PlaytestHub.Instance)PlaytestHub.Instance.RecordLightingChoice(Condition);
        }
    }
}
