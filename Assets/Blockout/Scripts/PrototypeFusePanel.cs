using UnityEngine;
using UHFPS.Runtime;

namespace Blockout.Discovery
{
    public class PrototypeFusePanel : MonoBehaviour, IInteractStart, IInteractTitle
    {
        public DiscoveryLoop Loop;
        public void InteractStart() { if (Loop) Loop.ReturnOne(); }
        public TitleParams InteractTitle() => new TitleParams {
            title = Loop ? "Return panel | " + Loop.Returned + "/" + Loop.RequiredFuses + " | Carrying " + Loop.Carried : "Unconnected return panel",
            button1 = "Insert one fuse"
        };
    }
}
