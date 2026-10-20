using System.Collections.Generic;
using UnityEngine;
using UHFPS.Runtime;
namespace Blockout.Discovery
{
    [RequireComponent(typeof(BoxCollider))]
    public class DesignMetricTrigger : MonoBehaviour
    {
        public string ZoneId="rename_this_zone";
        public string EnterEvent="zone_enter";
        public bool FirstVisitOnly;
        public bool LogExitAndDuration=true;
        readonly HashSet<Collider> occupants=new HashSet<Collider>();
        bool visited,recording;
        float entered;
        void Reset(){GetComponent<BoxCollider>().isTrigger=true;}
        void OnTriggerEnter(Collider other)
        {
            if(!other.GetComponentInParent<PlayerManager>()||!occupants.Add(other)||occupants.Count!=1)return;
            if(FirstVisitOnly&&visited)return;
            visited=true;recording=true;entered=Time.realtimeSinceStartup;
            DesignSession.Record(EnterEvent,ZoneId,other.transform.position);
        }
        void OnTriggerExit(Collider other){if(!occupants.Remove(other)||occupants.Count>0)return;EndVisit();}
        void EndVisit(){if(recording&&LogExitAndDuration)DesignSession.Record("zone_exit",ZoneId,transform.position,0,Time.realtimeSinceStartup-entered);recording=false;}
        void OnDisable(){EndVisit();occupants.Clear();}
        public void LogEvent(string eventName){DesignSession.Record(eventName,ZoneId,transform.position);}
        void OnDrawGizmos(){var box=GetComponent<BoxCollider>();Gizmos.matrix=transform.localToWorldMatrix;Gizmos.color=new Color(.3f,1,.6f,.15f);Gizmos.DrawCube(box.center,box.size);Gizmos.color=Color.green;Gizmos.DrawWireCube(box.center,box.size);}
    }
}
