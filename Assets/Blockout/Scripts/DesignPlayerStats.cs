using UnityEngine;
using UHFPS.Runtime;
namespace Blockout.Discovery
{
    public class DesignPlayerStats : MonoBehaviour
    {
        public float SampleInterval=10;
        Vector3 previous;
        float distance,idle,next;
        bool dead;
        PlayerHealth health;
        void Start(){previous=transform.position;health=GetComponent<PlayerHealth>();next=Time.unscaledTime+SampleInterval;DesignSession.Record("level_start",gameObject.scene.name,transform.position);}
        void Update()
        {
            float moved=Vector3.Distance(previous,transform.position);previous=transform.position;
            if(moved<5)distance+=moved;
            if(moved<.05f*Time.unscaledDeltaTime)idle+=Time.unscaledDeltaTime;
            if(health&&health.IsDead&&!dead){dead=true;DesignSession.Record("player_death",gameObject.scene.name,transform.position);}
            if(health&&!health.IsDead)dead=false;
            if(Time.unscaledTime>=next){next=Time.unscaledTime+Mathf.Max(1,SampleInterval);DesignSession.Record("movement_sample",gameObject.scene.name,transform.position,distance,idle);distance=0;idle=0;}
        }
    }
}
