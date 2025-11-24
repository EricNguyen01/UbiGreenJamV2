using UnityEditor;
using UnityEngine;

namespace CrossClimbLite
{
    [CustomPropertyDrawer(typeof(HelpBoxAttribute))]
    public class HelpBoxDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            HelpBoxAttribute helpBoxAttribute = (HelpBoxAttribute)attribute;

            // Calculate the height of the help box based on the message length
            float helpBoxHeight = EditorGUIUtility.singleLineHeight * 2; // Default height for a short message

            if (!string.IsNullOrEmpty(helpBoxAttribute.message))
            {
                GUIStyle style = GUI.skin.GetStyle("HelpBox");

                // Adjust width for padding
                helpBoxHeight = style.CalcHeight(new GUIContent(helpBoxAttribute.message), EditorGUIUtility.currentViewWidth - 38);
            }

            // Draw the help box
            Rect helpBoxRect = new Rect(position.x, position.y, position.width, helpBoxHeight);

            EditorGUI.HelpBox(helpBoxRect, helpBoxAttribute.message, (MessageType)helpBoxAttribute.type);

            // Adjust the position for the actual property below the help box
            position.y += helpBoxHeight + EditorGUIUtility.standardVerticalSpacing;

            position.height -= helpBoxHeight + EditorGUIUtility.standardVerticalSpacing;

            // Draw the original property field
            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            HelpBoxAttribute helpBoxAttribute = (HelpBoxAttribute)attribute;

            // Calculate the height of the help box
            float helpBoxHeight = EditorGUIUtility.singleLineHeight * 2;

            if (!string.IsNullOrEmpty(helpBoxAttribute.message))
            {
                GUIStyle style = GUI.skin.GetStyle("HelpBox");

                helpBoxHeight = style.CalcHeight(new GUIContent(helpBoxAttribute.message), EditorGUIUtility.currentViewWidth - 38);
            }

            // Add the height of the help box to the original property height
            return base.GetPropertyHeight(property, label) + helpBoxHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
