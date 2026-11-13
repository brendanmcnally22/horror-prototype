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
        void OnDisable()
        {
            // During scene teardown the carriage and its children are being deactivated.
            // Unity must finish destroying that hierarchy without reparenting it here.
            if(!gameObject.activeInHierarchy || (Platform&&!Platform.gameObject.activeInHierarchy))
            {
                if(rider)Physics.autoSyncTransforms=previousSync;
                rider=null;return;
            }
#if UNITY_EDITOR
            // Scene teardown restores the edit-mode hierarchy itself. Reparenting
            // a child during its parent's deactivation is rejected by Unity.
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (rider) Physics.autoSyncTransforms = previousSync;
                rider = null;
                return;
            }
#endif
            Release();
        }
        public void ReleaseBeforeSceneChange(){Release();}
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
