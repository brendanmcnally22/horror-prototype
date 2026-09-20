using UHFPS.Runtime;
using UnityEngine;

namespace Blockout.Optimization
{
    internal static class AreaActivationSafety
    {
        internal static bool CanDisable(GameObject target, Component owner)
        {
            if (!target) return false;
            // Keep the controlling object, player, and game manager alive.
            if (owner.transform.IsChildOf(target.transform) ||
                target.GetComponentInParent<PlayerManager>(true) || target.GetComponentInChildren<PlayerManager>(true) ||
                target.GetComponentInParent<GameManager>(true) || target.GetComponentInChildren<GameManager>(true))
            {
                Debug.LogWarning("Skipped disabling '" + target.name + "': it contains the controller, player or game manager, or is part of them.", owner);
                return false;
            }
            return true;
        }
    }
}
