using UnityEngine;
using UnityEditor;

namespace CostCenter.Attribution.Editor {
    // [CustomEditor(typeof(CCAttribution))]
    public class CCAttributionEditor : UnityEditor.Editor
    {
        private CCAttribution _att;
        // private bool _isShowAdapters = false;

        // void OnEnable()
        // {
        //     _att = (CCAttribution)target;
        //     _isShowAdapters = false;

        //     // Properties
        //     _IsRectTransform = serializedObject.FindProperty("IsRectTransform");
        // }

        // public override void OnInspectorGUI ()
        // {
        //     DrawDefaultInspector();

        //     _isShowAdapters = EditorGUILayout.Foldout(_isShowAdapters, "MMP Adapters");
        //     if (_isShowAdapters)
        //     {
        //         if (GUILayout.Button("Add Appsflyer Adapter")) {
        //             manager.AddAttribution();
        //             EditorUtility.SetDirty(manager);
        //         }
        //     }
        // }
    }
}
