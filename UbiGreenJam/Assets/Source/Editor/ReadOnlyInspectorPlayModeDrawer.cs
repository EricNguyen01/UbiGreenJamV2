// Script Author: Pham Nguyen. All Rights Reserved. 
// GitHub: https://github.com/EricNguyen01.

using UnityEngine;
using UnityEditor;

/*
 * This class contains custom drawer for ReadOnlyInspectorPlayModeAttribute.cs.
 */
[CustomPropertyDrawer(typeof(ReadOnlyInspectorPlayModeAttribute))]
public class ReadOnlyInspectorPlayModeDrawer : ReadOnlyInspectorDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool originalState = GUI.enabled;

        if (!Application.isPlaying) GUI.enabled = originalState;
        else GUI.enabled = false;

        // Drawing Property
        EditorGUI.PropertyField(position, property, label, true);

        // Setting old GUI enabled value
        GUI.enabled = true;
    }
}
