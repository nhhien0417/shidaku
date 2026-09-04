using UnityEngine;
using UnityEditor;

namespace CostCenter.Editor {
    [CustomEditor(typeof(CCServices))]
    public class CCServicesEditor: UnityEditor.Editor
    {
        public override void OnInspectorGUI ()
        {
            DrawDefaultInspector();
            CCServices manager = (CCServices)target;

            GUILayout.Space(10);

            if (GUILayout.Button("Add Attribution")) {
                manager.AddAttribution();
                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("Add Remote Config")) {
                manager.AddRemoteConfig();
                EditorUtility.SetDirty(manager);
            }
        }
    }
}
