using UnityEngine;
using UHFPS.Runtime;
namespace Blockout.Discovery
{
    [DefaultExecutionOrder(100)]
    public class FacilityPlayerRules : MonoBehaviour
    {
        public bool DisableJumping=true;
        public bool DisableSprinting;
        void Start(){Apply();}
        public void Apply(){var player=GetComponent<PlayerStateMachine>();if(player){player.SetStateEnabled(PlayerStateMachine.JUMP_STATE,!DisableJumping);if(DisableSprinting)player.SetStateEnabled(PlayerStateMachine.RUN_STATE,false);}}
    }
}

