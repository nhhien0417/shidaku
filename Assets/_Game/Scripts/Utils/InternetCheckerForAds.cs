using System.Collections;
using Design.Ids;
using UnityEngine;
using UserDataPack;

namespace _Game.Scripts.Utils
{
    public class InternetCheckerForAds : InternetChecker
    {
        public static bool RequireInternet = true;

        public static bool ShouldShowNoInternetPopup => RequireInternet && !(Instance?.IsInternetAvailable ?? true);
        
        private Coroutine _recheckCoroutine;

        protected override IEnumerator Start()
        {
            var userData = UserData.Instance;
            if (userData.SecuredData.GetNonconsumableItemAmount(ItemId.NoAds) > 0)
            {
                RequireInternet = false;
                Destroy(gameObject);
                yield break;
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
                RequireInternet = false;
                StopChecker();

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
                RequireInternet = true;
                StartChecker();
            }
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
                            RequireInternet = false;
                            Destroy(gameObject);
                        }
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