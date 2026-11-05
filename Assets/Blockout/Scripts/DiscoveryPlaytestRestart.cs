using UnityEngine;
using UnityEngine.SceneManagement;
using UHFPS.Runtime;

namespace Blockout.Discovery
{
    [DefaultExecutionOrder(10000)]
    public class DiscoveryPlaytestRestart : MonoBehaviour
    {
        public DiscoveryLoop Loop;
        public bool Completed => Loop && Loop.Returned >= Loop.RequiredFuses;
        public bool IsOpen { get; private set; }
        bool offered, restarting, frozen;
        CursorLockMode previousLock;
        bool previousCursor;
        GUIStyle titleStyle, bodyStyle, buttonStyle;

        void Start() { if (!Loop) Loop = FindAnyObjectByType<DiscoveryLoop>(); }
        void Update()
        {
            if (!Completed || restarting || VirgoTerminal.Active || (GameManager.HasReference && GameManager.Instance.IsPaused)) return;
            if (!offered) { offered = true; Open(); }
        }
        void LateUpdate()
        {
            if (IsOpen) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }
        public void Open()
        {
            if (!Completed || IsOpen || restarting) return;
            previousLock = Cursor.lockState; previousCursor = Cursor.visible;
            IsOpen = true;
            if (GameManager.HasReference) { GameManager.Instance.FreezePlayer(true, true); frozen = true; }
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            DesignSession.Record("playtest_complete", name, transform.position, Loop.Returned);
        }
        public void ContinueExploring() { Release(); }
        public void RestartPlaytest()
        {
            if (!Completed || restarting) return;
            restarting = true;
            DesignSession.Record("level_restart_completed", name, transform.position, Loop.Returned);
            Release();
            SaveGameManager.ClearLoadType(); SaveGameManager.ClearLoadName();
            Time.timeScale = 1;
            SceneManager.LoadSceneAsync(gameObject.scene.path, LoadSceneMode.Single);
        }
        void Release()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (frozen && GameManager.HasReference) GameManager.Instance.FreezePlayer(false);
            frozen = false;
            Cursor.lockState = previousLock; Cursor.visible = previousCursor;
        }
        void OnDisable() { Release(); }
        void OnGUI()
        {
            if (!IsOpen) return;
            titleStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            bodyStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            buttonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 18 };
            float width = Mathf.Min(460, Screen.width - 24);
            var panel = new Rect((Screen.width - width) / 2, (Screen.height - 220) / 2, width, 220);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x, panel.y + 16, width, 42), "POWER RESTORED", titleStyle);
            GUI.Label(new Rect(panel.x, panel.y + 60, width, 30), "All 3 fuses returned. Playtest complete.", bodyStyle);
            if (GUI.Button(new Rect(panel.x + 24, panel.y + 105, width - 48, 44), "Restart Playtest", buttonStyle)) RestartPlaytest();
            if (GUI.Button(new Rect(panel.x + 24, panel.y + 159, width - 48, 38), "Keep Exploring", buttonStyle)) ContinueExploring();
        }
    }
}
