using System;
using System.Collections.Generic;
using UnityEngine;

#if MOCALIB_USE_GAMEANALYTICS
using GameAnalyticsSDK;
using _Game.Scripts.ResourcesAnalytics;
#endif

using Titipi.MocaLib.Runtime.Services;
using Titipi.MocaLib.Runtime.Services.Internal;

namespace _Game.Scripts.LiveOps
{
    public class FIAMPopupHandler : IFIAMPopupHandler
    {
        public void Process(PopupButton? button, Metadata? metadata)
        {
            if (button == null) return;

            var b = (PopupButton)button;
            switch (b.ActionType)
            {
                case PopupButtonActionType.Dismiss:
                    ProcessDismiss(b.ActionParam, metadata);
                    break;

                case PopupButtonActionType.OpenScreen:
                    ProcessOpenScreen(b.ActionParam, metadata);
                    break;

                case PopupButtonActionType.OpenURL:
                    ProcessOpenURL(b.ActionParam, metadata);
                    break;

                case PopupButtonActionType.RateApp:
                    ProcessRateApp(b.ActionParam, metadata);
                    break;

                case PopupButtonActionType.Purchase:
                    ProcessPurchase(b.ActionParam, metadata);
                    break;

                case PopupButtonActionType.PurchaseWithCoins:
                    ProcessPurchaseWithCoins(b.ActionParam, metadata);
                    break;

                case PopupButtonActionType.PurchaseWithGems:
                    ProcessPurchaseWithGems(b.ActionParam, metadata);
                    break;

                case PopupButtonActionType.ShowInterstitial:
                    ProcessShowInterstitial(b.ActionParam, metadata);
                    break;

                case PopupButtonActionType.ShowRewardedVideo:
                    ProcessShowRewardedVideo(b.ActionParam, metadata);
                    break;
            }
        }

        private void ProcessDismiss(string param, Metadata? metadata)
        {
        }

        private void ProcessOpenScreen(string param, Metadata? metadata)
        {
        }

        private void ProcessOpenURL(string param, Metadata? metadata)
        {
            if (string.IsNullOrEmpty(param)) return;

            Application.OpenURL(param);
        }

        private void ProcessRateApp(string param, Metadata? metadata)
        {
        }

        private void ProcessPurchase(string param, Metadata? metadata)
        {
            if (string.IsNullOrEmpty(param)) return;
            if (metadata == null) return;

            var meta = (Metadata)metadata;
            if (string.IsNullOrEmpty(meta.Rewards)) return;

            MocaLib.Instance.IAPManager.PurchaseProduct(param,
                onSuccess =>
                {
                    AddRewards(meta.Rewards, metadata.Value.CampaignName);
                },
                onError =>
                {
                    // UIManager.Instance.ShowPopUp("PurchaseFailed", PopUpShowBehaviour.KEEP_PREVIOUS);
                });
        }

        private void ProcessPurchaseWithCoins(string param, Metadata? metadata)
        {
            if (string.IsNullOrEmpty(param)) return;
            if (metadata == null) return;

            var meta = (Metadata)metadata;
            if (string.IsNullOrEmpty(meta.Rewards)) return;

            if (int.TryParse(param, out var price))
            {
//                 CoinBar.Instance.DecreaseCoin(price, onSuccessful: () =>
//                 {
//                     AddRewards(meta.Rewards, metadata.Value.CampaignName);

// #if MOCALIB_USE_GAMEANALYTICS
//                     GameAnalytics.NewResourceEvent(
//                         GAResourceFlowType.Sink,
//                         currency: nameof(GAResourceCurrency.Coin),
//                         amount: price,
//                         itemType: nameof(GAResourceItemType.FIAM),
//                         itemId: metadata.Value.CampaignName
//                     );
// #endif
//                 });
            }
        }

        private void ProcessPurchaseWithGems(string param, Metadata? metadata)
        {
        }

        private void ProcessShowInterstitial(string param, Metadata? metadata)
        {
        }

        private void ProcessShowRewardedVideo(string param, Metadata? metadata)
        {
            if (metadata == null) return;

            var meta = (Metadata)metadata;
            if (string.IsNullOrEmpty(meta.Rewards)) return;

            // AdController.Instance.ShowRewardedAd(metadata.Value.CampaignName, (success, adDuration) =>
            // {
            //     if (success)
            //     {
            //         AddRewards(meta.Rewards, metadata.Value.CampaignName);
            //     }
            // });
        }

        private Dictionary<string, int> ParseRewards(string data)
        {
            var rewards = new Dictionary<string, int>();

            var parts = data.Split(';');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                var keyValue = trimmed.Split(':');
                var key = keyValue[0];
                var v = keyValue[1];

                if (int.TryParse(v, out var value))
                {
                    rewards.Add(key, value);
                }
            }

            return rewards;
        }

        private void AddRewards(string data, string campaignName)
        {
            AudioManager.Instance.PlaySFX("Common_CashOut");

            var rewards = ParseRewards(data);

            foreach (var reward in rewards)
            {
                switch (reward.Key)
                {
                    case "RemoveAds":
                        AddAdsRemoval(reward.Value);
                        break;

//                     case "AddRevealHints":
//                         RewardManager.Instance.AddReward(RewardType.Reveal, reward.Value);
//                         break;

//                     case "AddClearHints":
//                         RewardManager.Instance.AddReward(RewardType.Clear, reward.Value);
//                         break;

//                     case "AddCoins":
//                         RewardManager.Instance.AddReward(RewardType.Coin, reward.Value);
//                         CoinBar.Instance.IncreaseCoin();

// #if MOCALIB_USE_GAMEANALYTICS
//                         GameAnalytics.NewResourceEvent(
//                             GAResourceFlowType.Source,
//                             currency: nameof(GAResourceCurrency.Coin),
//                             amount: reward.Value,
//                             itemType: nameof(GAResourceItemType.FIAM),
//                             itemId: campaignName
//                         );
// #endif
//                         break;
                }
            }
        }

        private void AddAdsRemoval(int duration)
        {
            // var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // long currentExpireTime = 0;

            // if (UserData.Instance.Data.NoAdsPurchaseTime != string.Empty)
            // {
            //     long.TryParse(UserData.Instance.Data.NoAdsPurchaseTime, out currentExpireTime);
            // }

            // var newExpireTime = nowUnix > currentExpireTime
            //     ? nowUnix + duration
            //     : currentExpireTime + duration;

            // UserProperty.SetAdsEnabled(false);
            // UserData.Instance.Data.NoAdsPurchaseTime = newExpireTime.ToString();
            // UserData.Instance.Data.IsAdsEnabled = false;
            // UserData.Instance.Save();

            // MainMenu.Instance?.CheckForRemoveAds();
        }
    }
}
