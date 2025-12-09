using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Editor
{
    public class ShowIfEnumAttribute : PropertyAttribute
    {
        public string enumFieldName;
        public int[] enumValue;

        public ShowIfEnumAttribute(string enumFieldName, params int[] enumValue)
        {
            this.enumFieldName = enumFieldName;
            this.enumValue = enumValue;
        }
    }
    
    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ShowIfEnumAttribute))]
    public class ShowIfEnumDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ShowIfEnumAttribute attr = (ShowIfEnumAttribute)attribute;
            SerializedProperty enumProp = property.serializedObject.FindProperty(attr.enumFieldName);

            for (int i = 0; i < attr.enumValue.Length; i++)
            {
                if (enumProp != null && enumProp.enumValueIndex == attr.enumValue[i])
                {
                    EditorGUI.PropertyField(position, property, label, true);
                    return;
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ShowIfEnumAttribute attr = (ShowIfEnumAttribute)attribute;
            SerializedProperty enumProp = property.serializedObject.FindProperty(attr.enumFieldName);

            for (int i = 0; i < attr.enumValue.Length; i++)
            {
                if (enumProp != null && enumProp.enumValueIndex == attr.enumValue[i])
                    return EditorGUI.GetPropertyHeight(property, label, true);
            }
            return -EditorGUIUtility.standardVerticalSpacing;
        }
    }

    [CustomPropertyDrawer(typeof(Scripts.Interface.ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;               // disable editing
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = true;                // restore editing
        }
    }
    #endif
}