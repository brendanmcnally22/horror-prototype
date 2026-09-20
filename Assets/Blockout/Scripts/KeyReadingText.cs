using UnityEngine;
using UHFPS.Runtime;
namespace Blockout.Progression
{
    [RequireComponent(typeof(InteractableItem))]
    public class KeyReadingText:MonoBehaviour
    {
        public KeyReadingProfile Text;
        void Awake(){Apply();}
        public void Apply()
        {
            if(!Text)return;
            var item=GetComponent<InteractableItem>();
            item.PaperText=new GString("*"+Text.ReadingText,Text.ReadingText);
            item.ExamineTitle=new GString("*"+Text.Title,Text.Title);
            item.ExamineInventoryTitle=false;item.IsPaper=true;item.TakeFromExamine=false;
        }
    }
}
