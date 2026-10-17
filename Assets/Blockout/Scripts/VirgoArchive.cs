using System;
using UnityEngine;

namespace Blockout.Discovery
{
    [CreateAssetMenu(menuName="Virgo/Terminal Archive")]
    public class VirgoArchive : ScriptableObject
    {
        [Serializable] public class Record
        {
            public string Title;
            [Range(0,2)] public int Clearance;
            [TextArea(5,20)] public string Body;
        }
        [Serializable] public class Extension
        {
            public string Number, Department;
            [TextArea(3,10)] public string Transcript;
        }
        public Record[] Records = new Record[0];
        public Extension[] Extensions = new Extension[0];
        [Range(88,108)] public float HiddenFrequency=96.4f;
        [TextArea] public string DecodedMessage="MAINTENANCE / LOWER CIRCUIT\nThree replacement fuses required. Return to the entry electrical panel.\nVoice line: extension 204.";
    }
}
