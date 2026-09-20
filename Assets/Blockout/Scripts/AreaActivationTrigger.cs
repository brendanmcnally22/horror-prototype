using System.Collections.Generic;
using UHFPS.Runtime;
using UnityEngine;

namespace Blockout.Optimization
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    [AddComponentMenu("Blockout/Optimization/Area Activation Trigger")]
    public sealed class AreaActivationTrigger : MonoBehaviour
    {
        [Tooltip("Scene objects to enable when the UHFPS player enters. Nothing is selected automatically.")]
        public GameObject[] EnableOnEnter = new GameObject[0];
        [Tooltip("Optional scene objects to disable on entry. Keep this trigger outside those objects. Does not run on exit.")]
        public GameObject[] DisableOnEnter = new GameObject[0];
        [Tooltip("If enabled, this trigger responds only to the first player entry this play session.")]
        public bool OneShot;

        readonly HashSet<Collider> occupants = new HashSet<Collider>();
        bool used;

        void Reset()
        {
            var box = GetComponent<BoxCollider>();
            box.isTrigger = true; box.size = new Vector3(3, 3, 1);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.GetComponentInParent<PlayerManager>()) return;
            occupants.RemoveWhere(c => !c || !c.enabled || !c.gameObject.activeInHierarchy);
            if (!occupants.Add(other) || occupants.Count != 1 || (OneShot && used)) return;
            Apply();
        }

        void OnTriggerExit(Collider other) { occupants.Remove(other); }
        void OnDisable() { occupants.Clear(); }

        // Also available to a UnityEvent, if a door or elevator should drive the change.
        public void Apply()
        {
            if (!Application.isPlaying || (OneShot && used)) return;
            used = true;
            // Enable first to avoid a blank frame. Enable wins if an object is in both lists.
            foreach (var target in EnableOnEnter) if (target) target.SetActive(true);
            foreach (var target in DisableOnEnter)
                if (target && System.Array.IndexOf(EnableOnEnter, target) < 0 && AreaActivationSafety.CanDisable(target, this)) target.SetActive(false);
        }

        void OnDrawGizmosSelected()
        {
            var box = GetComponent<BoxCollider>();
            if (!box) return;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(.1f, .9f, .6f, .2f); Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(.1f, .9f, .6f, 1); Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
