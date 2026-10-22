using UnityEngine;
using UHFPS.Runtime;
namespace Blockout.Discovery
{
    public sealed class PlatformReturnAfterExit : MonoBehaviour
    {
        public PrototypeMover Mover;
        public BoxCollider RiderZone;
        [Min(0)] public float ReturnDelay=2;
        readonly Collider[] hits=new Collider[32];
        bool carriedPlayer;
        float emptySince=-1,nextCheck;
        public bool Occupied {get;private set;}
        void Update()
        {
            if(!Mover||!RiderZone||Time.time<nextCheck)return;nextCheck=Time.time+.1f;
            var scale=RiderZone.transform.lossyScale;scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
            int count=Physics.OverlapBoxNonAlloc(RiderZone.transform.TransformPoint(RiderZone.center),Vector3.Scale(RiderZone.size,scale)*.5f,hits,RiderZone.transform.rotation,~0,QueryTriggerInteraction.Collide);
            Occupied=count==hits.Length; // If saturated, conservatively keep the platform still.
            for(int i=0;i<count&&!Occupied;i++)Occupied=hits[i]&&hits[i].GetComponentInParent<PlayerManager>();
            if(Occupied){carriedPlayer=true;emptySince=-1;return;}
            if(Mover.AtStart){carriedPlayer=false;emptySince=-1;return;}
            if(!Mover.AtEnd||!carriedPlayer){emptySince=-1;return;}
            if(emptySince<0)emptySince=Time.time;
            if(Time.time-emptySince>=ReturnDelay){Mover.Close();carriedPlayer=false;emptySince=-1;DesignSession.Record("platform_auto_return",name,Mover.MovingPart.position);}
        }
    }
}
