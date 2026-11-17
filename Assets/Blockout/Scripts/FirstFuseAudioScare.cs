using UnityEngine;
namespace Blockout.Discovery{
 public sealed class FirstFuseAudioScare:MonoBehaviour{
 public float DistanceFromPlayer=12,Volume=.85f;
 public AudioClip Shriek;
 bool fired;AudioClip generated;AudioSource source;
 public void TriggerFromPickup(Vector3 player){
 if(fired)return;fired=true;
 // A positional sound behind the pickup location; no enemy object or AI is spawned.
 Vector3 forward=UHFPS.Runtime.PlayerManager.Instance?UHFPS.Runtime.PlayerManager.Instance.transform.forward:Vector3.forward;
 var pos=player-forward*DistanceFromPlayer+Vector3.up*1.5f;
 var go=new GameObject("First fuse - distant shriek");go.transform.position=pos;go.transform.SetParent(transform,true);
 source=go.AddComponent<AudioSource>();source.spatialBlend=1;source.minDistance=3;source.maxDistance=60;source.playOnAwake=false;
 if(!Shriek)generated=PrototypeScareAudio.Make("Unseen creature shriek",1);
 source.PlayOneShot(Shriek?Shriek:generated,Volume);DesignSession.Record("first_fuse_distant_shriek",name,pos);
 }
 void OnDestroy(){if(generated)Destroy(generated);}
 }}
