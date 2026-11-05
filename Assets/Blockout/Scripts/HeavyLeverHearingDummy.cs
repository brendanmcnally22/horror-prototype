using UnityEngine;
namespace Blockout.Discovery
{
    public sealed class HeavyLeverHearingDummy : MonoBehaviour
    {
        public float Speed=1.25f,StopDistance=2.6f;
        public Renderer Indicator;
        Vector3 home,target;bool investigating;
        MaterialPropertyBlock block;
        void Awake(){home=transform.position;block=new MaterialPropertyBlock();}
        public void Investigate(Vector3 position){target=position;target.y=transform.position.y;investigating=true;Tint(Color.red);}
        public void ResetDummy(){transform.position=home;investigating=false;Tint(new Color(.1f,.8f,.65f));}
        void Tint(Color color){if(!Indicator)return;block??=new MaterialPropertyBlock();block.SetColor("_BaseColor",color);block.SetColor("_EmissionColor",color);Indicator.SetPropertyBlock(block);}
        void Update(){if(investigating&&Vector3.Distance(transform.position,target)>StopDistance)transform.position=Vector3.MoveTowards(transform.position,target,Speed*Time.deltaTime);}
    }
}
