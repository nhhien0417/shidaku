using UnityEngine;
using UnityEditor;

namespace CostCenter.Editor {
    public static class CCGUIMenu
    {
        [MenuItem ("GameObject/Cost Center/Create Manager")]
        public static void CreateManagerGameObject() {
            GameObject go = new GameObject("CostCenter");
            go.AddComponent<CCFirebase>();
            go.AddComponent<CCServices>();
            go.transform.position = Vector3.zero;
            EditorUtility.SetDirty(go);
        }
    }
}
