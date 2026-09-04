using System.Collections.Generic;
using UnityEngine;

namespace Titipi.MocaLib.Runtime.Services.Internal
{
    [CreateAssetMenu(fileName = "AdConfig", menuName = "MocaLib/AdConfig")]
    public class AdConfig : ScriptableObject
    {
        [Space(10)]
        public string AppKey;

        [Space(10)]
        public bool IsAppOpenAdEnabled;
        public List<string> AppOpenAdIds;

        [Space(10)]
        public bool IsBannerAdEnabled;
        public List<string> BannerAdIds;

        [Space(10)]
        public bool IsInterstitialAdEnabled;
        public List<string> InterstitialAdIds;

        [Space(10)]
        public bool IsRewardedAdEnabled;
        public List<string> RewardedAdIds;
        
        [Tooltip("Minimum number of seconds between 2 interstitial ads")]
        public int InterstitialInterval = 30;
        
        [HideInInspector]
        public string UserId;
    }
}
