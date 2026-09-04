using UnityEngine;
using UnityEditor;

namespace CostCenter.RemoteConfig.Editor {
    [CustomEditor(typeof(CCRemoteConfig))]
    public class CCRemoteConfigEditor : UnityEditor.Editor
    {
        private CCRemoteConfig _rc;

        void OnEnable()
        {
            _rc = (CCRemoteConfig)target;
        }

        public override void OnInspectorGUI ()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Reset Default Values")) {
                _rc.ResetDefaultValues();
                EditorUtility.SetDirty(_rc);
            }
        }
    }
}
