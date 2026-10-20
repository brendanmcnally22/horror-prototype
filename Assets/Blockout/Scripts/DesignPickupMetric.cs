using UnityEngine;
using UHFPS.Runtime;
namespace Blockout.Discovery
{
    public class DesignPickupMetric : MonoBehaviour
    {
        public string PickupId;
        bool recorded;
        void Start(){var item=GetComponent<InteractableItem>();if(item){item.OnTakeEvent??=new UnityEngine.Events.UnityEvent();item.OnTakeEvent.AddListener(RecordPickup);}}
        public void RecordPickup(){if(recorded)return;recorded=true;DesignSession.Record("fuse_collected",PickupId,transform.position);}
    }
}
