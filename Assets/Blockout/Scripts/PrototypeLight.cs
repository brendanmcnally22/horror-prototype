using UnityEngine;

namespace Blockout.Discovery
{
    [RequireComponent(typeof(Light))]
    public class PrototypeLight : MonoBehaviour
    {
        public Color Color = Color.white;
        [Min(0)] public float Intensity = 3;
        [Min(.1f)] public float Range = 12;
        public bool Flicker = true;
        [Range(0, 1)] public float MinimumBrightness = .1f;
        public Vector2 Interval = new Vector2(.05f, .3f);
        public bool Powered = true;
        Light source;
        float next;
        void Awake() { source = GetComponent<Light>(); }
        void Update()
        {
            source.color = Color; source.range = Range;
            if (!Powered) { source.intensity = 0; return; }
            if (!Flicker) { source.intensity = Intensity; return; }
            if (Time.time < next) return;
            next = Time.time + Random.Range(Mathf.Max(.01f, Interval.x), Mathf.Max(.01f, Interval.x, Interval.y));
            source.intensity = Intensity * Random.Range(MinimumBrightness, 1f);
        }
        public void SetPowered(bool powered) { Powered = powered; }
        public void RestoreWhite() { Color = UnityEngine.Color.white; Flicker = false; Powered = true; }
        void OnValidate()
        {
            var light = GetComponent<Light>();
            light.color = Color; light.range = Range; light.intensity = Powered ? Intensity : 0;
        }
    }
}
