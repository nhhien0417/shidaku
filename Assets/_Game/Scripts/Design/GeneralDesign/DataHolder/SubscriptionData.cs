using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Design.Structures;

namespace Design.DataHolder
{
    [CreateAssetMenu(fileName = "SubscriptionData", menuName = "Design/SubscriptionData", order = 1)]
    [Serializable]
    public class SubscriptionData : ScriptableObject 
    {
        public List<Subscription> Items = new();
        
        public Subscription GetItemById(string id)
        {
            return Items.Find(item => item.Id == id);
        }
        
        public Subscription GetItemByProductId(string productId)
        {
            return Items.Find(item => item.WeeklyProductId.GetProductId() == productId || item.MonthlyProductId.GetProductId() == productId);
        }
    }
}
