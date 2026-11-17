using UnityEngine;
namespace DeadAir.Playtesting
{
    public class PlaytestExperience : MonoBehaviour
    {
        public HubPage Page;
        public string ExperienceId;
        [Header("Assign your enemy here; none is substituted automatically")]
        public GameObject EnemyRoot;
        [Min(1)] public float SecurityOperatingSeconds=60;
        [TextArea] public string Objective;
        void Awake(){
            if(Page!=HubPage.Experience)return;
            if(ExperienceId=="security")Objective="Spot enemies on 3 different cameras";
            else if(ExperienceId=="lighting")Objective="Choose a door";
            else if(ExperienceId=="discovery")Objective="Find 3 fuses";
        }
        void Start()
        {
            if(Page==HubPage.Experience)
                foreach(var old in FindObjectsByType<Blockout.Discovery.DiscoveryPlaytestRestart>(FindObjectsSortMode.None))old.enabled=false;
        }
        public void CompleteExperience(){if(PlaytestHub.Instance)PlaytestHub.Instance.Finish("completed","goal_reached");}
    }
}


