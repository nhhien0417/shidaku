using System;
using System.Linq;
using Common.Comparison;
using Design.Ids;
using Sirenix.OdinInspector;
using UnityEngine;
using UserDataPack;

namespace Design.Conditions
{
    public class ItemAmountCondition : Condition
    {
        [SerializeField][ValueDropdown("GetAllItemIds")] private string _itemId;
        [SerializeField] private int _amount;
        [SerializeField] private ComparisonType _comparison;

        public ItemAmountCondition()
        {

        }

        public ItemAmountCondition(string itemId, int amount, ComparisonType comparison)
        {
            _itemId = itemId;
            _amount = amount;
            _comparison = comparison;
        }

        public override bool IsMet()
        {
            var currentItemAmount = UserData.Instance.GetItemAmount(_itemId);
            return _comparison.GetResult(currentItemAmount, _amount);
        }

#if UNITY_EDITOR
        private string[] GetAllItemIds()
        {
            return ItemId.All.ToArray();
        }
#endif
    }
}