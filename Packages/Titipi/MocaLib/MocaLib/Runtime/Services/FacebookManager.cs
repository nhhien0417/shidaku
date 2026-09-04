using Facebook.Unity;
using UnityEngine;

using Titipi.MocaLib.Runtime.Common;


namespace Titipi.MocaLib.Runtime.Services
{
    public class FacebookManager : MonoBehaviour
    {
        private const string TAG = "FacebookManager";

        public void Initialize()
        {
#if UNITY_WEBGL
            return;
#endif

            Utils.MocaLibLog(TAG, "Initialize");

            if (!FB.IsInitialized)
            {
                FB.Init(InitCallback);
            }
            else
            {
                FB.ActivateApp();
            }
        }

        private static void InitCallback()
        {
            if (FB.IsInitialized)
            {
                FB.ActivateApp();
            }
            else
            {
                Utils.MocaLibLogWarning(TAG, "Failed to initialize the Facebook SDK");
            }
        }
    }
}
