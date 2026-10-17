using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UHFPS.Runtime;

namespace Blockout.Discovery
{
    public class VirgoTerminal : MonoBehaviour, IInteractStart, IInteractTitle
    {
        public VirgoArchive Archive;
        public string StationName="RECEPTION-01";
        [Range(0,2)] public int Clearance;
        public bool PhoneOnly;
        [Range(0,1)] public float SoundVolume=.35f;
        [Tooltip("Optional authored replacements for the generated sounds.")]
        public AudioClip KeySound, ClickSound, BootSound, DialSound;
        public UnityEvent OnSignalDecoded=new UnityEvent();
        public UnityEvent OnCircuitSolved=new UnityEvent();
        public static VirgoTerminal Active { get; private set; }
        public bool IsOpen => Active==this;
        public bool SignalDecoded { get; private set; }
        public bool CircuitSolved => cells.All(c=>!c);
        public float Frequency=88f;
        public float SignalStrength => Archive ? Mathf.Clamp01(1-Mathf.Abs(Frequency-Archive.HiddenFrequency)/1.5f) : 0;
        readonly bool[] cells=new bool[16];
        readonly bool[] windows=new bool[4];
        Rect[] frames={new Rect(160,68,760,520),new Rect(190,98,650,430),new Rect(240,100,570,440),new Rect(270,120,560,430)};
        string[] names={"VIRGO Records","Line Monitor","Relay Break","Staff Telephone"};
        string query="", dial="", response="Dial a staff extension. Internal calls only.";
        int selected=-1, moves;
        bool menu, completed;
        float signalHold, openedAt;
        Vector2 recordScroll, listScroll;
        AudioSource audioSource, carrier;
        AudioClip generatedKey,generatedClick,generatedBoot,generatedDial,generatedCarrier;
        GUIStyle text,heading,button,label,field;
        CursorLockMode oldCursor; bool oldCursorVisible;
        bool frozen;
        Keyboard keyboard;
        int dragging=-1;
        void Awake()
        {
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=0;
            carrier=gameObject.AddComponent<AudioSource>();carrier.playOnAwake=false;carrier.loop=true;carrier.spatialBlend=0;
            generatedKey=Sound("Mechanical key",.045f,1900,.75f);
            generatedClick=Sound("Mouse switch",.025f,1150,.5f);
            generatedBoot=Sound("Virgo boot",.35f,660,.02f);
            generatedDial=Sound("Telephone key",.16f,697,.01f);
            generatedCarrier=Sound("Receiver",1,160,.75f,false); carrier.clip=generatedCarrier;
            ResetCircuit();
        }
        static AudioClip Sound(string name,float duration,float hz,float noise,bool decay=true)
        {
            const int rate=22050;var data=new float[(int)(rate*duration)];var random=new System.Random(73);
            for(int i=0;i<data.Length;i++){float t=(float)i/rate;float envelope=decay?Mathf.Exp(-t*12/duration):.22f;
                data[i]=((float)(random.NextDouble()*2-1)*noise+Mathf.Sin(t*hz*2*Mathf.PI)*(1-noise))*envelope*.5f;}
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        void Play(AudioClip authored,AudioClip fallback,float pitch=1)
        {if(!audioSource)return;audioSource.pitch=pitch;audioSource.PlayOneShot(authored?authored:fallback,SoundVolume);}
        public TitleParams InteractTitle()=>new TitleParams{title=PhoneOnly?"Virgo staff telephone":"Virgo workstation",button1=PhoneOnly?"Use telephone":"Use computer"};
        public void InteractStart()=>Open();
        public void Open()
        {
            if(Active || !Archive)return;
            Active=this;openedAt=Time.unscaledTime;
            oldCursor=Cursor.lockState;oldCursorVisible=Cursor.visible;
            if(GameManager.HasReference){GameManager.Instance.FreezePlayer(true,true);frozen=true;}
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            keyboard=Keyboard.current;if(keyboard!=null)keyboard.onTextInput+=Typed;
            if(PhoneOnly)OpenApplication(3);
            Play(BootSound,generatedBoot);
        }
        public void Close()
        {
            if(!IsOpen)return;
            if(keyboard!=null)keyboard.onTextInput-=Typed;keyboard=null;
            carrier.Stop();Active=null;
            if(frozen&&GameManager.HasReference)GameManager.Instance.FreezePlayer(false);
            frozen=false;Cursor.lockState=oldCursor;Cursor.visible=oldCursorVisible;
        }
        void Typed(char c){if(IsOpen&&!char.IsControl(c))Play(KeySound,generatedKey,UnityEngine.Random.Range(.9f,1.12f));}
        void Update()
        {
            if(!IsOpen)return;
            if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame&&Time.unscaledTime-openedAt>.25f){Close();return;}
            if(Keyboard.current!=null&&Keyboard.current.backspaceKey.wasPressedThisFrame)Play(KeySound,generatedKey);
            if(windows[1])
            {
                signalHold=SignalStrength>.97f ? Mathf.Min(2,signalHold+Time.unscaledDeltaTime) : 0;
                carrier.volume=SoundVolume*.12f*(1-SignalStrength*.8f);carrier.pitch=.8f+SignalStrength*.5f;
                if(!carrier.isPlaying)carrier.Play();
            }
            else {carrier.Stop();signalHold=0;}
        }
        void OnDisable(){Close();}
        void OnDestroy()
        {
            Close();foreach(var clip in new[]{generatedKey,generatedClick,generatedBoot,generatedDial,generatedCarrier})if(clip)Destroy(clip);
        }
        public bool CanRead(VirgoArchive.Record record)=>record!=null&&record.Clearance<=Clearance;
        public void OpenApplication(int index){if(index>=0&&index<windows.Length){Array.Clear(windows,0,windows.Length);windows[index]=true;}}
        public bool TryDecode()
        {
            if(SignalStrength<=.97f||signalHold<2)return false;
            if(!SignalDecoded){SignalDecoded=true;OnSignalDecoded.Invoke();Play(BootSound,generatedBoot);}
            return true;
        }
        public void ToggleCell(int index)
        {
            if(index<0||index>=16)return;
            int x=index%4,y=index/4;
            foreach(var delta in new[]{Vector2Int.zero,Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})
            {int nx=x+delta.x,ny=y+delta.y;if(nx>=0&&nx<4&&ny>=0&&ny<4)cells[ny*4+nx]=!cells[ny*4+nx];}
            moves++;
            if(CircuitSolved&&!completed){completed=true;OnCircuitSolved.Invoke();}
        }
        public void ResetCircuit()
        {
            Array.Clear(cells,0,cells.Length);foreach(int i in new[]{0,3,5,10,12,15})ToggleCell(i);moves=0;completed=false;
        }
        public string Dial(string number)
        {
            var contact=Archive.Extensions.FirstOrDefault(e=>e.Number==number);
            response=contact==null?"NO CONNECTION\nThe extension is not assigned.":contact.Department+"\n\n"+contact.Transcript;
            Play(DialSound,generatedDial);return response;
        }
        bool Button(Rect rect,string caption)
        {
            if(!GUI.Button(rect,caption,button))return false;
            Play(ClickSound,generatedClick);return true;
        }
        static void Fill(Rect rect,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
        void Styles()
        {
            if(text!=null)return;
            text=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true,richText=false};text.normal.textColor=new Color(.08f,.1f,.12f);
            label=new GUIStyle(text){fontSize=14};heading=new GUIStyle(text){fontSize=24,fontStyle=FontStyle.Bold};
            button=new GUIStyle(GUI.skin.button){fontSize=15,wordWrap=true};
            field=new GUIStyle(GUI.skin.textField){fontSize=16};
        }
        void OnGUI()
        {
            if(!IsOpen)return;
            GUI.depth=0;Styles();var oldMatrix=GUI.matrix;
            float scale=Mathf.Min(Screen.width/1024f,Screen.height/768f);
            Fill(new Rect(0,0,Screen.width,Screen.height),Color.black);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1024*scale)/2,(Screen.height-768*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            Fill(new Rect(0,0,1024,768),new Color(.04f,.29f,.3f));
            GUI.color=new Color(.48f,.75f,.7f);GUI.Label(new Rect(570,615,420,40),"V I R G O   /   SYSTEMS",new GUIStyle(heading){fontSize=30});GUI.color=Color.white;
            GUI.Label(new Rect(580,657,420,25),"INTERNAL NETWORK    •    "+StationName);
            if(!PhoneOnly)for(int i=0;i<4;i++)if(Button(new Rect(22,28+i*92,118,72),new[]{"[=]\nRecords","[~]\nLine Monitor","[+]\nRelay Break","[T]\nTelephone"}[i]))OpenApplication(i);
            for(int i=0;i<4;i++)if(windows[i])
            {
                var ev=Event.current;
                if(ev.type==EventType.MouseDown&&ev.button==0&&new Rect(frames[i].x,frames[i].y,frames[i].width-36,22).Contains(ev.mousePosition)){dragging=i;ev.Use();}
                if(dragging==i&&ev.type==EventType.MouseDrag){frames[i].position+=ev.delta;ev.Use();}
                if(ev.type==EventType.MouseUp)dragging=-1;
                frames[i].x=Mathf.Clamp(frames[i].x,0,1024-frames[i].width);frames[i].y=Mathf.Clamp(frames[i].y,0,724-frames[i].height);
                GUI.BeginGroup(frames[i]);DrawWindow(7900+i);GUI.EndGroup();
            }
            Fill(new Rect(0,726,1024,42),new Color(.72f,.73f,.7f));
            if(Button(new Rect(8,732,100,29),"V  Start"))menu=!menu;
            if(Button(new Rect(864,732,152,29),"Leave workstation"))Close();
            GUI.Label(new Rect(125,736,720,24),"VIRGO OS 95    |    "+(Clearance==0?"RECEPTION":Clearance==1?"STAFF":"DIRECTOR")+"    |    02:13 AM",label);
            if(menu)
            {
                Fill(new Rect(8,530,180,194),new Color(.75f,.76f,.73f));
                for(int i=0;i<4;i++)if(Button(new Rect(14,536+i*38,168,32),names[i])){OpenApplication(i);menu=false;}
                GUI.Label(new Rect(18,691,165,23),"Esc: leave computer",label);
            }
            GUI.matrix=oldMatrix;
        }
        void DrawWindow(int id)
        {
            int index=id-7900;float width=frames[index].width;
            Fill(new Rect(3,2,width-6,18),new Color(.06f,.12f,.36f));
            GUI.Label(new Rect(10,1,width-48,20),names[index]);
            Fill(new Rect(4,20,width-8,frames[index].height-24),new Color(.79f,.8f,.76f));
            if(Button(new Rect(width-32,1,26,18),"X"))windows[index]=false;
            if(index==0)Records();if(index==1)Receiver();if(index==2)Game();if(index==3)Telephone();
        }
        void Records()
        {
            GUI.Label(new Rect(18,32,700,26),"CORPORATE ARCHIVE / indexed local copy",heading);
            GUI.Label(new Rect(18,76,64,25),"Search",label);query=GUI.TextField(new Rect(80,73,650,28),query,80,field);
            listScroll=GUI.BeginScrollView(new Rect(16,112,240,370),listScroll,new Rect(0,0,215,Mathf.Max(350,Archive.Records.Length*54)));
            int row=0;
            for(int i=0;i<Archive.Records.Length;i++)
            {
                var record=Archive.Records[i];if(!CanRead(record))continue;
                if(!record.Title.Contains(query,StringComparison.OrdinalIgnoreCase)&&!record.Body.Contains(query,StringComparison.OrdinalIgnoreCase))continue;
                if(Button(new Rect(0,row++*54,210,48),record.Title)){selected=i;recordScroll=Vector2.zero;}
            }
            GUI.EndScrollView();
            Fill(new Rect(270,112,464,370),new Color(.94f,.94f,.87f));
            string body=selected>=0&&CanRead(Archive.Records[selected]) ? Archive.Records[selected].Title+"\n\n"+Archive.Records[selected].Body : "VIRGO CORPORATION\n\nSelect a record.\n\nSome archives are available only at staff or director terminals.\n\nNo external network connection.";
            float height=text.CalcHeight(new GUIContent(body),418);
            recordScroll=GUI.BeginScrollView(new Rect(282,124,440,346),recordScroll,new Rect(0,0,418,Mathf.Max(330,height)));
            GUI.Label(new Rect(0,0,418,Mathf.Max(330,height)),body,text);GUI.EndScrollView();
            GUI.Label(new Rect(18,489,710,24),"Controlled copy  /  authorization: "+Clearance+"  /  local records only",label);
        }
        void Receiver()
        {
            GUI.Label(new Rect(20,38,600,32),"VIRGO LINE MONITOR",heading);
            GUI.Label(new Rect(20,83,600,45),"Tune slowly. Hold a clean carrier for two seconds, then decode.\nMaintenance posted the carrier frequency in the archive.",text);
            GUI.Label(new Rect(24,141,260,40),Frequency.ToString("0.00")+" MHz",heading);
            Frequency=GUI.HorizontalSlider(new Rect(24,195,580,24),Frequency,88,108);
            if(Button(new Rect(24,222,90,30),"- 0.05"))Frequency=Mathf.Max(88,Frequency-.05f);
            if(Button(new Rect(125,222,90,30),"+ 0.05"))Frequency=Mathf.Min(108,Frequency+.05f);
            Fill(new Rect(240,224,364,24),new Color(.12f,.15f,.13f));Fill(new Rect(240,224,364*SignalStrength,24),new Color(.35f,.7f,.3f));
            GUI.enabled=signalHold>=2;if(Button(new Rect(24,271,160,34),"Decode carrier"))TryDecode();GUI.enabled=true;
            GUI.Label(new Rect(200,277,400,28),signalHold>=2?"CARRIER LOCKED":"Lock: "+(signalHold/2*100).ToString("0")+"%",label);
            GUI.Label(new Rect(24,320,580,95),SignalDecoded?Archive.DecodedMessage:"[ static / no authenticated message ]",text);
        }
        void Game()
        {
            GUI.Label(new Rect(20,38,530,35),"RELAY BREAK  /  Staff recreation",heading);
            GUI.Label(new Rect(20,81,530,50),"Turn every relay OFF. Each click flips a relay and its neighbours. No time limit.",text);
            for(int i=0;i<16;i++)
            {
                GUI.backgroundColor=cells[i]?new Color(1,.65f,.18f):new Color(.25f,.35f,.32f);
                if(Button(new Rect(22+i%4*61,145+i/4*61,55,55),cells[i]?"ON":"OFF"))ToggleCell(i);
            }
            GUI.backgroundColor=Color.white;
            GUI.Label(new Rect(295,148,250,95),CircuitSolved?"SHIFT COMPLETE\nAll relays isolated.":"Moves: "+moves+"\n\nApproved for break periods only.",text);
            if(Button(new Rect(295,265,225,35),"New shift / reset"))ResetCircuit();
            GUI.Label(new Rect(295,324,240,75),"Facilities asks that you stop submitting game scores as maintenance reports.",label);
        }
        void Telephone()
        {
            GUI.Label(new Rect(20,35,520,30),"VIRGO INTERNAL EXCHANGE",heading);
            GUI.Label(new Rect(20,80,245,160),string.Join("\n",Archive.Extensions.Select(e=>e.Number+"  "+e.Department)),text);
            dial=GUI.TextField(new Rect(290,80,240,30),dial,6,field);
            for(int i=0;i<12;i++){string key="123456789*0#"[i].ToString();if(Button(new Rect(290+i%3*80,120+i/3*43,73,37),key)&&dial.Length<6)dial+=key;}
            if(Button(new Rect(290,298,114,32),"Call"))Dial(dial);
            if(Button(new Rect(415,298,114,32),"Clear")){dial="";response="Line cleared.";}
            GUI.Label(new Rect(20,255,250,155),response,text);
            GUI.Label(new Rect(290,350,240,65),"INTERNAL LINES ONLY\nUse the staff directory for department extensions.",label);
        }
    }
}
