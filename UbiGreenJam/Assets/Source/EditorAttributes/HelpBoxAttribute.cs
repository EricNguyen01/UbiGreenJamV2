using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace CrossClimbLite
{
    public class HelpBoxAttribute : PropertyAttribute
    {
        public string message;

        public HelpBoxMessageType type;

        public HelpBoxAttribute(string message, HelpBoxMessageType type = HelpBoxMessageType.Info)
        {
            this.message = message;

            this.type = type;
        }
    }
}
