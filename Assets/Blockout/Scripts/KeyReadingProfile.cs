using UnityEngine;
namespace Blockout.Progression
{
    [CreateAssetMenu(menuName="Blockout/Key Reading Text",fileName="Power Hall Key Reading")]
    public class KeyReadingProfile:ScriptableObject
    {
        public string Title="Power Hall Key";
        [TextArea(4,12)] public string ReadingText="POWER HALL\n\nThis key opens the power hall. Find the downstairs fusebox and restore full power to the experiment room.";
    }
}
