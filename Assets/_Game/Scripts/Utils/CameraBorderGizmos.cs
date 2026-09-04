using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Camera))]
public class CameraBorderGizmos : MonoBehaviour
{
    #if UNITY_EDITOR
    [SerializeField, InspectorReadOnly] private Camera cam;

    private void OnDrawGizmos()
    {
        if (cam == null)
            cam = GetComponent<Camera>();
        
        Gizmos.color = Color.green;
        Vector3 cameraPosition = cam.transform.position;
        Vector3 cameraSize = new Vector3(cam.orthographicSize * 2 * cam.aspect, cam.orthographicSize * 2, 0);
        Gizmos.DrawWireCube(cameraPosition, cameraSize);
    }
    #endif
}
