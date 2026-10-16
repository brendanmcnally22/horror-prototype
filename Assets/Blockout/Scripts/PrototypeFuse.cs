using UnityEngine;
using UHFPS.Runtime;

namespace Blockout.Discovery
{
    public class PrototypeFuse : MonoBehaviour, IInteractStart, IInteractTitle
    {
        public DiscoveryLoop Loop;
        public string Label = "Facility fuse";
        public void InteractStart()
        {
            if (!Loop) Loop = FindAnyObjectByType<DiscoveryLoop>();
            if (Loop && Loop.Collect(this)) gameObject.SetActive(false);
        }
        public TitleParams InteractTitle() => new TitleParams { title = Label, button1 = "Take fuse (optional)" };
    }
}
