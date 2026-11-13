using UnityEngine;
namespace DeadAir.Playtesting
{
    public class LightingTestLens : MonoBehaviour
    {
        public Light Source;
        public Renderer Lens;
        public float ReferenceIntensity=3;
        MaterialPropertyBlock properties;
        void LateUpdate()
        {
            if(!Source||!Lens)return;
            properties??=new MaterialPropertyBlock();Lens.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor",Source.color*Mathf.Clamp01(Source.intensity/Mathf.Max(.01f,ReferenceIntensity)));Lens.SetPropertyBlock(properties);
        }
    }
}
