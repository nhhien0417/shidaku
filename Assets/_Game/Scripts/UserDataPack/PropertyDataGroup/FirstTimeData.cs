using System;
using System.Collections.Generic;
using UnityEngine;

namespace UserDataPack.PropertyDataGroup
{
    [Serializable]
    public class FirstTimeData : IUserDataPropertyDataGroup
    {
        public bool FirstTimeAutoX = true;
        [SerializeField] private List<string> _shownBoosterTutorials = new();
        [SerializeField] private List<string> _claimedUnlockBoosterRewards = new();

        public void FixData()
        {
            if (_shownBoosterTutorials == null)
            {
                _shownBoosterTutorials = new();
            }
            if (_claimedUnlockBoosterRewards == null)
            {
                _claimedUnlockBoosterRewards = new();
            }
        }

        public void UseAutoX()
        {
            FirstTimeAutoX = false;
        }

        public bool HasShownBoosterTutorial(string boosterId)
        {
            return _shownBoosterTutorials.Contains(boosterId);
        }

        public void MarkBoosterTutorialShown(string boosterId)
        {
            if (!_shownBoosterTutorials.Contains(boosterId))
            {
                _shownBoosterTutorials.Add(boosterId);
            }
        }

        public bool HasClaimedUnlockBoosterReward(string boosterId)
        {
            return _claimedUnlockBoosterRewards.Contains(boosterId);
        }

        public void MarkUnlockBoosterRewardsClaimed(string boosterId)
        {
            if (!_claimedUnlockBoosterRewards.Contains(boosterId))
            {
                _claimedUnlockBoosterRewards.Add(boosterId);
            }
        }
    }
}
