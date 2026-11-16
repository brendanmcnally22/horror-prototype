using UnityEngine;
using System.Collections.Generic;

namespace DeadAir.Playtesting
{
    public sealed class TrialCameraOperator : MonoBehaviour
    {
        SecurityTrialTerminal terminal;
        readonly Dictionary<Camera,Quaternion> homes=new Dictionary<Camera,Quaternion>();
        readonly Dictionary<Camera,Vector2> angles=new Dictionary<Camera,Vector2>();
        void Start(){
            terminal=GetComponent<SecurityTrialTerminal>();
            foreach(var c in terminal.CameraFeeds)if(c){homes[c]=c.transform.localRotation;angles[c]=Vector2.zero;}
            scouts=new TrialCameraHider[Mathf.Min(3,terminal.CameraFeeds.Length)];
            for(int i=0;i<scouts.Length;i++){var h=gameObject.AddComponent<TrialCameraHider>();h.Terminal=terminal;h.Home=terminal.CameraFeeds[i*terminal.CameraFeeds.Length/scouts.Length];h.Number=i+1;scouts[i]=h;}
        }
        TrialCameraHider[] scouts;
        public int Spots{get;private set;}
        public string SpotMessage{get;private set;}="";
        public void Spot(){
            if(!terminal.WatchedCamera||scouts==null)return;
            TrialCameraHider best=null;float distance=2;
            foreach(var scout in scouts){float d=scout.SpotDistance(terminal.WatchedCamera);if(d<distance){distance=d;best=scout;}}
            if(!best){SpotMessage="No new scout in sight.";return;}
            best.Mark();Spots++;SpotMessage="Scout "+best.Number+" spotted!";
            Blockout.Discovery.DesignSession.Record("scout_spotted","Scout "+best.Number,best.Feet,Spots);
        }
        public void Aim(Camera c,Vector2 delta,bool home=false){
            if(!c||!homes.ContainsKey(c))return;
            Vector2 a=home?Vector2.zero:angles[c]+delta;
            a.x=Mathf.Clamp(a.x,-95,95);a.y=Mathf.Clamp(a.y,-50,50);
            angles[c]=a;c.transform.localRotation=homes[c]*Quaternion.Euler(a.y,a.x,0);
        }
        public void Draw(Rect view){
            var c=terminal.WatchedCamera;if(!c||!GUI.enabled)return;
            int id=GUIUtility.GetControlID(FocusType.Passive);
            var e=Event.current;
            if(e.type==EventType.MouseDown&&e.button==0&&view.Contains(e.mousePosition)){GUIUtility.hotControl=id;e.Use();}
            else if(GUIUtility.hotControl==id&&e.type==EventType.MouseDrag){Aim(c,new Vector2(e.delta.x,e.delta.y)*.27f);e.Use();}
            else if(GUIUtility.hotControl==id&&e.type==EventType.MouseUp){GUIUtility.hotControl=0;e.Use();}
            if(e.type==EventType.ScrollWheel&&view.Contains(e.mousePosition)){c.fieldOfView=Mathf.Clamp(c.fieldOfView+e.delta.y*2,20,75);e.Use();}
            
        }
        void Update(){
            if(!terminal||!terminal.WatchedCamera)return;
            var desktop=GetComponent<Blockout.Discovery.VirgoTerminal>();
            if(desktop&&desktop.ModalAd)return;
            var k=UnityEngine.InputSystem.Keyboard.current;if(k==null)return;
            if(k.spaceKey.wasPressedThisFrame)terminal.NextFeed();
            int direction=(k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0);
            if(direction!=0)Aim(terminal.WatchedCamera,new Vector2(direction*32f*Time.deltaTime,0));
        }
        void OnDestroy(){foreach(var pair in homes)if(pair.Key)pair.Key.transform.localRotation=pair.Value;}
    }
}



