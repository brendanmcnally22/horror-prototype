using UnityEngine;
using UnityEngine.Events;
namespace Blockout.Discovery {
    public sealed class HeavyLeverHearingReceiver : MonoBehaviour
    {
        [Range(0,1)]public float MinimumStrength=.05f;
        public UnityEvent<Vector3> OnInvestigatePosition=new UnityEvent<Vector3>();
        public Vector3 LastHeardPosition {get;private set;}
        public int HeardCount {get;private set;}
        void OnEnable()=>HeavyLeverNoise.Emitted+=Hear;
        void OnDisable()=>HeavyLeverNoise.Emitted-=Hear;
        void Hear(LeverNoiseSignal signal)
        {
            if(signal.Strength<MinimumStrength||Vector3.Distance(transform.position,signal.Position)>signal.Radius)return;
            LastHeardPosition=signal.Position;HeardCount++;OnInvestigatePosition.Invoke(signal.Position);
        }
    }
}

