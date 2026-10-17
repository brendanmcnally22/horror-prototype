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
        [Header("Arrival / emergency lighting")]
        public bool BeginOnEmergencyPower = true;
        public Color AmberColor = new Color(1f, .42f, .06f);
        [Range(.05f, 1f)] public float AmberBrightness = .35f;
        public Transform ObjectivePanel;
        public bool ShowPanelMarker = true;
        public Color EmergencyAmbient = new Color(.018f,.022f,.032f);
        Color normalAmbient;
        bool armed;
        bool[] normalEnabled;
        bool[] prototypePowered;
        Color[] normalColors, prototypeColors;
        float[] normalIntensities, prototypeIntensities;
        bool[] prototypeFlicker;
        void Awake()
        {
            normalAmbient = RenderSettings.ambientLight;
            normalEnabled = new bool[NormalLights.Length];
            for (int i=0;i<NormalLights.Length;i++) normalEnabled[i] = NormalLights[i] && NormalLights[i].enabled;
            prototypePowered = new bool[NormalPrototypeLights.Length];
            for (int i=0;i<NormalPrototypeLights.Length;i++) prototypePowered[i] = NormalPrototypeLights[i] && NormalPrototypeLights[i].Powered;
            normalColors = new Color[NormalLights.Length]; normalIntensities = new float[NormalLights.Length];
            for(int i=0;i<NormalLights.Length;i++) if(NormalLights[i]) { normalColors[i]=NormalLights[i].color; normalIntensities[i]=NormalLights[i].intensity; }
            prototypeColors = new Color[NormalPrototypeLights.Length]; prototypeIntensities = new float[NormalPrototypeLights.Length]; prototypeFlicker = new bool[NormalPrototypeLights.Length];
            for(int i=0;i<NormalPrototypeLights.Length;i++) if(NormalPrototypeLights[i]) { prototypeColors[i]=NormalPrototypeLights[i].Color; prototypeIntensities[i]=NormalPrototypeLights[i].Intensity; prototypeFlicker[i]=NormalPrototypeLights[i].Flicker; }
        }
        void Start()
        {
            if(!ObjectivePanel) { var panel=FindAnyObjectByType<UHFPS.Runtime.FuseboxPuzzle>(); if(panel) ObjectivePanel=panel.transform; }
            if(BeginOnEmergencyPower) CutPower();
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
            foreach (var source in NormalLights) if (source && !source.GetComponentInParent<FacilityLamp>() && source.color.g > .65f && source.color.b > .65f) { source.color=AmberColor; source.intensity*=AmberBrightness; }
            foreach (var source in NormalPrototypeLights) if (source) { source.Color=AmberColor; source.Intensity*=AmberBrightness; source.Flicker=false; }
            foreach (var lamp in FindObjectsByType<FacilityLamp>(FindObjectsSortMode.None)) lamp.SetEmergencyMode(true, AmberColor, AmberBrightness);
            foreach (var source in EmergencyLights) if (source) source.enabled = true;
            if (ObjectiveDisplay) ObjectiveDisplay.Show("RESTORE FACILITY POWER\nFind 3 fuses and return them to the electrical panel", true);
            OnPowerCut.Invoke();
        }
        public void RestorePower()
        {
            if (!PowerCut || PowerRestored) return;
            PowerRestored = true;
            RenderSettings.ambientLight = normalAmbient;
            for (int i=0;i<NormalLights.Length;i++) if (NormalLights[i]) { NormalLights[i].enabled = normalEnabled[i]; NormalLights[i].color=normalColors[i]; NormalLights[i].intensity=normalIntensities[i]; }
            for (int i=0;i<NormalPrototypeLights.Length;i++) if (NormalPrototypeLights[i]) { NormalPrototypeLights[i].Color=prototypeColors[i]; NormalPrototypeLights[i].Intensity=prototypeIntensities[i]; NormalPrototypeLights[i].Flicker=prototypeFlicker[i]; NormalPrototypeLights[i].SetPowered(prototypePowered[i]); }
            foreach (var lamp in FindObjectsByType<FacilityLamp>(FindObjectsSortMode.None)) lamp.SetEmergencyMode(false, AmberColor, AmberBrightness);
            OnPowerRestored.Invoke();
        }
        public void RestoreLocalPower()
        {
            if (!PowerCut || EmergencyLights.Length == 0 || !EmergencyLights[0]) return;
            EmergencyLights[0].color = new Color(.88f,.94f,1);
            EmergencyLights[0].intensity = 2.5f;
        }
        void OnGUI()
        {
            if(!ShowPanelMarker || !PowerCut || PowerRestored || !ObjectivePanel || VirgoTerminal.Active) return;
            var camera=Camera.main; if(!camera) return;
            Vector3 point=camera.WorldToScreenPoint(ObjectivePanel.position+Vector3.up*.8f);
            float x=point.z>0 ? Mathf.Clamp(point.x,150,Screen.width-150) : Screen.width/2;
            float y=point.z>0 ? Mathf.Clamp(Screen.height-point.y,100,Screen.height-100) : Screen.height-110;
            float distance=Vector3.Distance(camera.transform.position,ObjectivePanel.position);
            GUI.Box(new Rect(x-145,y,290,52),"ELECTRICAL PANEL  /  "+distance.ToString("0")+" m\n"+(point.z>0 ? "Return fuses here to restore power" : "Turn toward the electrical panel"));
        }
    }
}
