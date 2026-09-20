using UnityEngine;
using UHFPS.Runtime;
namespace Blockout.Progression
{
    [RequireComponent(typeof(BoxCollider))]
    public class ObservationExitTrigger:MonoBehaviour
    {
        public FacilityProgression Progression;
        void OnTriggerEnter(Collider other){if(other.GetComponentInParent<PlayerManager>())Progression.CrossObservationThreshold();}
        void OnTriggerStay(Collider other){if(!Progression.Won && other.GetComponentInParent<PlayerManager>())Progression.CrossObservationThreshold();}
    }
}
