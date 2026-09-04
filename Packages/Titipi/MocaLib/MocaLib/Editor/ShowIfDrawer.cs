using UnityEngine;
using UnityEditor;

namespace Titipi.MocaLib.Editor
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer
    {
        // Checks whether the property should be displayed based on the condition
        private bool ShouldShow(SerializedProperty property)
        {
            ShowIfAttribute showIf = attribute as ShowIfAttribute;
            
            // Build sibling path: replace last segment with the condition field name
            string propertyPath = property.propertyPath;
            string conditionPath = propertyPath.Contains(".")
                ? propertyPath.Substring(0, propertyPath.LastIndexOf('.')) + "." + showIf.conditionFieldName
                : showIf.conditionFieldName;
            
            // Find the condition property within the same serialized object
            SerializedProperty conditionProperty = property.serializedObject.FindProperty(conditionPath);

            if (conditionProperty == null)
            {
                Debug.LogWarning($"[ShowIf] Could not find field: {showIf.conditionFieldName}");
                return true;
            }

            // Compare the values based on the condition property's type
            switch (conditionProperty.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return conditionProperty.boolValue.Equals(showIf.expectedValue);
                case SerializedPropertyType.Enum:
                    return conditionProperty.enumValueIndex.Equals((int)showIf.expectedValue);
                case SerializedPropertyType.Integer:
                    return conditionProperty.intValue.Equals((int)showIf.expectedValue);
                case SerializedPropertyType.Float:
                    return conditionProperty.floatValue.Equals((float)showIf.expectedValue);
                case SerializedPropertyType.String:
                    return conditionProperty.stringValue.Equals((string)showIf.expectedValue);
                default:
                    Debug.LogWarning($"[ShowIf] Unsupported property type: {conditionProperty.propertyType}");
                    return true;
            }
        }

        // Calculates the property height (returns 0 or negative spacing if hidden)
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
            // Subtract standard vertical spacing so the hidden field collapses completely
            return -EditorGUIUtility.standardVerticalSpacing;
        }

        // Draws the property on the Inspector
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }
    }
}