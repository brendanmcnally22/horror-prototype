using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UHFPS.Runtime;
using Blockout.Discovery;

namespace DeadAir.Playtesting
{
    public class SecurityTrialTerminal : MonoBehaviour,IInteractStart,IInteractTitle
    {
        public Camera[] CameraFeeds=new Camera[0];
        public PrototypeMover[] RemoteDoors=new PrototypeMover[0];
        public Light[] RemoteLights=new Light[0];
        public static SecurityTrialTerminal Active {get;private set;}
        public bool IsOpen=>Active==this;
        bool desktopSession;
        Vector2 cameraScroll;
        int selected;
        RenderTexture feed;
        RenderTexture previousTarget;
        bool previousEnabled;
        CursorLockMode previousCursor;bool previousVisible;
        Camera current;
        readonly Dictionary<Camera,(bool enabled,RenderTexture target)> cameraStates=new Dictionary<Camera,(bool,RenderTexture)>();
        GUIStyle text,button;
        public TitleParams InteractTitle()=>new TitleParams{title="Security terminal",button1="Operate security system"};
        public void InteractStart()=>Open();
        public void Open()
        {
            var desktop=GetComponent<VirgoTerminal>();
            if(desktop&&desktop.Archive&&!desktop.PhoneOnly){desktop.Open();if(desktop.IsOpen)desktop.OpenApplication(4);return;}
            if(Active||VirgoTerminal.Active)return;Active=this;
            desktopSession=false;
            previousCursor=Cursor.lockState;previousVisible=Cursor.visible;
            if(GameManager.HasReference)GameManager.Instance.FreezePlayer(true,true);
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            StartFeeds();
        }
        public void OpenDesktop()
        {
            if(Active||!GetComponent<VirgoTerminal>()||VirgoTerminal.Active!=GetComponent<VirgoTerminal>())return;
            Active=this;desktopSession=true;StartFeeds();
        }
        void StartFeeds()
        {
            cameraStates.Clear();
            foreach(var camera in CameraFeeds)if(camera&&!cameraStates.ContainsKey(camera)){cameraStates.Add(camera,(camera.enabled,camera.targetTexture));camera.enabled=false;}
            SelectFeed(selected);DesignSession.Record("security_terminal_opened",name,transform.position);
        }
        public void Close()
        {
            if(!IsOpen)return;ReleaseCamera();
            foreach(var entry in cameraStates)if(entry.Key){entry.Key.enabled=entry.Value.enabled;entry.Key.targetTexture=entry.Value.target;}
            cameraStates.Clear();Active=null;
            if(!desktopSession)
            {
                if(GameManager.HasReference)GameManager.Instance.FreezePlayer(false);
                Cursor.lockState=previousCursor;Cursor.visible=previousVisible;
            }
            desktopSession=false;
            DesignSession.Record("security_terminal_closed",name,transform.position);
        }
        void Update(){if(IsOpen&&!desktopSession&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)Close();}
        void LateUpdate(){if(IsOpen){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}}
        void SelectFeed(int index)
        {
            ReleaseCamera();if(CameraFeeds.Length==0)return;selected=Mathf.Clamp(index,0,CameraFeeds.Length-1);
            current=CameraFeeds[selected];if(!current)return;
            previousTarget=current.targetTexture;previousEnabled=current.enabled;
            feed=new RenderTexture(640,360,24);feed.Create();current.targetTexture=feed;current.enabled=true;
            DesignSession.Record("security_camera_selected",current.name,transform.position,selected);
        }
        void ReleaseCamera()
        {
            if(current){current.targetTexture=previousTarget;current.enabled=previousEnabled;}current=null;
            if(feed){feed.Release();Destroy(feed);feed=null;}
        }
        void OnDisable(){Close();}void OnDestroy(){ReleaseCamera();}
        void OnGUI()
        {
            if(!IsOpen||desktopSession)return;
            text??=new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true};button??=new GUIStyle(GUI.skin.button){fontSize=17};
            var old=GUI.matrix;float scale=Mathf.Min(Screen.width/960f,Screen.height/640f);GUI.depth=-10;
            var color=GUI.color;GUI.color=new Color(.025f,.045f,.04f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=color;
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-960*scale)/2,(Screen.height-640*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            GUI.Label(new Rect(28,20,780,35),"VIRGO / SECURITY CONTROL",text);
            if(feed)GUI.DrawTexture(new Rect(28,72,640,360),feed,ScaleMode.ScaleToFit);
            else GUI.Box(new Rect(28,72,640,360),"NO CAMERA FEED\nAssign your cameras to Camera Feeds on this terminal.");
            GUI.BeginGroup(new Rect(690,72,245,450));
            for(int i=0;i<CameraFeeds.Length;i++)if(GUI.Button(new Rect(0,i*42,240,36),CameraFeeds[i]?CameraFeeds[i].name:"Unassigned camera",button))SelectFeed(i);
            GUI.EndGroup();
            int row=0;foreach(var door in RemoteDoors)if(door){if(GUI.Button(new Rect(28+(row%3)*216,448+(row/3)*42,208,36),door.name,button)){door.Toggle();DesignSession.Record("security_door_used",door.name,transform.position);}row++;}
            foreach(var lamp in RemoteLights)if(lamp){if(GUI.Button(new Rect(28+(row%3)*216,448+(row/3)*42,208,36),lamp.name,button)){lamp.enabled=!lamp.enabled;DesignSession.Record("security_light_used",lamp.name,transform.position);}row++;}
            if(GUI.Button(new Rect(28,585,230,36),"Leave terminal / Esc",button))Close();
            GUI.Label(new Rect(285,587,640,35),"The facility keeps running while you operate this terminal.",text);
            GUI.matrix=old;
        }
        public void DrawDesktop(Rect area)
        {
            if(!IsOpen)return;
            text??=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};
            text.normal.textColor=Color.black;
            button??=new GUIStyle(GUI.skin.button){fontSize=14};
            GUI.BeginGroup(area);
            float viewWidth=area.width-210;
            GUI.Label(new Rect(8,0,area.width-16,24),"VIRGO SECURITY / "+(current?current.name:"No feed selected"),text);
            if(feed)GUI.DrawTexture(new Rect(8,30,viewWidth,340),feed,ScaleMode.ScaleToFit);
            else GUI.Box(new Rect(8,30,viewWidth,340),"NO CAMERA FEED\nConnect the placed cameras in Camera Feeds.");
            cameraScroll=GUI.BeginScrollView(new Rect(area.width-192,30,184,340),cameraScroll,new Rect(0,0,164,Mathf.Max(330,CameraFeeds.Length*42)));
            for(int i=0;i<CameraFeeds.Length;i++)if(GUI.Button(new Rect(0,i*42,162,36),CameraFeeds[i]?CameraFeeds[i].name:"Unassigned camera",button))SelectFeed(i);
            GUI.EndScrollView();
            int row=0;
            foreach(var door in RemoteDoors)if(door){if(GUI.Button(new Rect(8+row%3*272,386+row/3*40,264,34),door.name,button)){door.Toggle();DesignSession.Record("security_door_used",door.name,transform.position);}row++;}
            foreach(var lamp in RemoteLights)if(lamp){if(GUI.Button(new Rect(8+row%3*272,386+row/3*40,264,34),lamp.name,button)){lamp.enabled=!lamp.enabled;DesignSession.Record("security_light_used",lamp.name,transform.position);}row++;}
            GUI.Label(new Rect(8,area.height-30,area.width-16,26),"LIVE / Facility remains active. Close this window to return to the desktop.",text);
            GUI.EndGroup();
        }
    }
}
