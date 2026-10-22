using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadAir.Playtesting
{
    [CreateAssetMenu(menuName="Dead Air/Playtest Settings")]
    public sealed class PlaytestSettings : ScriptableObject
    {
        public bool Enabled=true;
        [Tooltip("Leave off for public non-development builds. Disables recording and developer UI.")]
        public bool AllowInReleaseBuilds;
        public bool EnableDeveloperUI=true;
        public Key MenuKey=Key.F8;
        public bool AutoRecord=true;
        [Range(1,60)] public float SamplesPerSecond=10;
        [Min(.1f)] public float StationaryHeartbeat=1;
        [Min(1)] public float FlushInterval=5;
        public bool RecordCamera=true;
        public string PrototypeVersion="";
        [Header("Replay camera (does not change gameplay cameras)")]
        public LayerMask MapLayers=~((1<<3)|(1<<5)|(1<<7)|(1<<13));
        public float CameraHeight=80;
        public float MinimumElevation=-20;
        public float MaximumElevation=50;
        [Min(.25f)] public float HeatmapCellSize=1;
        public bool Available=>Enabled&&(Application.isEditor||Debug.isDebugBuild||AllowInReleaseBuilds);
    }
}
