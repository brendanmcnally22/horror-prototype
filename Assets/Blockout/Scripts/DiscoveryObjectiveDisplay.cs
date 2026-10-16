using TMPro;
using UnityEngine;

namespace Blockout.Discovery
{
    public class DiscoveryObjectiveDisplay : MonoBehaviour
    {
        public TMP_Text Text;
        public DiscoveryLoop Loop;
        [TextArea] public string CompletionText = "POWER RESTORED — Continue exploring";
        string objective;
        bool showProgress;
        public void Show(string message, bool progress)
        {
            objective = message; showProgress = progress;
            if (Text) Text.gameObject.SetActive(true);
            Refresh();
        }
        public void Hide() { if (Text) Text.gameObject.SetActive(false); }
        public void Refresh()
        {
            if (!Text || string.IsNullOrEmpty(objective)) return;
            if (!Loop) Loop = FindAnyObjectByType<DiscoveryLoop>();
            Text.text = showProgress && Loop
                ? (Loop.Returned >= Loop.RequiredFuses ? CompletionText : objective + "\n" + Loop.Returned + " / " + Loop.RequiredFuses + " fuses returned")
                : objective;
        }
        void Update() { if (Text && Text.gameObject.activeSelf) Refresh(); }
    }
}
