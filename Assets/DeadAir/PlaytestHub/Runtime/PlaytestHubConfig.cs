using System;
using UnityEngine;

namespace DeadAir.Playtesting
{
    public enum HubPage { Experience, Menu, Feedback, Results }
    public enum HubExperienceKind { Discovery, Security, Lighting }
    [Serializable] public class HubExperienceDefinition
    {
        public string Id, Title, Description, ScenePath;
        public HubExperienceKind Kind;
    }
    [CreateAssetMenu(menuName="Dead Air/Playtest Hub")]
    public class PlaytestHubConfig : ScriptableObject
    {
        public string MenuScene, FeedbackScene, ResultsScene;
        public HubExperienceDefinition[] Experiences = Array.Empty<HubExperienceDefinition>();
    }
}
