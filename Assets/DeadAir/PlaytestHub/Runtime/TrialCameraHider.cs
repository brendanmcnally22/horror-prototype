using System.Linq;
using UnityEngine;
namespace DeadAir.Playtesting
{
    // A short validated ground route keeps this test subject independent of the facility enemy/navmesh.
    public sealed class TrialCameraHider : MonoBehaviour
    {
        public SecurityTrialTerminal Terminal;
        public Camera Home;public int Number;
        GameObject subject,screen;
        bool marked;float unseenSince=-1,noticed=-1;bool towardCover;
        public Vector3 Feet=>subject?subject.transform.position-Vector3.up*(Height/2):Vector3.zero;
        public float SpotDistance(Camera c){
            if(marked||!subject||!Visible(c,Feet))return float.PositiveInfinity;
            var v=c.WorldToViewportPoint(Feet+Vector3.up*(Height*.5f));return Vector2.Distance(new Vector2(v.x,v.y),new Vector2(.5f,.5f));
        }
        public void Mark(){marked=true;hiding=true;wait=Time.time+2;}
        Vector3 exposed,hidden;
        int fallbackFeed;float wait,scan;bool hiding,evadeToExposed;
        const float Radius=.25f,Height=1.6f;
        const int World=~((1<<3)|(1<<5)|(1<<7)|(1<<13));
        bool Ground(Vector3 p,out Vector3 feet){
            if(Physics.Raycast(p+Vector3.up*1.2f,Vector3.down,out var hit,2.6f,World,QueryTriggerInteraction.Ignore)&&hit.normal.y>.87f){
                feet=hit.point+Vector3.up*.025f;return true;}
            feet=default;return false;
        }
        static bool Seen(Camera c,Vector3 feet){
            if(!c)return false;
            for(int i=0;i<2;i++){
                Vector3 point=feet+Vector3.up*(i==0?Height*.8f:Height*.4f),v=c.WorldToViewportPoint(point);
                if(v.z<c.nearClipPlane||v.z>c.farClipPlane||v.x<0||v.x>1||v.y<0||v.y>1)continue;
                if(!Physics.Linecast(c.transform.position,point,World,QueryTriggerInteraction.Ignore))return true;
            }
            return false;
        }
        bool Clear(Vector3 p)=>!Physics.CheckCapsule(p+Vector3.up*(Radius+.035f),p+Vector3.up*(Height-Radius),Radius,World,QueryTriggerInteraction.Ignore);
        bool Route(Vector3 a,Vector3 b){
            float length=Vector3.Distance(a,b);Vector3 last=a;
            for(int i=1;i<=Mathf.CeilToInt(length/.14f);i++){
                if(!Ground(Vector3.Lerp(a,b,Mathf.Min(1,i*.14f/length)),out var p)||Mathf.Abs(p.y-last.y)>.13f||!Clear(p))return false;
                last=p;
            }return true;
        }
        bool Place(Camera c){
            // Find walkable floor visible in this feed. Prefer existing occlusion; otherwise add a small test screen.
            Vector3 side=c.transform.right;side.y=0;side.Normalize();
            for(int row=0;row<7;row++)for(int col=0;col<9;col++){
                var ray=c.ViewportPointToRay(new Vector3(.5f+(col%2==0?1:-1)*((col+1)/2)*.09f,.14f+row*.075f));
                if(!Physics.Raycast(ray,out var hit,24,World,QueryTriggerInteraction.Ignore)||hit.normal.y<.87f)continue;
                Vector3 a=hit.point+Vector3.up*.025f;if(!Clear(a)||!Seen(c,a)||FindObjectsByType<TrialCameraHider>(FindObjectsSortMode.None).Any(h=>h!=this&&h.subject&&Vector3.Distance(h.Feet,a)<2))continue;
                foreach(int sign in new[]{1,-1}){
                    if(!Ground(a+side*sign*1.65f,out var b)||!Route(a,b))continue;
                    if(Seen(c,b)){
                        Vector3 towards=c.transform.position-b;towards.y=0;towards.Normalize();
                        Vector3 center=b+towards*.6f+Vector3.up*1.0f;
                        var rot=Quaternion.LookRotation(towards);
                        if(Physics.CheckBox(center,new Vector3(.42f,.95f,.08f),rot,World,QueryTriggerInteraction.Ignore))continue;
                        screen=GameObject.CreatePrimitive(PrimitiveType.Cube);screen.name="Camera test - cover screen";screen.transform.SetPositionAndRotation(center,rot);
                        screen.transform.localScale=new Vector3(.88f,2f,.16f);screen.transform.SetParent(transform,true);
                        // Raycast-only cover: no new barrier in the player's route.
                        screen.GetComponent<Collider>().isTrigger=true;
                        screen.GetComponent<Renderer>().material.color=new Color(.22f,.29f,.29f);
                        if(!Visible(c,a)||Visible(c,b)){screen.SetActive(false);Destroy(screen);screen=null;continue;}
                    }
                    exposed=a;hidden=b;subject=GameObject.CreatePrimitive(PrimitiveType.Capsule);subject.name=Number==1?"Camera-shy scout":"Camera-shy scout "+Number;
                    subject.GetComponent<Collider>().enabled=false;subject.transform.SetParent(transform,true);subject.transform.localScale=new Vector3(.36f,.39f,.36f);
                    subject.transform.position=a+Vector3.up*(Height/2);subject.GetComponent<Renderer>().material.color=new Color(.93f,.53f,.12f);
                    var eye=GameObject.CreatePrimitive(PrimitiveType.Sphere);eye.name="Optic";eye.GetComponent<Collider>().enabled=false;eye.transform.SetParent(subject.transform,false);
                    eye.transform.localScale=new Vector3(.55f,.34f,.48f);eye.transform.localPosition=Vector3.up*.36f+Vector3.forward*.4f;eye.GetComponent<Renderer>().material.color=Color.black;
                    if(Terminal.RoamingEnemyModel){
                        subject.GetComponent<Renderer>().enabled=false;eye.SetActive(false);
                        var visual=Instantiate(Terminal.RoamingEnemyModel,subject.transform);
                        // Imported character models may include export-preview cameras and lights.
                        // Keep the existing player and CCTV cameras in charge of rendering.
                        foreach(var embeddedCamera in visual.GetComponentsInChildren<Camera>(true))embeddedCamera.enabled=false;
                        foreach(var embeddedLight in visual.GetComponentsInChildren<Light>(true))embeddedLight.enabled=false;
                        foreach(var embeddedListener in visual.GetComponentsInChildren<AudioListener>(true))embeddedListener.enabled=false;
                        foreach(var bodyCollider in visual.GetComponentsInChildren<Collider>())bodyCollider.enabled=false;
                        var rs=visual.GetComponentsInChildren<Renderer>();if(rs.Length>0){var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);visual.transform.localScale*=Height/Mathf.Max(.01f,bounds.size.y);bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);visual.transform.position+=subject.transform.position-bounds.center;}
                    }
                    subject.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(c.transform.position-a,Vector3.up).normalized,Vector3.up);return true;
                }
            }return false;
        }
        bool Visible(Camera c,Vector3 p){
            if(!screen)return Seen(c,p);
            if(!c)return false;
            var collider=screen.GetComponent<Collider>();
            for(int i=0;i<2;i++){
                Vector3 point=p+Vector3.up*(i==0?Height*.8f:Height*.4f),delta=point-c.transform.position,v=c.WorldToViewportPoint(point);
                if(v.z<c.nearClipPlane||v.z>c.farClipPlane||v.x<0||v.x>1||v.y<0||v.y>1)continue;
                if(!Physics.Linecast(c.transform.position,point,World,QueryTriggerInteraction.Ignore)&&!collider.Raycast(new Ray(c.transform.position,delta.normalized),out _,delta.magnitude))return true;
            }return false;
        }
        void Update(){
            if(!Terminal||Time.timeScale==0)return;var c=Terminal.WatchedCamera;
            if(!subject){if(Home&&Time.time>=scan){scan=Time.time+2;if(!Place(Home)&&Terminal.CameraFeeds.Length>0)Place(Terminal.CameraFeeds[(fallbackFeed++)%Terminal.CameraFeeds.Length]);}return;}
            var feet=subject.transform.position-Vector3.up*(Height/2);
            if(Time.time>=scan){scan=Time.time+.16f;
                if(Visible(c,feet)){
                    unseenSince=-1;if(noticed<0)noticed=Time.time;
                    if(marked||Time.time-noticed>.8f){hiding=true;evadeToExposed=Visible(c,hidden)&&!Visible(c,exposed);wait=Time.time+1.6f;}
                }else{
                    noticed=-1;if(unseenSince<0)unseenSince=Time.time;if(Time.time-unseenSince>1)marked=false;
                    if(Time.time>wait&&!Visible(c,exposed))hiding=false;
                }
            }
            var target=hiding?(evadeToExposed?exposed:hidden):(towardCover?hidden:exposed);
            if(!hiding&&Vector3.Distance(feet,target)<.12f)towardCover=!towardCover;
            var next=Vector3.MoveTowards(feet,target,Time.deltaTime*(hiding?2.8f:.45f));
            if(Ground(next,out var floor)&&Clear(floor))subject.transform.position=floor+Vector3.up*(Height/2);
        }
    }
}






