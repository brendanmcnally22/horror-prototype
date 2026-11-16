using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UHFPS.Runtime;
namespace Blockout.Discovery
{
    public sealed class SecurityCameraPanel : MonoBehaviour,IInteractStart,IInteractTitle
    {
        [Serializable]public class Channel {public string Label;public CCTV_Camera Camera;public SecurityPanelDoor Door;}
        public Channel[] Channels=Array.Empty<Channel>();
        public Vector2Int FeedResolution=new Vector2Int(960,540);
        public bool IsOpen {get;private set;}
        public int SelectedChannel {get;private set;}
        public RenderTexture Feed {get;private set;}
        public string Message {get;private set;}="Select a camera and operate its door.";
        CursorLockMode oldCursor;bool oldVisible,hadItem;PlayerManager player;
        GUIStyle heading,label,button;
        void Start(){foreach(var c in Channels)if(c.Camera&&c.Camera.LiveCamera)c.Camera.LiveCamera.enabled=false;}
        public TitleParams InteractTitle()=>new TitleParams{title="SECURITY CONTROL PANEL",button1="Use cameras / operate security doors"};
        public void InteractStart()=>Open();
        public bool Open()
        {
            if(IsOpen||!GameManager.HasReference||GameManager.Instance.IsPaused||!GameManager.Instance.PlayerPresence.IsUnlockedAndCamera||Channels.Length==0)return false;
            player=PlayerManager.Instance;oldCursor=Cursor.lockState;oldVisible=Cursor.visible;hadItem=player.PlayerItems.IsAnyEquipped;
            player.PlayerItems.DeactivateCurrentItem();GameManager.Instance.FreezePlayer(true,true);
            IsOpen=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            if(!Feed){Feed=new RenderTexture(Mathf.Clamp(FeedResolution.x,256,1920),Mathf.Clamp(FeedResolution.y,144,1080),24){name="Security panel live feed"};Feed.Create();}
            SelectChannel(SelectedChannel);return true;
        }
        public void SelectChannel(int index)
        {
            if(index<0||index>=Channels.Length)return;
            SelectedChannel=index;
            for(int i=0;i<Channels.Length;i++)if(Channels[i].Camera&&Channels[i].Camera.LiveCamera)
            {var camera=Channels[i].Camera.LiveCamera;camera.enabled=IsOpen&&i==index;camera.targetTexture=camera.enabled?Feed:null;}
            Message="Viewing "+Channels[index].Label;
        }
        public bool OperateDoor(bool open)
        {
            if(!IsOpen||SelectedChannel>=Channels.Length)return false;
            var door=Channels[SelectedChannel].Door;if(!door){Message="No door assigned to this camera.";return false;}
            bool result=door.Request(open);Message=door.LastMessage;return result;
        }
        public void Close()
        {
            if(!IsOpen)return;IsOpen=false;
            foreach(var c in Channels)if(c.Camera&&c.Camera.LiveCamera){c.Camera.LiveCamera.enabled=false;c.Camera.LiveCamera.targetTexture=null;}
            if(GameManager.HasReference)GameManager.Instance.FreezePlayer(false);
            if(player&&hadItem)player.PlayerItems.ActivatePreviouslyDeactivatedItem();player=null;
            Cursor.lockState=oldCursor;Cursor.visible=oldVisible;
        }
        void Update()
        {
            if(!IsOpen)return;
            if(!player||(player.GetComponent<PlayerHealth>()&&player.GetComponent<PlayerHealth>().IsDead)){Close();return;}
            if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)Close();
        }
        void OnGUI()
        {
            if(!IsOpen)return;
            if(label==null){label=new GUIStyle(GUI.skin.label){fontSize=17,wordWrap=true};label.normal.textColor=Color.white;heading=new GUIStyle(label){fontSize=26,fontStyle=FontStyle.Bold};button=new GUIStyle(GUI.skin.button){fontSize=17};}
            var matrix=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/800f,1));GUI.depth=-500;
            var color=GUI.color;GUI.color=new Color(.035f,.05f,.065f,.98f);GUI.DrawTexture(new Rect(0,0,1280,800),Texture2D.whiteTexture);GUI.color=color;
            GUI.Label(new Rect(30,20,960,40),"DEAD AIR / SECURITY CONTROL",heading);
            if(GUI.Button(new Rect(1080,20,170,40),"EXIT  [ESC]",button))Close();
            for(int i=0;i<Channels.Length;i++)if(GUI.Button(new Rect(30,90+i*70,230,55),(i==SelectedChannel?"● ":"")+Channels[i].Label,button))SelectChannel(i);
            if(Feed)GUI.DrawTexture(new Rect(285,90,960,540),Feed,ScaleMode.ScaleToFit);
            var selected=Channels[SelectedChannel];var door=selected.Door;
            GUI.Label(new Rect(290,640,950,35),selected.Label+"  /  "+(door?door.DoorLabel+"  —  "+door.Status:"NO DOOR"),heading);
            GUI.Label(new Rect(30,350,235,160),door&&door.RequiresKeycard?"KEYCARD ACCESS\n"+door.CardLabel+"\n"+(door.HasAccess?"AUTHORIZED":"CARD REQUIRED"):"STANDARD ACCESS\nNo keycard required.",label);
            if(GUI.Button(new Rect(290,690,200,45),"OPEN DOOR",button))OperateDoor(true);
            if(GUI.Button(new Rect(510,690,200,45),"CLOSE DOOR",button))OperateDoor(false);
            GUI.Label(new Rect(735,685,510,85),Message,label);GUI.matrix=matrix;
        }
        void OnDisable()=>Close();
        void OnDestroy(){Close();if(Feed){Feed.Release();Destroy(Feed);}}
    }
}
