using UnityEngine;
using UnityEngine.InputSystem;

namespace Blockout.Discovery
{
    // Drawn in the workstation window's local coordinates. No gameplay input or physics.
    public sealed class BunnyBreak
    {
        const float X=90, H=240, W=520, Gap=94;
        float y=110,velocity,offset=340;int score,best;bool playing,crashed;
        public void Flap(){if(crashed||!playing){offset=340;y=110;score=0;crashed=false;playing=true;}velocity=-184;}
        static float Middle(int n)=>120+48*Mathf.Sin(n*2.37f+.3f);
        public void Tick(float dt){
            if(Keyboard.current!=null&&Keyboard.current.spaceKey.wasPressedThisFrame)Flap();
            if(!playing)return;
            while(dt>0&&playing){float d=Mathf.Min(dt,.016f);dt-=d;velocity+=460*d;y+=velocity*d;offset-=102*d;
                float mid=Middle(score);
                if(y<9||y>H-9||(offset<X+10&&offset+38>X-10&&(y-8<mid-Gap/2||y+8>mid+Gap/2))){playing=false;crashed=true;best=Mathf.Max(best,score);}
                else if(offset+38<X-10){score++;offset+=225;}
            }
        }
        static void Ink(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        public void Draw(){
            GUI.BeginGroup(new Rect(24,78,W,H));Ink(new Rect(0,0,W,H),new Color(.14f,.35f,.42f));
            for(int i=0;i<3;i++){float x=offset+i*225,mid=Middle(score+i);var green=new Color(.21f,.68f,.39f);
                Ink(new Rect(x,0,38,mid-Gap/2),green);Ink(new Rect(x,mid+Gap/2,38,H-mid-Gap/2),green);
                Ink(new Rect(x-4,mid-Gap/2-10,46,10),Color.green);Ink(new Rect(x-4,mid+Gap/2,46,10),Color.green);}
            Ink(new Rect(X-11,y-6,22,15),Color.white);Ink(new Rect(X+1,y-10,14,13),Color.white);
            Ink(new Rect(X+2,y-21,4,13),Color.white);Ink(new Rect(X+9,y-23,4,14),Color.white);
            Ink(new Rect(X+10,y-20,2,9),Color.magenta);Ink(new Rect(X+11,y-8,2,3),Color.black);Ink(new Rect(X-15,y-7,6,6),Color.white);
            GUI.Label(new Rect(12,8,250,25),"GATES  "+score+"     BEST  "+best);
            if(!playing)GUI.Box(new Rect(120,90,285,55),crashed?"BURROW CLOSED — click to retry":"BUNNY BREAK — click to start");
            if(GUI.enabled&&Event.current.type==EventType.MouseDown&&Event.current.button==0&&new Rect(0,0,W,H).Contains(Event.current.mousePosition)){Flap();Event.current.Use();}
            GUI.EndGroup();GUI.Label(new Rect(24,330,500,40),"Space or click to hop. Fly between the greenhouse gates.");
        }
    }
}

