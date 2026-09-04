using UnityEngine;

using Titipi.MocaLib.Runtime.Common;
using Titipi.MocaLib.Runtime.Services.Internal;

namespace Titipi.MocaLib.Runtime.Services
{
    public class PushNotificationManager : MonoBehaviour
    {
        private const string TAG = "PushNotificationManager";

        private IPushNotificationService _service;

        public void Initialize(IPushNotificationService service)
        {
#if UNITY_WEBGL
            return;
#endif

            Utils.MocaLibLog(TAG, "Initialize");

            _service = service;
        }

        public void RegisterForPushNotifications()
        {
            Utils.MocaLibLog(TAG, "RegisterForPushNotifications");

            var permissionHandler = gameObject.AddComponent<PushNotificationPermissionHandler>();

            permissionHandler.OnPermissionGrantedAction = () => { _service.Initialize(); };
            permissionHandler.OnPermissionDeniedAction = () =>
            {
                // Debug.Log("Handle denied gracefully (e.g. disable push features).");
            };

            permissionHandler.RequestNotificationPermission();
        }
    }
}
