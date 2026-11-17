using UnityEngine;
using UnityEngine.Events;
using UHFPS.Runtime;
namespace Blockout.Discovery {
 [RequireComponent(typeof(BoxCollider))]
 public sealed class PlaceableScareTrigger:MonoBehaviour{
 public bool TriggerOnce=true;
 public UnityEvent OnPlayerEntered=new UnityEvent();
 bool fired;
 void Reset(){GetComponent<BoxCollider>().isTrigger=true;}
 void OnTriggerEnter(Collider other){if((TriggerOnce&&fired)||!other.GetComponentInParent<PlayerManager>())return;fired=true;OnPlayerEntered.Invoke();}
 public void Rearm(){fired=false;}
 void OnDrawGizmosSelected(){var b=GetComponent<BoxCollider>();Gizmos.matrix=transform.localToWorldMatrix;Gizmos.color=Color.red;Gizmos.DrawWireCube(b.center,b.size);}
 }}
