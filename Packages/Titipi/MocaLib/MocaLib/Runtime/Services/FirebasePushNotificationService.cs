using System.Collections;
using UnityEngine;

using Titipi.MocaLib.Runtime.Common;
using Titipi.MocaLib.Runtime.Services.Internal;

namespace Titipi.MocaLib.Runtime.Services
{
    public class FirebasePushNotificationService : MonoBehaviour, IPushNotificationService
    {
        private const string TAG = "FirebasePushNotificationService";

        public void Initialize()
        {
#if !UNITY_WEBGL
            Utils.MocaLibLog(TAG, "Initialize");
            StartCoroutine(InitializeFirebaseMessaging());
#endif
        }

#if !UNITY_WEBGL
        private IEnumerator InitializeFirebaseMessaging()
        {
            yield return new WaitForEndOfFrame();

#if UNITY_IOS
            Firebase.Messaging.FirebaseMessaging.TokenRegistrationOnInitEnabled = true;
#endif

            Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
            Firebase.Messaging.FirebaseMessaging.MessageReceived += OnMessageReceived;
        }

        private void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
        {
            Utils.MocaLibLog(TAG, $"OnTokenReceived -> Received Registration Token: {token.Token}");
        }

        private void OnMessageReceived(object sender, Firebase.Messaging.MessageReceivedEventArgs e)
        {
            Utils.MocaLibLog(TAG, $"OnMessageReceived -> Received a new message from: {e.Message.From}");
        }
#endif
    }
}
