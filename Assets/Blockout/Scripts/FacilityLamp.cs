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
        bool emergencyMode;
        Color emergencyColor = new Color(1f, .42f, .06f);
        float emergencyBrightness = .35f;
        MaterialPropertyBlock properties;
        void Start()
        {
            var power = FindAnyObjectByType<FacilityPowerSequence>();
            if (power && power.PowerCut && !power.PowerRestored && !EmergencySupply)
                SetEmergencyMode(true, power.AmberColor, power.AmberBrightness);
            Apply();
        }
        public void SetPowered(bool on) { Powered = on; Apply(); }
        public void SetEmergencyMode(bool on, Color tint, float brightness)
        {
            emergencyMode = on && !EmergencySupply && Color.g > .65f && Color.b > .65f;
            emergencyColor = tint; emergencyBrightness = brightness; Apply();
        }
        void OnValidate() { Apply(); }
        void Apply()
        {
            var tint = emergencyMode ? emergencyColor : Color;
            float brightness = emergencyMode ? emergencyBrightness : 1f;
            foreach (var source in Sources) if (source)
            {
                source.enabled = Powered; source.color = tint; source.intensity = Intensity * brightness; source.range = Range;
                source.shadows = LightShadows.None;
            }
            properties ??= new MaterialPropertyBlock();
            foreach (var diffuser in Diffusers) if (diffuser)
            {
                diffuser.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", Powered ? tint : tint * .08f);
                properties.SetColor("_EmissionColor", Powered ? tint * Glow * brightness : UnityEngine.Color.black);
                diffuser.SetPropertyBlock(properties);
            }
        }
    }
}
