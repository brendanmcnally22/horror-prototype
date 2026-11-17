using System.Collections;
using UnityEngine;
using UnityEngine.Events;
namespace Blockout.Discovery {
 public sealed class LockerFallScare:MonoBehaviour{
 [Tooltip("Locker or a floor-level pivot containing it. Prefab includes a sample locker.")]public Transform LockerPivot;
 public Vector3 FallAngle=new Vector3(-87,0,0);
 public bool RotateAroundBase=true;
 [Min(.1f)]public float FallSeconds=.7f;
 public AudioClip ImpactSound;
 public UnityEvent OnImpact=new UnityEvent();
 public float HearingRadius=25;
 AudioSource source;AudioClip generated;bool falling;
 public void Activate(){if(!falling&&LockerPivot)StartCoroutine(Fall());}
 IEnumerator Fall(){
 falling=true;Vector3 localBase=Vector3.zero;
 if(RotateAroundBase){var renderers=LockerPivot.GetComponentsInChildren<Renderer>();if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);localBase=LockerPivot.InverseTransformPoint(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));}}
 Vector3 basePosition=LockerPivot.TransformPoint(localBase);
 Quaternion start=LockerPivot.localRotation,target=start*Quaternion.Euler(FallAngle);
 for(float t=0;t<FallSeconds;t+=Time.deltaTime){LockerPivot.localRotation=Quaternion.Slerp(start,target,Mathf.Pow(t/FallSeconds,2));if(RotateAroundBase)LockerPivot.position+=basePosition-LockerPivot.TransformPoint(localBase);yield return null;}
 LockerPivot.localRotation=target;if(RotateAroundBase)LockerPivot.position+=basePosition-LockerPivot.TransformPoint(localBase);
 if(!source){source=LockerPivot.gameObject.AddComponent<AudioSource>();source.spatialBlend=1;source.minDistance=2;source.maxDistance=40;source.playOnAwake=false;}
 if(!ImpactSound&&!generated)generated=PrototypeScareAudio.Make("Locker crash",0);
 source.PlayOneShot(ImpactSound?ImpactSound:generated,.85f);
 PrototypeScareAudio.Emit(LockerPivot.position,HearingRadius,1,gameObject);OnImpact.Invoke();
 }
 void OnDestroy(){if(generated)Destroy(generated);}
 }}


