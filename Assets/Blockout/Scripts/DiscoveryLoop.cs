using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UHFPS.Runtime;

namespace Blockout.Discovery
{
    public class DiscoveryLoop : MonoBehaviour
    {
        [Min(1)] public int RequiredFuses = 3;
        [Tooltip("One by default to encourage return trips. Increase to allow carrying several.")]
        [Min(1)] public int CarryCapacity = 1;
        [Tooltip("Use the horror pack's real inventory fuses. Carry limit is then controlled by the pack inventory.")]
        public bool UsePackInventory;
        public string PackFuseId = "277b6aee2d4b47aaa655f284bf75bf94";
        public PrototypeLight[] FirstReturnLights = new PrototypeLight[0];
        public PrototypeMover[] SecondReturnShortcuts = new PrototypeMover[0];
        public PrototypeMover[] FinalReturnDoors = new PrototypeMover[0];
        public UnityEvent OnFirstReturned = new UnityEvent();
        public UnityEvent OnSecondReturned = new UnityEvent();
        public UnityEvent OnCompleted = new UnityEvent();
        public UnityEvent<int> OnReturned = new UnityEvent<int>();
        readonly HashSet<PrototypeFuse> collected = new HashSet<PrototypeFuse>();
        readonly HashSet<int> returnedSockets = new HashSet<int>();
        int carried;
        public int Carried => UsePackInventory ? (Inventory.HasReference ? Inventory.Instance.GetItemQuantity(PackFuseId) : 0) : carried;
        public int Returned { get; private set; }
        public bool Collect(PrototypeFuse fuse)
        {
            if (UsePackInventory || !fuse || collected.Contains(fuse)) return false;
            if (Carried >= CarryCapacity) { Hint("Carrying capacity reached. Return a fuse to the panel first."); return false; }
            collected.Add(fuse); carried++;
            Hint("Fuse collected. Return it when ready, or keep exploring."); return true;
        }
        public bool ReturnOne()
        {
            if (Returned >= RequiredFuses) { Hint("Power restored. You can keep exploring."); return false; }
            if (Carried == 0) { Hint("Find a fuse and use this panel to return it. Exploration is always optional."); return false; }
            if (UsePackInventory) Inventory.Instance.RemoveItem(PackFuseId, 1);
            else carried--;
            AwardReturn();
            return true;
        }
        // The pack fusebox has already consumed the inventory item when it emits this event.
        public void RecordPackFuse(int socket)
        {
            if (Returned >= RequiredFuses || !returnedSockets.Add(socket)) return;
            AwardReturn();
        }
        void AwardReturn()
        {
            Returned++;
            if (Returned == 1)
            {
                foreach (var light in FirstReturnLights) if (light) light.RestoreWhite();
                OnFirstReturned.Invoke();
            }
            if (Returned == 2)
            {
                foreach (var shortcut in SecondReturnShortcuts) if (shortcut) shortcut.Open();
                OnSecondReturned.Invoke();
            }
            if (Returned == RequiredFuses)
            {
                foreach (var door in FinalReturnDoors) if (door) door.Open();
                OnCompleted.Invoke();
            }
            OnReturned.Invoke(Returned);
            Hint("Fuses returned: " + Returned + "/" + RequiredFuses + "." + (Returned == RequiredFuses ? " Power restored. Keep exploring if you wish." : " Facility power increased."));
        }
        public static void Hint(string message) { if (GameManager.HasReference) GameManager.Instance.ShowHintMessage(message, 5); }
    }
}
