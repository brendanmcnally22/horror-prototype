using UnityEngine;
using UHFPS.Runtime;
namespace Blockout.Discovery
{
    public sealed class HeavyLeverResetStation : MonoBehaviour,IInteractStart,IInteractTitle
    {
        public HeavyTwoHandLever Lever;
        public HeavyLeverHearingDummy Dummy;
        public void InteractStart(){if(Lever)Lever.ResetLever();if(Dummy)Dummy.ResetDummy();}
        public TitleParams InteractTitle()=>new TitleParams{title="PROTOTYPE RESET",button1="Reset lever, shutter and hearing dummy"};
    }
}
