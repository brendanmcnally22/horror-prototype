using UnityEngine;

namespace Blockout.Discovery
{
    // Two lights only. The existing lever controls the elevator.
    public class ElevatorFloorIndicator : MonoBehaviour
    {
        public PrototypeMover Elevator;
        public PrototypeLever Lever;
        public Renderer UpperLight, LowerLight;
        public Color IdleColor = new Color(.025f, .035f, .025f);
        public Color TravelColor = new Color(1f, .55f, .08f);
        public Color ArrivalColor = new Color(.25f, 1f, .42f);
        [Min(.1f)] public float PulseSpeed = 1.5f;
        MaterialPropertyBlock properties;

        void OnEnable() { if (Lever) Lever.OnChanged.AddListener(OnLeverChanged); }
        void OnDisable() { if (Lever) Lever.OnChanged.RemoveListener(OnLeverChanged); }
        void Update() { Refresh(); }
        public void OnLeverChanged(bool value) { Refresh(); }
        public void Refresh()
        {
            if (!Elevator) return;
            bool travelling = Elevator.TravelRequested;
            bool lower = Elevator.TargetEnd;
            Color active = travelling ? Color.Lerp(IdleColor, TravelColor, .35f + .65f * (.5f + .5f * Mathf.Sin(Time.time * PulseSpeed * Mathf.PI * 2))) : ArrivalColor;
            SetLight(UpperLight, lower ? IdleColor : active);
            SetLight(LowerLight, lower ? active : IdleColor);
        }
        void SetLight(Renderer lamp, Color color)
        {
            if (!lamp) return;
            properties ??= new MaterialPropertyBlock();
            lamp.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", color);
            lamp.SetPropertyBlock(properties);
        }
    }
}
