using UnityEngine;
using UHFPS.Runtime;
namespace Blockout.Discovery
{
    public class FacilityPanelUse : MonoBehaviour,IInteractStart,IInteractTitle
    {
        public FuseboxPuzzle Panel;
        public Transform Cover;
        public float OpenAngle=110;
        public float CoverSpeed=180;
        public bool IsOpen {get;private set;}
        Quaternion closed;
        void Awake(){if(Cover)closed=Cover.localRotation;}
        void Update(){if(Cover)Cover.localRotation=Quaternion.RotateTowards(Cover.localRotation,closed*Quaternion.Euler(0,IsOpen?OpenAngle:0,0),CoverSpeed*Time.deltaTime);}
        public void InteractStart()
        {
            if(!Panel)return;
            if(!IsOpen){IsOpen=true;DiscoveryLoop.Hint("Find the three fuses. Use this panel to insert them as you return.");DesignSession.Record("panel_opened",name,transform.position);return;}
            if(Panel.FusesConnected){DiscoveryLoop.Hint("Facility power restored.");return;}
            if(!Inventory.HasReference||Inventory.Instance.GetInventoryItem(Panel.FuseItem)==null){DiscoveryLoop.Hint("No fuse carried. Find a fuse in the facility and bring it here.");DesignSession.Record("panel_empty_attempt",name,transform.position);return;}
            int before=Panel.Fuses.FindAll(f=>f.IsInserted).Count;
            Panel.UseInteract=true;Panel.InteractStart();
            int after=Panel.Fuses.FindAll(f=>f.IsInserted).Count;
            for(int i=before;i<after;i++)DesignSession.Record("fuse_returned",name,transform.position,i);
        }
        public TitleParams InteractTitle()=>new TitleParams{title="FACILITY ELECTRICAL PANEL",button1=!IsOpen?"Open panel cover":Panel&&Panel.FusesConnected?"Power restored":"Insert carried fuse"};
    }
}
