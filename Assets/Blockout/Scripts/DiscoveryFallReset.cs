using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UHFPS.Runtime;

namespace Blockout.Discovery
{
    [RequireComponent(typeof(BoxCollider))]
    public class DiscoveryFallReset : MonoBehaviour
    {
        bool restarting;

        void Reset() { GetComponent<BoxCollider>().isTrigger = true; }
        void OnTriggerEnter(Collider other)
        {
            if (restarting || !other.GetComponentInParent<PlayerManager>()) return;
            restarting = true;
            DesignSession.Record("level_restart_fall", name, other.transform.position);
            StartCoroutine(RestartLevel());
        }

        IEnumerator RestartLevel()
        {
            // Leave the physics callback before unloading the current level.
            yield return null;
            var scene = gameObject.scene;
            SaveGameManager.ClearLoadType();
            SaveGameManager.ClearLoadName();
            Time.timeScale = 1;
            SceneManager.LoadSceneAsync(scene.path, LoadSceneMode.Single);
        }

        void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            if (!box) return;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1, .15f, .1f, .12f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(1, .25f, .1f, .8f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
