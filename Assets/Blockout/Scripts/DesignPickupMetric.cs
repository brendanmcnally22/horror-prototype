using UnityEngine;
using UHFPS.Runtime;
namespace Blockout.Discovery
{
    public class DesignPickupMetric : MonoBehaviour
    {
        public string PickupId;
        public AudioClip PickupLaugh;
        bool recorded;
        void Start(){var item=GetComponent<InteractableItem>();if(item){item.OnTakeEvent??=new UnityEngine.Events.UnityEvent();item.OnTakeEvent.AddListener(RecordPickup);}}
        public void RecordPickup(){if(recorded)return;recorded=true;if(PickupLaugh)AudioSource.PlayClipAtPoint(PickupLaugh,PlayerManager.Instance?PlayerManager.Instance.transform.position:transform.position,.75f);DesignSession.Record("fuse_collected",PickupId,transform.position);if(gameObject.scene.name=="AI Discovery Trial")foreach(var scare in FindObjectsByType<FirstFuseAudioScare>(FindObjectsSortMode.None))if(scare.gameObject.scene==gameObject.scene)scare.TriggerFromPickup(PlayerManager.Instance?PlayerManager.Instance.transform.position:transform.position);}
    }
}



