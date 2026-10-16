using UnityEngine;
using UnityEngine.Events;

namespace Blockout.Discovery
{
    public class FacilityPowerSequence : MonoBehaviour
    {
        public PrototypeMover[] SecurityDoors = new PrototypeMover[0];
        public Light[] NormalLights = new Light[0];
        public PrototypeLight[] NormalPrototypeLights = new PrototypeLight[0];
        public Light[] EmergencyLights = new Light[0];
        public DiscoveryObjectiveDisplay ObjectiveDisplay;
        [TextArea] public string Objective = "EMERGENCY POWER — Find 3 fuses and return them to the panel";
        public UnityEvent OnPowerCut = new UnityEvent();
        public UnityEvent OnPowerRestored = new UnityEvent();
        public bool PowerCut { get; private set; }
        public bool PowerRestored { get; private set; }
        public Color EmergencyAmbient = new Color(.018f,.022f,.032f);
        Color normalAmbient;
        bool armed;
        bool[] normalEnabled;
        bool[] prototypePowered;
        void Awake()
        {
            normalAmbient = RenderSettings.ambientLight;
            normalEnabled = new bool[NormalLights.Length];
            for (int i=0;i<NormalLights.Length;i++) normalEnabled[i] = NormalLights[i] && NormalLights[i].enabled;
            prototypePowered = new bool[NormalPrototypeLights.Length];
            for (int i=0;i<NormalPrototypeLights.Length;i++) prototypePowered[i] = NormalPrototypeLights[i] && NormalPrototypeLights[i].Powered;
        }
        public void OnLeverChanged(bool opened) { if (!PowerCut) armed = opened; }
        void Update()
        {
            if (!armed || PowerCut || SecurityDoors.Length == 0) return;
            foreach (var door in SecurityDoors) if (!door || !door.AtEnd) return;
            CutPower();
        }
        public void CutPower()
        {
            if (PowerCut) return;
            PowerCut = true; armed = false;
            RenderSettings.ambientLight = EmergencyAmbient;
            foreach (var source in NormalLights) if (source) source.enabled = false;
            foreach (var source in NormalPrototypeLights) if (source) source.SetPowered(false);
            foreach (var lamp in FindObjectsByType<FacilityLamp>(FindObjectsSortMode.None)) if (!lamp.EmergencySupply) lamp.SetPowered(false);
            foreach (var source in EmergencyLights) if (source) source.enabled = true;
            if (ObjectiveDisplay) ObjectiveDisplay.Show(Objective, true);
            OnPowerCut.Invoke();
        }
        public void RestorePower()
        {
            if (!PowerCut || PowerRestored) return;
            PowerRestored = true;
            RenderSettings.ambientLight = normalAmbient;
            for (int i=0;i<NormalLights.Length;i++) if (NormalLights[i]) NormalLights[i].enabled = normalEnabled[i];
            for (int i=0;i<NormalPrototypeLights.Length;i++) if (NormalPrototypeLights[i]) NormalPrototypeLights[i].SetPowered(prototypePowered[i]);
            foreach (var lamp in FindObjectsByType<FacilityLamp>(FindObjectsSortMode.None)) if (!lamp.EmergencySupply) lamp.SetPowered(true);
            OnPowerRestored.Invoke();
        }
        public void RestoreLocalPower()
        {
            if (!PowerCut || EmergencyLights.Length == 0 || !EmergencyLights[0]) return;
            EmergencyLights[0].color = new Color(.88f,.94f,1);
            EmergencyLights[0].intensity = 2.5f;
        }
    }
}
