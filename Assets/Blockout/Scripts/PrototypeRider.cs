using UnityEngine;
using UHFPS.Runtime;

namespace Blockout.Discovery
{
    // Uses the same hero/camera parenting API as the kit's elevator.
    public class PrototypeRider : MonoBehaviour
    {
        public Transform Platform;
        PlayerManager rider;
        Transform previousParent;
        bool previousSync;
        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerManager>();
            if (!player || rider) return;
            rider = player; previousParent = player.transform.parent;
            previousSync = Physics.autoSyncTransforms;
            rider.ParentToObject(Platform);
        }
        void OnTriggerExit(Collider other)
        {
            if (rider && other.gameObject == rider.gameObject) Release();
        }
        void OnDisable() { Release(); }
        void Release()
        {
            if (!rider) return;
            if (rider.transform.parent == Platform)
            {
                rider.UnparentFromObject();
                rider.transform.SetParent(previousParent, true);
                Physics.autoSyncTransforms = previousSync;
            }
            rider = null;
        }
    }
}
