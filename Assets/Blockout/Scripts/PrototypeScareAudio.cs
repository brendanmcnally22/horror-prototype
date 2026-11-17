using UnityEngine;
namespace Blockout.Discovery {
 public static class PrototypeScareAudio {
 public static event System.Action<Vector3,float,float,GameObject> Noise;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){Noise=null;}
 public static void Emit(Vector3 position,float radius,float strength,GameObject source){Noise?.Invoke(position,radius,strength,source);}
 public static AudioClip Make(string name,int kind){
 const int rate=22050;float seconds=kind==2?3:1.2f;var data=new float[(int)(rate*seconds)];var r=new System.Random(113);
 for(int i=0;i<data.Length;i++){float t=(float)i/rate,phase=(float)i/data.Length;
 float env=kind==0?Mathf.Exp(-t*5):Mathf.Sin(Mathf.PI*phase);
 float tone=kind==1?Mathf.Sin(2*Mathf.PI*(600*t-140*t*t)):kind==2?Mathf.Sin(2*Mathf.PI*85*t)+.4f*Mathf.Sin(2*Mathf.PI*128*t):Mathf.Sin(2*Mathf.PI*47*t);
 data[i]=(tone*.27f+(float)(r.NextDouble()*2-1)*.28f)*env;}
 var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
 }}
}

