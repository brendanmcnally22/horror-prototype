using UnityEngine;

namespace Blockout.Discovery
{
    // Steady light: no per-frame updates and no material instances.
    public class FacilityLamp : MonoBehaviour
    {
        public Light[] Sources = new Light[0];
        public Renderer[] Diffusers = new Renderer[0];
        public Color Color = Color.white;
        [Min(0)] public float Intensity = 2;
        [Min(.1f)] public float Range = 6;
        [Min(0)] public float Glow = 1.5f;
        public bool EmergencySupply;
        public bool Powered = true;
        MaterialPropertyBlock properties;
        void Start()
        {
            var power = FindAnyObjectByType<FacilityPowerSequence>();
            if (power && power.PowerCut && !power.PowerRestored && !EmergencySupply) Powered = false;
            Apply();
        }
        public void SetPowered(bool on) { Powered = on; Apply(); }
        void OnValidate() { Apply(); }
        void Apply()
        {
            foreach (var source in Sources) if (source)
            {
                source.enabled = Powered; source.color = Color; source.intensity = Intensity; source.range = Range;
                source.shadows = LightShadows.None;
            }
            properties ??= new MaterialPropertyBlock();
            foreach (var diffuser in Diffusers) if (diffuser)
            {
                diffuser.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", Powered ? Color : Color * .08f);
                properties.SetColor("_EmissionColor", Powered ? Color * Glow : UnityEngine.Color.black);
                diffuser.SetPropertyBlock(properties);
            }
        }
    }
}
