using System;
using System.Linq;
using UnityEngine;
namespace Blockout.Discovery
{
    public partial class VirgoTerminal
    {
        readonly bool[] maximized=new bool[5];
        readonly Rect[] restore=new Rect[5];
        readonly Vector2[] minimum={new Vector2(510,320),new Vector2(650,430),new Vector2(570,400),new Vector2(510,390),new Vector2(720,548)};
        int sizing=-1,folder=-1,site=-1;
        string search="";
        [Tooltip("Optional images for Canteen, Transit and Rabbit Club, in that order.")]
        public Texture2D[] GoogusSiteImages=new Texture2D[3];
        static readonly string[] sites={"Night Shift Canteen","Cross-Facility Transit","Carrot Club"};
        static readonly string[] urls={"canteen","transit","carrotclub"};
        static readonly string[] headlines={"MIDNIGHT SERVICE","SHUTTLE BULLETIN","THE BURROW BOARD"};
        static readonly string[] copy={"Tonight: tomato soup, rye toast and two sugars per coffee. Bring your own mug.\n\nFresh bread arrives at the east hatch after shift change.","Last departure: Platform B at 02:40. Show your staff pass before boarding.\n\nLost property: one umbrella and a lunchbox marked DO NOT FREEZE.","Rabbit of the week: Turnip, undefeated at chewing network cables.\n\nClub tip: short hops keep you clear of the greenhouse gates."};
        static readonly string[] folders={"Reception","Staff","Director"};
        void ResizeEvents(int i,Event e){
            if(!GUI.enabled||maximized[i])return;
            var f=frames[i];if(e.type==EventType.MouseDown&&e.button==0&&new Rect(f.xMax-22,f.yMax-22,22,22).Contains(e.mousePosition)){sizing=i;e.Use();}
            if(sizing==i&&e.type==EventType.MouseDrag){f.width=Mathf.Clamp(e.mousePosition.x-f.x,minimum[i].x,1024-f.x);f.height=Mathf.Clamp(e.mousePosition.y-f.y,minimum[i].y,724-f.y);frames[i]=f;e.Use();}
            if(e.rawType==EventType.MouseUp)sizing=-1;
        }
        void Maximize(int i){if(maximized[i])frames[i]=restore[i];else{restore[i]=frames[i];frames[i]=new Rect(0,0,1024,724);}maximized[i]=!maximized[i];}
        void WarningAd(){
            Fill(new Rect(247,194,530,312),new Color(.66f,.04f,.02f));Fill(new Rect(252,199,520,302),Color.white);
            var red=new GUIStyle(heading);red.normal.textColor=new Color(.8f,.045f,.02f);
            GUI.Label(new Rect(281,229,460,50),"[ ! ]   WARNING!",red);
            GUI.Label(new Rect(283,291,460,40),Ads[adIndex],red);
            GUI.Label(new Rect(283,347,455,100),adIndex==3?"Googus Cache Guard detected two suspicious carrot cookies. Contact the imaginary help desk at EXT. BUN.":"Your archive may be haunted by obsolete floppy disks. VIRGO Disk Doctor recommends a coffee break.",text);
        }
        void Explorer(float w,float h){
            string path="C:\\Records"+(folder<0?"":"\\"+folders[folder]);
            GUI.Label(new Rect(10,25,w-20,22),"File     Edit     View     Help",label);
            if(Button(new Rect(10,51,68,28),"Up")){if(selected>=0)selected=-1;else folder=-1;listScroll=recordScroll=Vector2.zero;}
            Fill(new Rect(85,51,w-97,28),Color.white);GUI.Label(new Rect(91,55,w-110,22),path,label);
            var pane=new Rect(10,86,w-20,h-118);Fill(pane,Color.white);
            if(selected>=0&&CanRead(Archive.Records[selected])){
                var r=Archive.Records[selected];string body=r.Title+"\n\n"+r.Body;float ch=Mathf.Max(pane.height,text.CalcHeight(new GUIContent(body),pane.width-44)+20);
                recordScroll=GUI.BeginScrollView(pane,recordScroll,new Rect(0,0,pane.width-20,ch));GUI.Label(new Rect(10,8,pane.width-44,ch),body,text);GUI.EndScrollView();
            }else{
                int cols=Mathf.Max(1,(int)((pane.width-16)/120)),count=folder<0?Mathf.Clamp(Clearance+1,0,3):Archive.Records.Count(r=>CanRead(r)&&r.Clearance==folder);
                listScroll=GUI.BeginScrollView(pane,listScroll,new Rect(0,0,pane.width-18,Mathf.Max(pane.height,((count+cols-1)/cols)*94)));
                int n=0;
                for(int j=0;j<(folder<0?Mathf.Min(Clearance+1,3):Archive.Records.Length);j++){
                    bool dir=folder<0;if(!dir&&(!CanRead(Archive.Records[j])||Archive.Records[j].Clearance!=folder))continue;
                    float x=8+(n%cols)*120,y=8+n/cols*94;n++;
                    if(dir){Fill(new Rect(x+37,y+2,17,7),new Color(.82f,.64f,.12f));Fill(new Rect(x+37,y+8,40,24),new Color(.98f,.81f,.28f));}
                    else{Fill(new Rect(x+41,y+2,30,31),new Color(.81f,.84f,.86f));for(int k=0;k<3;k++)Fill(new Rect(x+45,y+10+k*5,19,1),Color.gray);}
                    var style=new GUIStyle(label){alignment=TextAnchor.UpperCenter};
                    GUI.Label(new Rect(x,y+37,112,48),dir?folders[j]:Archive.Records[j].Title+".txt",style);
                    if(GUI.Button(new Rect(x,y,112,85),GUIContent.none,GUIStyle.none)){if(dir)folder=j;else selected=j;recordScroll=listScroll=Vector2.zero;break;}
                }GUI.EndScrollView();
            }
            GUI.Label(new Rect(12,h-29,w-28,24),"Local disk (C:)   |   "+path+"   |   Read only",label);
        }
        void Googus(float w,float h){
            if(Button(new Rect(10,30,82,27),"Home")){site=-1;search="";}
            Fill(new Rect(100,30,w-111,27),Color.white);GUI.Label(new Rect(106,33,w-126,24),"googus://"+(site<0?"directory":urls[site]),label);
            Fill(new Rect(10,67,w-20,h-96),Color.white);
            if(site<0){
                var logo=new GUIStyle(heading){richText=true,fontSize=34};GUI.Label(new Rect(30,80,w-60,52),"<color=#246ccc>G</color><color=#c2382b>o</color><color=#ae7700>o</color><color=#246ccc>g</color><color=#238049>u</color><color=#c2382b>s</color>!",logo);GUI.Label(new Rect(30,134,w-60,25),"Your guide to the facility intranet",label);
                search=GUI.TextField(new Rect(30,172,w-60,27),search,60,field);int row=0;
                for(int i=0;i<sites.Length;i++)if((sites[i]+" "+copy[i]).IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0){if(Button(new Rect(30,211+row++*40,w-60,32),sites[i]))site=i;}
                if(row==0)GUI.Label(new Rect(30,214,w-60,40),"No local pages match your search.",label);
            }else{
                Fill(new Rect(26,126,w-52,4),site==0?new Color(.83f,.33f,.18f):site==1?new Color(.14f,.4f,.76f):new Color(.16f,.52f,.28f));GUI.Label(new Rect(26,84,w-52,40),headlines[site],heading);
                bool image=GoogusSiteImages!=null&&site<GoogusSiteImages.Length&&GoogusSiteImages[site];
                if(image)GUI.DrawTexture(new Rect(w*.63f,138,w*.32f,h-190),GoogusSiteImages[site],ScaleMode.ScaleToFit);
                GUI.Label(new Rect(28,141,image?w*.54f:w-56,h-192),copy[site],text);
            }GUI.Label(new Rect(12,h-26,w-25,22),"Googus Navigator   |   Local intranet",label);
        }
    }
}

