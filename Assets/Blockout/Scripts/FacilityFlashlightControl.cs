using UnityEngine;
using UHFPS.Input;
using UHFPS.Runtime;

namespace Blockout.Discovery
{
    public class FacilityFlashlightControl : MonoBehaviour
    {
        public PlayerItemsManager Items;
        public FlashlightItem Flashlight;
        void Update()
        {
            if(!Items||!Flashlight||!Items.CanInteract||!GameManager.HasReference)return;
            var game=GameManager.Instance;
            if(game.IsPaused||game.IsInventoryShown||game.PlayerDied||Time.timeScale<=0||
                !game.PlayerPresence.IsUnlockedAndCamera||Flashlight.ExamineController.IsExamining)return;
            if(InputManager.ReadButtonOnce(this,Controls.FLASHLIGHT))
            {
                int index=Items.PlayerItems.IndexOf(Flashlight);
                if(index>=0)Items.SwitchPlayerItem(index);
            }
        }
    }
}
