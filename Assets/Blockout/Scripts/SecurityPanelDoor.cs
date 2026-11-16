using UnityEngine;
using UnityEngine.Events;
using UHFPS.Runtime;
namespace Blockout.Discovery
{
    [DefaultExecutionOrder(-50)]
    public sealed class SecurityPanelDoor : MonoBehaviour
    {
        public string DoorLabel="SECURITY DOOR";
        public PrototypeMover Mover;
        public bool RequiresKeycard;
        public string RequiredCardId="96cdd54e574046aeb73adf165af2a6f5";
        public string CardLabel="Green security keycard";
        public Vector3 SafetyCenter=new Vector3(0,1.5f,0),SafetySize=new Vector3(3,3,1.2f);
        public UnityEvent OnAccessDenied=new UnityEvent(),OnOpenRequested=new UnityEvent(),OnCloseRequested=new UnityEvent();
        readonly Collider[] hits=new Collider[32];
        public string LastMessage {get;private set;}="Ready";
        public bool HasAccess=>!RequiresKeycard||(Inventory.HasReference&&Inventory.Instance.GetItemQuantity(RequiredCardId)>0);
        public bool Occupied
        {
            get
            {
                var s=transform.lossyScale;s=new Vector3(Mathf.Abs(s.x),Mathf.Abs(s.y),Mathf.Abs(s.z));
                int count=Physics.OverlapBoxNonAlloc(transform.TransformPoint(SafetyCenter),Vector3.Scale(SafetySize,s)*.5f,hits,transform.rotation,~0,QueryTriggerInteraction.Collide);
                if(count==hits.Length)return true;
                for(int i=0;i<count;i++)if(hits[i]&&hits[i].GetComponentInParent<PlayerManager>())return true;
                return false;
            }
        }
        public string Status=>!Mover?"OFFLINE":Mover.AtEnd?"OPEN":Mover.AtStart?"CLOSED":Mover.TargetEnd?"OPENING":"CLOSING";
        public bool Request(bool open)
        {
            if(!Mover){LastMessage="Door controller offline.";return false;}
            if(!HasAccess){LastMessage="ACCESS DENIED — requires "+CardLabel+" in inventory.";OnAccessDenied.Invoke();return false;}
            if(!open&&Occupied){LastMessage="CLOSING BLOCKED — player in doorway.";return false;}
            Mover.SetEnd(open);LastMessage=open?"Opening door.":"Closing door.";
            if(open)OnOpenRequested.Invoke();else OnCloseRequested.Invoke();
            DesignSession.Record(open?"security_door_open":"security_door_close",DoorLabel,transform.position);return true;
        }
        void Update(){if(Mover&&!Mover.TargetEnd&&!Mover.AtStart&&Occupied){Mover.Open();LastMessage="SAFETY REOPEN — doorway occupied.";}}
        void OnDrawGizmosSelected(){Gizmos.matrix=transform.localToWorldMatrix;Gizmos.color=Color.yellow;Gizmos.DrawWireCube(SafetyCenter,SafetySize);}
    }
}
