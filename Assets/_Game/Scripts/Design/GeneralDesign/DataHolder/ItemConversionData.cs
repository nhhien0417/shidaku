using System;
using System.Collections.Generic;
using UnityEngine;
using Design.Ids;
using Sirenix.OdinInspector;

namespace Design.DataHolder
{
    [Serializable]
    public class ItemConversionRule
    {
        [ValueDropdown("GetAllItemIds")]
        public string FromItemId;

        [ValueDropdown("GetAllItemIds")]
        public string ToItemId;

        public float Multiplier = 1f;

        public int CalculateConvertedAmount(int originalAmount)
        {
            return Mathf.RoundToInt(originalAmount * Multiplier);
        }

#if UNITY_EDITOR
        private string[] GetAllItemIds()
        {
            return ItemId.All;
        }
#endif
    }

    [CreateAssetMenu(fileName = "ItemConversionData", menuName = "Design/ItemConversionData")]
    public class ItemConversionData : ScriptableObject
    {
        public List<ItemConversionRule> ConversionRules = new();

        public bool TryGetConversion(string fromId, out ItemConversionRule rule)
        {
            rule = ConversionRules.Find(x => x.FromItemId == fromId);
            return rule != null;
        }

        public void GetConvertedItem(string originalId, int originalAmount, out string finalId, out int finalAmount)
        {
            if (TryGetConversion(originalId, out var rule))
            {
                finalId = rule.ToItemId;
                finalAmount = rule.CalculateConvertedAmount(originalAmount);
            }
            else
            {
                finalId = originalId;
                finalAmount = originalAmount;
            }
        }
    }
}