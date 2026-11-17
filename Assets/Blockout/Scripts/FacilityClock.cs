using UnityEngine;
namespace Blockout.Discovery {
 public sealed class FacilityClock : MonoBehaviour {
 public Transform HourHand,MinuteHand,SecondHand;
 public Vector3 Axis=Vector3.forward;public float Direction=-1;
 Quaternion hour,minute,second;
 void Awake(){if(HourHand)hour=HourHand.localRotation;if(MinuteHand)minute=MinuteHand.localRotation;if(SecondHand)second=SecondHand.localRotation;}
 void Update(){var t=System.DateTime.Now;float sec=t.Second+t.Millisecond/1000f;
 if(HourHand)HourHand.localRotation=hour*Quaternion.AngleAxis(Direction*((t.Hour%12)+t.Minute/60f)*30,Axis);
 if(MinuteHand)MinuteHand.localRotation=minute*Quaternion.AngleAxis(Direction*(t.Minute+sec/60)*6,Axis);
 if(SecondHand)SecondHand.localRotation=second*Quaternion.AngleAxis(Direction*sec*6,Axis);}
 }
}
