using UnityEngine;
using UnityEngine.Events;
namespace Blockout.Discovery {
 public sealed class TransitNoiseScare:MonoBehaviour{
 [Tooltip("Assign the transit car. If empty, sound comes from this trigger.")]public Transform TransitCar;
 public PrototypeMover TransitMover;
 public bool RunCar=true;
 [Min(.1f)]public float RunSpeed=10;
 public AudioClip Sound;
 [Range(0,1)]public float Volume=.85f;
 public float HearingRadius=40;
 public UnityEvent OnNoise=new UnityEvent();
 AudioSource source;AudioClip generated;
 public void Activate(){
 if(TransitMover&&RunCar){TransitMover.Speed=RunSpeed;TransitMover.Acceleration=8;TransitMover.Automatic=false;TransitMover.SetEnd(!TransitMover.AtEnd);}
 var target=TransitCar?TransitCar:transform;
 if(!source){source=target.gameObject.AddComponent<AudioSource>();source.spatialBlend=1;source.minDistance=3;source.maxDistance=60;source.playOnAwake=false;}
 if(!Sound&&!generated)generated=PrototypeScareAudio.Make("Transit brake roar",2);
 if(TransitMover&&RunCar){source.clip=Sound?Sound:generated;source.loop=true;source.volume=Volume;source.Play();StopAllCoroutines();StartCoroutine(StopAtArrival());}else source.PlayOneShot(Sound?Sound:generated,Volume);PrototypeScareAudio.Emit(target.position,HearingRadius,1,gameObject);OnNoise.Invoke();
 }
 System.Collections.IEnumerator StopAtArrival(){yield return null;while(TransitMover&&(TransitMover.TravelRequested||TransitMover.IsTravelling))yield return null;if(source)source.Stop();}
 void OnDestroy(){if(generated)Destroy(generated);}
 }}




