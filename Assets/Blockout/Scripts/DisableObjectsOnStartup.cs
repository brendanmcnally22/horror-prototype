using UnityEngine;

namespace Blockout.Optimization
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Blockout/Optimization/Disable Objects On Startup")]
    public sealed class DisableObjectsOnStartup : MonoBehaviour
    {
        [Tooltip("Explicit scene objects to disable in Awake. No objects are discovered or selected automatically. You can later enable these using Area Activation Trigger.")]
        public GameObject[] ObjectsToDisable = new GameObject[0];

        void Awake()
        {
            foreach (var target in ObjectsToDisable)
                if (AreaActivationSafety.CanDisable(target, this)) target.SetActive(false);
        }
    }
}
