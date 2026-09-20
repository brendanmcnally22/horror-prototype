using UHFPS.Runtime;
using UnityEngine;

namespace Blockout.Optimization
{
    [DisallowMultipleComponent]
    public sealed class RadioExamineLighting : MonoBehaviour
    {
        public ExamineController Controller;
        [Min(0)] public float InspectionIntensity = .65f;
        [Min(.1f)] public float InspectionRange = 1.5f;
        Light lightSource;
        float previousIntensity, previousRange;
        bool applied;

        public void BeginInspection()
        {
            if (applied || !Controller || !Controller.ExamineLight) return;
            lightSource = Controller.ExamineLight;
            previousIntensity = lightSource.intensity;
            previousRange = lightSource.range;
            lightSource.intensity = InspectionIntensity;
            lightSource.range = InspectionRange;
            applied = true;
        }

        public void EndInspection()
        {
            if (!applied) return;
            if (lightSource) { lightSource.intensity = previousIntensity; lightSource.range = previousRange; }
            applied = false;
        }

        void OnDisable() { EndInspection(); }
    }
}
