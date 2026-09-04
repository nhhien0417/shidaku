using System.Collections;
using Design.Ids;
using UnityEngine;
using UserDataPack;
using System.Threading.Tasks;

namespace _Game.Scripts.Utils
{
    public class InternetCheckerWithRemoteConfig : InternetChecker
    {
        public static bool LocalRequireInternet = true;
        public static int RemoteRequireInternetConfig = 1;

        public static bool RemoteRequireInternet
        {
            get
            {
                switch (RemoteRequireInternetConfig)
                {
                    case 0: return false;
                    case 2: return true;

                    case 1:
                        return !UserData.Instance.NoAdsActivated();

                    default:
                        return false;
                }
            }
        }
        public static bool RequireInternet => LocalRequireInternet || RemoteRequireInternet;
        public static bool AlwaysRequireInternet => RequireInternet && RemoteRequireInternetConfig == 2;
        public static bool ShouldShowNoInternetPopup => RequireInternet && !(Instance?.IsInternetAvailable ?? true);

        private Coroutine _recheckCoroutine;

        public async Task ApplyRemoteConfig(int requireInternetConfig)
        {
            RemoteRequireInternetConfig = requireInternetConfig;

            var timeout = 20f;
            while (!DateTimeManager.IsUpToDate && timeout > 0f)
            {
                await Task.Yield();
                timeout -= Time.deltaTime;
            }

            UpdateRequireInternetStatus();
        }

        protected override IEnumerator Start()
        {
            var userData = UserData.Instance;
            if (userData.SecuredData.GetNonconsumableItemAmount(ItemId.NoAds) > 0)
            {
                LocalRequireInternet = false;
            }

            if (checkOnStart)
                TriggerPing();

            UserData.Instance.OnNonconsumableItemChanged += OnNonconsumableItemChanged;

            var timeout = 20f;
            while (!DateTimeManager.IsUpToDate && timeout > 0f)
            {
                yield return new WaitForEndOfFrame();
                timeout -= Time.deltaTime;
            }

            UpdateRequireInternetStatus();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_recheckCoroutine != null)
                StopCoroutine(_recheckCoroutine);

            UserData.Instance.OnNonconsumableItemChanged -= OnNonconsumableItemChanged;
        }

        protected override void OnApplicationFocus(bool hasFocus)
        {
            if (!RequireInternet)
                return;

            base.OnApplicationFocus(hasFocus);
        }

        protected override void OnApplicationPause(bool paused)
        {
            if (!RequireInternet)
                return;

            base.OnApplicationPause(paused);
        }

        protected void UpdateRequireInternetStatus()
        {
            var userData = UserData.Instance;
            if (userData.NoAdsActivated())
            {
                LocalRequireInternet = false;

                var remainTime = userData.SecuredData.NoAdsLimitedTimeData.GetRemainingSeconds();
                if (remainTime > 0)
                {
                    if (_recheckCoroutine != null)
                        StopCoroutine(_recheckCoroutine);
                    _recheckCoroutine = StartCoroutine(RecheckAfterSeconds(remainTime));
                }
            }
            else
            {
                LocalRequireInternet = true;
            }

            if (RequireInternet)
                StartChecker();
            else
                StopChecker();
        }

        protected IEnumerator RecheckAfterSeconds(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            UpdateRequireInternetStatus();
        }

        protected void OnNonconsumableItemChanged(string itemId, int amount)
        {
            switch (itemId)
            {
                case ItemId.NoAds:
                    {
                        var userData = UserData.Instance;
                        if (userData.SecuredData.GetNonconsumableItemAmount(ItemId.NoAds) > 0)
                        {
                            LocalRequireInternet = false;
                        }

                        if (RequireInternet)
                            StartChecker();
                        else
                            StopChecker();
                    }
                    break;

                case ItemId.NoAdsLimitedTime:
                case ItemId.NoAds_7Days:
                case ItemId.NoAds_24h:
                    UpdateRequireInternetStatus();
                    break;
            }
        }
    }
}