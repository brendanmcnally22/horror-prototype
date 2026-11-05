using System;
using UnityEngine;
using UnityEngine.Events;
namespace Blockout.Discovery
{
    public readonly struct LeverNoiseSignal
    {
        public readonly Vector3 Position;
        public readonly float Radius,Strength;
        public readonly GameObject Source;
        public LeverNoiseSignal(Vector3 position,float radius,float strength,GameObject source){Position=position;Radius=radius;Strength=strength;Source=source;}
    }
    public static class HeavyLeverNoise
    {
        public static event Action<LeverNoiseSignal> Emitted;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset()=>Emitted=null;
        public static void Emit(Vector3 position,float radius,float strength,GameObject source)=>Emitted?.Invoke(new LeverNoiseSignal(position,radius,strength,source));
    }
}
