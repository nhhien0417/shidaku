using System;
using System.Collections.Generic;
using UnityEngine;
using UserDataPack.Structures;

namespace UserDataPack.PropertyDataGroup
{
    [Serializable]
    public class SubscriptionHolder : IUserDataPropertyDataGroup
    {
        public List<SubscriptionUserData> Subscriptions = new();

        public void FixData()
        {
            if (Subscriptions == null)
            {
                Subscriptions = new();
            }
        }

        // public bool UpdateSubscription(SubscriptionInfoCustom subscriptionInfo, string receipt)
        // {
        //     var prdId = subscriptionInfo.getProductId();
        //     var design = DesignData.Instance?.SubscriptionData?.GetItemByProductId(prdId);
        //     if (design != null)
        //     {
        //         if (subscriptionInfo.isSubscribed() == Result.True && subscriptionInfo.isExpired() == Result.False && subscriptionInfo.getRemainingTime().TotalSeconds > 0)
        //         {
        //             var subsData = ActiveSubscription(design, subscriptionInfo.getPurchaseDate(), subscriptionInfo.getExpireDate(), receipt);
        //             return ProcessReward(subsData);
        //         }
        //         else
        //         {
        //             DeactiveSubscription(design);
        //         }
        //     }
        //     return false;
        // }
        //
        // public SubscriptionUserData ActiveSubscription(Subscription subscription, DateTime activeDate, DateTime expiredDate, string receipt)
        // {
        //     var subs = ActiveSubscriptions.Find(sub => sub.SubscriptionId == subscription.Id);
        //     if (subs == null)
        //     {
        //         subs = new SubscriptionUserData
        //         {
        //             SubscriptionId = subscription.Id,
        //             ActiveDate = activeDate.ToTicksAsString(),
        //             ExpiredDate = expiredDate.ToTicksAsString(),
        //             LastTimeReward = activeDate.ToTicksAsString(),
        //             Receipt = receipt,
        //             IsActivated = true,
        //             UnRewardRemainDays = 0
        //         };
        //         ActiveSubscriptions.Add(subs);
        //     }
        //     else
        //     {
        //         if (subs.Receipt != receipt) // Reset reward time if has new receipt
        //         {
        //             ProcessReward(subs);
        //             subs.ActiveDate = activeDate.ToTicksAsString();
        //             subs.ExpiredDate = expiredDate.ToTicksAsString();
        //             subs.LastTimeReward = activeDate.AddDays(-subs.UnRewardRemainDays).ToTicksAsString();
        //             subs.UnRewardRemainDays = 0;
        //             subs.Receipt = receipt;
        //         }
        //         subs.IsActivated = true;
        //     }
        //     
        //     return subs;
        // }
        //
        // public void DeactiveSubscription(Subscription subscription)
        // {
        //     var subs = ActiveSubscriptions.Find(sub => sub.SubscriptionId == subscription.Id);
        //     DeactiveSubscription(subs);
        // }
        //
        // public void DeactiveSubscription(SubscriptionUserData subscriptionData)
        // {
        //     if (subscriptionData != null)
        //     {
        //         ProcessReward(subscriptionData);
        //         BlockGemsUserData.Instance.ScoreMultiplier.DeactiveSubscriptionMultiplier(subscriptionData.SubscriptionId);
        //         subscriptionData.IsActivated = false;
        //         
        //         Debug.Log("[BlockGemsUserData] DeactiveSubscription: ");
        //         subscriptionData.PrintDebug("[BlockGemsUserData] ");
        //     }
        // }
        //
        // public bool IsSubscriptionActivated(string subscriptionId)
        // {
        //     var subs = ActiveSubscriptions.Find(sub => sub.SubscriptionId == subscriptionId);
        //     return subs?.IsActivated ?? false;
        // }
        //
        // public bool ProcessReward(SubscriptionUserData subscriptionData)
        // {
        //     if (subscriptionData == null)
        //         return false;
        //     
        //     var design = DesignData.Instance?.SubscriptionData?.GetItemById(subscriptionData.SubscriptionId);
        //     if (design is { RewardIntervalDays: > 0 })
        //     {
        //         var now = DateTimeManager.Now;
        //         var expiredDate = subscriptionData.ExpiredDate.ToDateTime();
        //         var lastTimeReward = subscriptionData.LastTimeReward.ToDateTime();
        //         var timePassed = now < expiredDate ? now - lastTimeReward : expiredDate - lastTimeReward;
        //         var daysPassed = timePassed.Days;
        //         var rewardCount = daysPassed / design.RewardIntervalDays;
        //         var residualDays = daysPassed % design.RewardIntervalDays;
        //         
        //         if (rewardCount > 0)
        //         {
        //             var userData = BlockGemsUserData.Instance;
        //             for (var i = 0; i < rewardCount; i++)
        //             {
        //                 userData.AddSubscriptionRewards(design);
        //             }
        //
        //             subscriptionData.UnRewardRemainDays = residualDays;
        //             subscriptionData.LastTimeReward = lastTimeReward.AddDays(daysPassed-residualDays).ToTicksAsString();
        //             
        //             return true;
        //         }
        //     }
        //
        //     return false;
        // }
    }
}