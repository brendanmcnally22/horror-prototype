using System.Collections;
using UnityEngine;
using UHFPS.Runtime;
using UHFPS.Input;
namespace Blockout.Discovery {
 public sealed class CandleTrialExperience:MonoBehaviour{
 public PlayerManager Player;
 public CandleItem Candle;
 public PrototypeMover Elevator;
 public BoxCollider ElevatorRiderZone;
 public AudioClip SlenderAmbience,AnxiousBreathing;
 [Range(0,1)]public float AmbienceVolume=.25f,BreathingVolume=.32f;
 public float DarknessThreshold=.06f,LightThreshold=.12f,DarknessDelay=1.25f;
 public string CandleId="6993e34dac744c23966afbcbba685cf9";
 AudioSource ambience,breathing;Light[] lights;bool leftElevator,wasInElevator,dark;float nextSense,darkTime;
 public void CandlePickedUp(){StartCoroutine(EquipAndExplain());}
 IEnumerator EquipAndExplain(){
 yield return null;
 if(Player&&Candle&&Inventory.HasReference&&Inventory.Instance.GetItemQuantity(CandleId)>0){int index=Player.PlayerItems.PlayerItems.IndexOf(Candle);if(index>=0)Player.PlayerItems.SwitchPlayerItem(index);}
 if(GameManager.HasReference)GameManager.Instance.ShowHintMessage("Candle collected. Press F to equip or put it away. Hold RIGHT MOUSE to raise it for more light.",9);
 }
 AudioSource Source(AudioClip clip){
 var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.loop=true;s.spatialBlend=0;s.clip=clip;s.volume=0;return s;
 }
 void Start(){ambience=Source(SlenderAmbience);breathing=Source(AnxiousBreathing);lights=FindObjectsByType<Light>(FindObjectsSortMode.None);if(!Elevator)leftElevator=true;}
 void Update(){
 if(!Player)return;
 var game=GameManager.HasReference?GameManager.Instance:null;
 bool paused=Time.timeScale<=0||(game&&(game.IsPaused||game.PlayerDied)); 
 if(paused){if(ambience.isPlaying)ambience.Pause();if(breathing.isPlaying)breathing.Pause();return;}
 if(!leftElevator&&ElevatorRiderZone){
 var p=ElevatorRiderZone.transform.InverseTransformPoint(Player.transform.position+Vector3.up*.45f)-ElevatorRiderZone.center;
 var half=ElevatorRiderZone.size*.5f;bool inside=Mathf.Abs(p.x)<=half.x+.2f&&Mathf.Abs(p.y)<=half.y+.5f&&Mathf.Abs(p.z)<=half.z+.2f;
 if(inside)wasInElevator=true;
 if(wasInElevator&&!inside&&Elevator.AtEnd)leftElevator=true;
 }
 if(leftElevator&&SlenderAmbience&&!ambience.isPlaying)ambience.Play();
 ambience.volume=Mathf.MoveTowards(ambience.volume,leftElevator?AmbienceVolume:0,Time.unscaledDeltaTime*.12f);
 if(Candle&&game&&Player.PlayerItems.CanInteract&&!game.IsInventoryShown&&!Candle.ExamineController.IsExamining&&game.PlayerPresence.IsUnlockedAndCamera&&Inventory.HasReference&&Inventory.Instance.GetItemQuantity(CandleId)>0&&InputManager.ReadButtonOnce(this,Controls.FLASHLIGHT)){
 int i=Player.PlayerItems.PlayerItems.IndexOf(Candle);if(i>=0)Player.PlayerItems.SwitchPlayerItem(i);
 }
 if(Time.time>=nextSense){nextSense=Time.time+.3f;float illumination=Illumination();if(illumination<DarknessThreshold)dark=true;else if(illumination>LightThreshold)dark=false;}
 bool candleLit=Candle&&Candle.ItemObject&&Candle.ItemObject.activeInHierarchy&&Candle.FlameRenderer&&Candle.FlameRenderer.gameObject.activeInHierarchy;
 if(candleLit)dark=false;
 darkTime=dark&&!candleLit?darkTime+Time.deltaTime:0;
 float target=darkTime>DarknessDelay?BreathingVolume:0;
 if(target>0&&AnxiousBreathing&&!breathing.isPlaying)breathing.Play();
 breathing.volume=Mathf.MoveTowards(breathing.volume,target,Time.deltaTime*.25f);
 if(breathing.volume<=.001f&&target==0&&breathing.isPlaying)breathing.Pause();
 }
 float Illumination(){
 if(Candle&&Candle.ItemObject.activeInHierarchy&&Candle.FlameRenderer.gameObject.activeInHierarchy)return 1;
 Vector3 head=Player.MainCamera?Player.MainCamera.transform.position:Player.transform.position+Vector3.up*1.4f;float total=0;
 foreach(var light in lights){
 if(!light||!light.isActiveAndEnabled||light.intensity<=0||light.GetComponentInParent<PlayerManager>()||light.type==LightType.Directional)continue;
 Vector3 delta=head-light.transform.position;float distance=delta.magnitude;if(distance>light.range||distance<.01f)continue;
 if(light.type==LightType.Spot&&Vector3.Angle(light.transform.forward,delta)>light.spotAngle*.5f)continue;
 if(Physics.Linecast(light.transform.position,head,~((1<<3)|(1<<5)|(1<<7)),QueryTriggerInteraction.Ignore))continue;
 float luminance=light.color.r*.2126f+light.color.g*.7152f+light.color.b*.0722f;
 total+=luminance*light.intensity*Mathf.Pow(1-distance/light.range,2)/(1+distance*distance*.15f);
 if(total>LightThreshold)return total;
 }return total;
 }
 void OnDisable(){if(ambience)ambience.Stop();if(breathing)breathing.Stop();}
 }}
