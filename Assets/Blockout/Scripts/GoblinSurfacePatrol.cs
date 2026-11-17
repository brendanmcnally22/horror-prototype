using UnityEngine;
using UnityEngine.Events;
using UHFPS.Runtime;
namespace Blockout.Discovery {
 public sealed class GoblinSurfacePatrol : MonoBehaviour {
 [System.Serializable]public struct SurfacePoint {public Vector3 Position,Normal;}
 public SurfacePoint[] Route=new SurfacePoint[0];
 public Transform Visual;
 [Min(.1f)]public float CollisionRadius=.45f;
 public float Speed=1.4f,ScareDistance=3.5f,ScareCooldown=22,ScareSeconds=.4f;
 public UnityEvent OnScare=new UnityEvent();
 public AudioClip ScareSound;
 AudioClip shriek;bool firstFuse,investigating,groundedInvestigation;Vector3 pickupLocation;
 UnityEngine.AI.NavMeshPath investigatePath;int corner;
 public void InvestigateFirstFuse(Vector3 location){
 if(firstFuse)return;firstFuse=true;pickupLocation=location;investigating=true;direction=-1;index=Mathf.Max(0,index-1);
 if(!audioSource)return;audioSource.spatialBlend=1;audioSource.minDistance=2;audioSource.maxDistance=60;
 if(!shriek)shriek=PrototypeScareAudio.Make("Goblin first fuse shriek",1);audioSource.PlayOneShot(shriek,.9f);
 DesignSession.Record("goblin_first_fuse_shriek",name,transform.position);
 }
 void InvestigateGround(){
 transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(Vector3.ProjectOnPlane(pickupLocation-transform.position,Vector3.up).sqrMagnitude>.01f?Vector3.ProjectOnPlane(pickupLocation-transform.position,Vector3.up):transform.forward,Vector3.up),Time.deltaTime*5);
 Vector3 goal=pickupLocation+Vector3.up*.85f;
 if(Vector3.Distance(transform.position,goal)<1.5f){investigating=false;return;}
 if(investigatePath==null){
 investigatePath=new UnityEngine.AI.NavMeshPath();
 if(UnityEngine.AI.NavMesh.SamplePosition(transform.position,out var from,3,UnityEngine.AI.NavMesh.AllAreas)&&UnityEngine.AI.NavMesh.SamplePosition(pickupLocation,out var to,3,UnityEngine.AI.NavMesh.AllAreas)&&UnityEngine.AI.NavMesh.CalculatePath(from.position,to.position,UnityEngine.AI.NavMesh.AllAreas,investigatePath)&&investigatePath.status==UnityEngine.AI.NavMeshPathStatus.PathComplete)corner=1;
 }
 if(investigatePath.corners.Length>corner){goal=investigatePath.corners[corner]+Vector3.up*.85f;if(Vector3.Distance(transform.position,goal)<.3f)corner++;}
 var forward=Vector3.ProjectOnPlane(goal-transform.position,Vector3.up).normalized;
 foreach(float angle in new[]{0f,40,-40,80,-80,130,-130}){
 var d=Quaternion.Euler(0,angle,0)*forward;float step=Speed*Time.deltaTime;
 if(Physics.SphereCast(transform.position,.35f,d,out _,step+.4f,~((1<<3)|(1<<5)|(1<<7)),QueryTriggerInteraction.Ignore))continue;
 var next=transform.position+d*step;
 if(Physics.Raycast(next,Vector3.down,out var floor,1.1f,~((1<<3)|(1<<5)|(1<<7)),QueryTriggerInteraction.Ignore)&&floor.normal.y>.8f){next.y=floor.point.y+.85f;transform.position=next;break;}
 }
 }
 AudioClip generated;int index=1,direction=1;float cooldown,flashUntil;AudioSource audioSource;
 public int Scares {get;private set;}
 void Awake(){audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=.6f;audioSource.playOnAwake=false;if(Route.Length>0)transform.position=Route[0].Position;}
 void Update(){
 if(Time.timeScale<=0||Route.Length<2)return;
 if(investigating&&groundedInvestigation){InvestigateGround();return;}
 var target=Route[index];Vector3 delta=target.Position-transform.position;Vector3 up=Vector3.Slerp(transform.up,target.Normal,Time.deltaTime*5);
 if(delta.sqrMagnitude>.01f){
   if(!Physics.SphereCast(transform.position,CollisionRadius,delta.normalized,out _,Mathf.Min(delta.magnitude,Speed*Time.deltaTime+.03f),~((1<<3)|(1<<5)|(1<<7)),QueryTriggerInteraction.Ignore))
       transform.position=Vector3.MoveTowards(transform.position,target.Position,Speed*Time.deltaTime);
   var forward=Vector3.ProjectOnPlane(delta,up);if(forward.sqrMagnitude<.001f)forward=Vector3.ProjectOnPlane(transform.forward,up);if(forward.sqrMagnitude<.001f)forward=Vector3.ProjectOnPlane(Vector3.right,up);transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(forward,up),Time.deltaTime*7);
 }else{index+=direction;if(index>=Route.Length){direction=-1;index=Route.Length-2;}if(index<0){if(investigating){groundedInvestigation=true;index=0;}else{direction=1;index=1;}}}
 if(!PlayerManager.Instance||Time.time<cooldown)return;
 var camera=PlayerManager.Instance.MainCamera;if(!camera)return;
 var pos=transform.position+transform.up*.3f;var view=camera.WorldToViewportPoint(pos);float distance=Vector3.Distance(camera.transform.position,pos);
 if(distance<ScareDistance&&view.z>0&&view.x>.05f&&view.x<.95f&&view.y>.05f&&view.y<.95f&&!Physics.Linecast(camera.transform.position,pos,~((1<<3)|(1<<5)|(1<<7)),QueryTriggerInteraction.Ignore)){
    Scares++;cooldown=Time.time+ScareCooldown;flashUntil=Time.unscaledTime+ScareSeconds;OnScare.Invoke();
    if(!ScareSound&&!generated){const int rate=22050;var samples=new float[rate/2];var rng=new System.Random(17);for(int i=0;i<samples.Length;i++){float t=(float)i/rate;samples[i]=((float)(rng.NextDouble()*2-1)*.32f+Mathf.Sin(2*Mathf.PI*(110*t-35*t*t))*.25f)*Mathf.Sin(Mathf.PI*i/samples.Length);}generated=AudioClip.Create("Goblin hiss",samples.Length,1,rate,false);generated.SetData(samples,0);}
    audioSource.PlayOneShot(ScareSound?ScareSound:generated,.65f);
    DesignSession.Record("goblin_scare",name,transform.position,Scares);
 }
 }
 void OnDestroy(){if(generated)Destroy(generated);if(shriek)Destroy(shriek);}
 void OnGUI(){if(Time.unscaledTime>=flashUntil)return;var c=GUI.color;GUI.depth=-45;GUI.color=new Color(.24f,.015f,.01f,.18f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=c;}
 void OnDrawGizmosSelected(){Gizmos.color=Color.magenta;for(int i=0;i<Route.Length;i++){Gizmos.DrawWireSphere(Route[i].Position,.16f);Gizmos.DrawLine(Route[i].Position,Route[i].Position+Route[i].Normal*.5f);if(i>0)Gizmos.DrawLine(Route[i-1].Position,Route[i].Position);}}
 }
}



