using UnityEngine;
using UHFPS.Runtime;
namespace Blockout.Discovery
{
    [DefaultExecutionOrder(-20)]
    public class PlatformSafetyGate : MonoBehaviour
    {
        public PrototypeMover Mover;
        public Transform LeftLeaf,RightLeaf;
        public Vector3 LeftClosed,RightClosed;
        public float SlideDistance=1.55f;
        public float DoorSpeed=2;
        public Vector3 EntranceCenter=new Vector3(0,.85f,-1.5f);
        public Vector3 EntranceSize=new Vector3(3,1.7f,.5f);
        public bool OpenAtStart=true,OpenAtEnd=true;
        public bool ReadyForTravel=>LeftLeaf&&RightLeaf&&!Blocked&&
            Vector3.Distance(LeftLeaf.localPosition,LeftClosed)<.002f&&Vector3.Distance(RightLeaf.localPosition,RightClosed)<.002f;
        public bool Blocked {get;private set;}
        public bool IsOpen {get;private set;}
        float nextHint;
        void OnEnable()
        {
            if(!Mover)Mover=GetComponentInParent<PrototypeMover>();
            if(Mover)Mover.RegisterGate(this);
        }
        void Start()
        {
            if(!Mover){Debug.LogWarning("Parent this gate under a platform's Carriage, or assign its Mover.",this);return;}
            if(!Mover.TravelRequested&& (Mover.TargetEnd?OpenAtEnd:OpenAtStart))SetLeaves(true,true);
        }
        void Update()
        {
            if(!Mover||!LeftLeaf||!RightLeaf)return;
            bool travel=Mover.TravelRequested;
            Blocked=false;
            if(travel)
            {
                var scaled=Vector3.Scale(EntranceSize,transform.lossyScale)*.5f;scaled=new Vector3(Mathf.Abs(scaled.x),Mathf.Abs(scaled.y),Mathf.Abs(scaled.z));
                foreach(var hit in Physics.OverlapBox(transform.TransformPoint(EntranceCenter),scaled,transform.rotation,~0,QueryTriggerInteraction.Ignore))
                    if(hit.GetComponentInParent<PlayerManager>()){Blocked=true;break;}
                if(Blocked&&Time.time>nextHint){DiscoveryLoop.Hint("Clear the doorway — the platform cannot move until the gate closes.");nextHint=Time.time+3;DesignSession.Record("gate_obstructed",name,transform.position);}
            }
            bool open=Blocked||(!travel&&(Mover.TargetEnd?OpenAtEnd:OpenAtStart));
            SetLeaves(open,false);
        }
        void SetLeaves(bool open,bool instant)
        {
            if(!LeftLeaf||!RightLeaf)return;IsOpen=open;
            var a=LeftClosed+(open?Vector3.left*SlideDistance:Vector3.zero);var b=RightClosed+(open?Vector3.right*SlideDistance:Vector3.zero);
            LeftLeaf.localPosition=instant?a:Vector3.MoveTowards(LeftLeaf.localPosition,a,DoorSpeed*Time.deltaTime);
            RightLeaf.localPosition=instant?b:Vector3.MoveTowards(RightLeaf.localPosition,b,DoorSpeed*Time.deltaTime);
        }
    }
}
