using UnityEngine;
using UHFPS.Runtime;
using UHFPS.Input;

namespace Blockout.Discovery
{
    // Adapter around the pack's pushing state: holding Use keeps the grip; releasing exits.
    public class FacilityCartHandle : MonoBehaviour, IStateInteract, IInteractTitle
    {
        public MovableObject Movement;
        public string Label = "Supply cart";
        public bool HoldToMove = true;
        PlayerStateMachine player;
        bool holding;
        int grabFrame;
        public bool IsHeld => holding;
        public StateParams OnStateInteract()
        {
            if (!Movement || !PlayerPresenceManager.HasReference) return null;
            var state = Movement.OnStateInteract();
            if (state != null) { player = PlayerManager.Instance.PlayerStateMachine; holding = true; grabFrame = Time.frameCount; }
            return state;
        }
        void LateUpdate()
        {
            if (!holding || !player || Time.frameCount <= grabFrame) return;
            if (!player.IsCurrent(PlayerStateMachine.PUSHING_STATE)) { holding = false; return; }
            if (HoldToMove && !InputManager.ReadButton(Controls.USE)) Release();
        }
        public void Release()
        {
            if (holding && player && player.IsCurrent(PlayerStateMachine.PUSHING_STATE)) player.ChangeState(PlayerStateMachine.WALK_STATE);
            holding = false;
        }
        void OnDisable() { Release(); }
        void OnApplicationFocus(bool focused) { if (!focused) Release(); }
        public TitleParams InteractTitle() => new TitleParams { title = Label, button1 = "Hold Use + move to push / pull" };
    }
}
