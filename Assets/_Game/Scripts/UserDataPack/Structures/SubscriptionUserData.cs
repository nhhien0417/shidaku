using System;
using UnityEngine;

namespace UserDataPack.Structures
{
    [Serializable]
    public class SubscriptionUserData
    {
        public string SubscriptionId;
        public string ActiveDate;
        public string ExpiredDate;
        public string LastTimeReward;
        public string Receipt;
        public bool IsActivated;
        public int UnRewardRemainDays;

        public void PrintDebug(string prefix = "")
        {
            Debug.Log($"{prefix}SubscriptionId: {SubscriptionId}");
            Debug.Log($"{prefix}ActiveDate: {ActiveDate.ToDateTime()}");
            Debug.Log($"{prefix}ExpiredDate: {ExpiredDate.ToDateTime()}");
            Debug.Log($"{prefix}LastTimeReward: {LastTimeReward.ToDateTime()}");
            Debug.Log($"{prefix}Receipt: {Receipt}");
            Debug.Log($"{prefix}IsActivated: {IsActivated}");
        }
    }
}
