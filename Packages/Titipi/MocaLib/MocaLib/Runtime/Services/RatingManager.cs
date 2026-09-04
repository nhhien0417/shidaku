using System.Collections;
using UnityEngine;

#if UNITY_IOS
using System;
using UnityEngine.iOS;
#endif

#if UNITY_ANDROID
using Google.Play.Review;
#endif

using Titipi.MocaLib.Runtime.Common;

namespace Titipi.MocaLib.Runtime.Services
{
    public class RatingManager: MonoSingleton<RatingManager>
    {
        private const string TAG = "RatingManager";

        private string _iOSAppId;
        private bool _useInAppRating;

        private bool _ratingShown;
        private const string RatingShownKey = "Moca_RatingShown";

        public void Initialize(string iOSAppId, bool useInAppRating)
        {
#if UNITY_WEBGL
            return;
#endif
            
            Utils.MocaLibLog(TAG, "Initialize");

            _iOSAppId = iOSAppId;
            _useInAppRating = useInAppRating;

            _ratingShown = PlayerPrefs.GetInt(RatingShownKey, 0) == 1;
        }

        public void Show()
        {
#if UNITY_WEBGL
            return;
#endif

            if (_ratingShown) return;

            _ratingShown = true;
            PlayerPrefs.SetInt(RatingShownKey, 1);
            PlayerPrefs.Save();

            Utils.MocaLibLog(TAG, "Show");

            if (_useInAppRating)
            {
#if UNITY_IOS
		        Device.RequestStoreReview();
#elif UNITY_ANDROID
                RequestStoreReview();
#endif
            }
            else
            {
#if UNITY_IOS
                Application.OpenURL($"itms-apps://itunes.apple.com/app/{_iOSAppId}");
#elif UNITY_ANDROID
                Application.OpenURL("market://details?id=" + Application.identifier);
#endif
            }
        }

#if UNITY_ANDROID
        private void RequestStoreReview()
        {
            StopCoroutine(nameof(CreateGoogleStoreReview));
            StartCoroutine(nameof(CreateGoogleStoreReview));
        }

        private IEnumerator CreateGoogleStoreReview()
        {
            var reviewManager = new ReviewManager();
            var requestFlowOperation = reviewManager.RequestReviewFlow();

            yield return requestFlowOperation;

            if (requestFlowOperation.Error != ReviewErrorCode.NoError)
            {
                yield break;
            }

            var playReviewInfo = requestFlowOperation.GetResult();
            var launchFlowOperation = reviewManager.LaunchReviewFlow(playReviewInfo);

            yield return launchFlowOperation;

            if (launchFlowOperation.Error != ReviewErrorCode.NoError)
            {
                Application.OpenURL("market://details?id=" + Application.identifier);
            }
        }
#endif
    }
}
