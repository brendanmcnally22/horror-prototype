using UnityEngine;
using UnityEngine.Rendering;

namespace Blockout.Discovery
{
    [DefaultExecutionOrder(200)]
    public class FacilityFlashlightBeam : MonoBehaviour
    {
        public Light Source;
        public MeshRenderer Volume;
        public Transform PlayerRoot;
        [Range(.001f,.2f)] public float Density=.045f;
        [Range(1,24)] public float MaximumLength=14;
        public LayerMask ObstructionMask=~0;
        readonly RaycastHit[] hits=new RaycastHit[64];
        MaterialPropertyBlock block;
        public float VisibleLength {get;private set;}
        void LateUpdate()
        {
            if(!Source||!Volume)return;
            bool on=Source.isActiveAndEnabled&&Source.intensity>.01f;
            Volume.enabled=on;
            if(!on)return;
            float length=Mathf.Min(MaximumLength,Source.range);
            int count=Physics.RaycastNonAlloc(Source.transform.position,Source.transform.forward,hits,length,ObstructionMask,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];
                if(PlayerRoot&&hit.transform.IsChildOf(PlayerRoot))continue;
                length=Mathf.Min(length,hit.distance);
            }
            VisibleLength=length;
            float cone=Mathf.Tan(Source.spotAngle*.5f*Mathf.Deg2Rad);
            Volume.transform.SetPositionAndRotation(Source.transform.position+Source.transform.forward*length*.5f,Source.transform.rotation);
            Volume.transform.localScale=new Vector3(Mathf.Max(.01f,2*length*cone),Mathf.Max(.01f,2*length*cone),Mathf.Max(.01f,length));
            block??=new MaterialPropertyBlock();
            block.SetMatrix("_BeamWorldToLocal",Source.transform.worldToLocalMatrix);
            block.SetVector("_BeamParams",new Vector4(length,cone,Density,Mathf.Min(Source.intensity/3f,1.5f)));
            block.SetColor("_BeamColor",Source.color);
            Volume.SetPropertyBlock(block);
        }
        void OnDisable(){if(Volume)Volume.enabled=false;}
    }
}
