using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LayerSelectorAttribute : PropertyAttribute
{
    
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(LayerSelectorAttribute))]
public class LayerSelectorAttributeEditor : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        property.intValue = EditorGUI.LayerField(position, label,  property.intValue);
    }
}
#endif