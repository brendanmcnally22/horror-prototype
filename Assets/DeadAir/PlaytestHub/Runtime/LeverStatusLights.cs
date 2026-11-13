using UnityEngine;
using Blockout.Discovery;
namespace DeadAir.Playtesting
{
    public class LeverStatusLights : MonoBehaviour
    {
        public PrototypeLever Lever;
        public Renderer Upper,Lower;
        MaterialPropertyBlock properties;
        void LateUpdate()
        {
            if(!Lever)return;
            Color off=new Color(.015f,.025f,.02f),on=new Color(.12f,.9f,.3f),jam=new Color(.9f,.06f,.025f);
            Apply(Upper,Lever.Jammed?jam:Lever.IsOn?off:on);
            Apply(Lower,Lever.Jammed?jam:Lever.IsOn?on:off);
        }
        void Apply(Renderer renderer,Color value)
        {
            if(!renderer)return;properties??=new MaterialPropertyBlock();renderer.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor",value);properties.SetColor("_EmissionColor",value);renderer.SetPropertyBlock(properties);
        }
    }
}
