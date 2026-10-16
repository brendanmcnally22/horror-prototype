using UnityEngine;
using UnityEngine.Events;
using UHFPS.Runtime;

namespace Blockout.Discovery
{
    [RequireComponent(typeof(BoxCollider))]
    public class DiscoveryObjectiveTrigger : MonoBehaviour
    {
        [TextArea] public string ObjectiveText = "Find 3 fuses and return them to the panel";
        public bool ShowFuseProgress = true;
        public bool TriggerOnce = true;
        public DiscoveryObjectiveDisplay Display;
        [Tooltip("Optional: objective can only appear after this facility loses power.")]
        public FacilityPowerSequence RequiredPowerCut;
        public UnityEvent OnTriggered = new UnityEvent();
        bool triggered;
        void Reset() { GetComponent<BoxCollider>().isTrigger = true; }
        void OnTriggerEnter(Collider other)
        {
            if (!other.GetComponentInParent<PlayerManager>() || (triggered && TriggerOnce)) return;
            if (RequiredPowerCut && !RequiredPowerCut.PowerCut) return;
            if (!Display) Display = FindAnyObjectByType<DiscoveryObjectiveDisplay>();
            if (!Display) { Debug.LogWarning("Objective trigger needs an Objective Display in the scene.", this); return; }
            triggered = true; Display.Show(ObjectiveText, ShowFuseProgress); OnTriggered.Invoke();
        }
        void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(.2f, .85f, 1, .16f); Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = Color.cyan; Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
