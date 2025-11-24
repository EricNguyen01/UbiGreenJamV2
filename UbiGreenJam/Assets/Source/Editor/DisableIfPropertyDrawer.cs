// Script Author: Pham Nguyen. All Rights Reserved. 
// GitHub: https://github.com/EricNguyen01.

using UnityEngine;
using UnityEditor;
using FMOD;

/*
 * This class contains custom drawer for DisableIfAttribute.cs.
 */

[CustomPropertyDrawer(typeof(DisableIfAttribute))]
public class DisableIfPropertyDrawer : PropertyDrawer
{
    private bool shouldDisable = false;

    private DisableIfAttribute disableIfAttribute;

    private SerializedProperty boolTargettedProp;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        disableIfAttribute = (DisableIfAttribute)attribute;

        boolTargettedProp = property.serializedObject.FindProperty(disableIfAttribute.targettedProperty);

        if(boolTargettedProp == null)
        {
            property.serializedObject.FindProperty($"<{disableIfAttribute.targettedProperty}>k__BackingField");
        }

        if (boolTargettedProp == null)
        {
            UnityEngine.Debug.LogWarning("[DisableIf] Invalid Property Name for Attribute: " +
                                                        disableIfAttribute.targettedProperty +
                                                        " of Object: " +
                                                        property.serializedObject.targetObject);
        }

        else
        {
            if (boolTargettedProp.boolValue == disableIfAttribute.disableCond) shouldDisable = true;
            else shouldDisable = false;
        }

        if (shouldDisable) GUI.enabled = false;
        else GUI.enabled = true;

        EditorGUI.PropertyField(position, property, label, true);

        GUI.enabled = true;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
