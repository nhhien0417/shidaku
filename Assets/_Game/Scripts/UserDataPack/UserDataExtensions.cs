using System;
using _Game.UI.NotifyBadge;
using Design.Structures;
using UnityEngine;
using Item = UserDataPack.Structures.Item;

namespace UserDataPack
{
    public partial class UserDataExtensions // If there are more UserDataExtensions, rename this file to UserDataExtensions.Tracking
    {
        public static void TrackResourceItemAdded(Item item, int addedAmount, string source)
        {
            try
            {
                if (item != null)
                    Track.CostCenter.ResourceSource(item.Id, addedAmount, item.Amount, source);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to track resource item added. ItemId: {item?.Id}, AddedAmount: {addedAmount}, Source: {source}. Exception: {e}");
            }
        }
        
        public static void TrackResourceItemSpent(Item item, int spentAmount, string actionName)
        {
            try
            {
                if (item != null)
                    Track.CostCenter.ResourceSink(item.Id, spentAmount, item.Amount, actionName);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to track resource item spent. ItemId: {item?.Id}, SpentAmount: {spentAmount}, Action: {actionName}. Exception: {e}");
            }
        }
        
        public static void TrackNonConsumableItemAdded(string itemId, int addedAmount, int balance, string source)
        {
            Track.CustomEvent("item_added", new ()
            {
                { "item_id", itemId },
                { "amount", addedAmount },
                { "balance", balance },
                { "source", source },
            });
        }

        public static void TrackCollectionItemAdded(CollectionItem item, string source)
        {
            BadgeNotificationManager.Instance?.IncreaseNotification(BadgeNotificationType.GetNewCollectionItemNotifyType(item));
            
            Track.CustomEvent("collection_item_added", new ()
            {
                { "type", item.CollectionType },
                { "collection_id", item is SingleIdCollectionItem<int> sidITem ? sidITem.CollectionId : "unknown"},
                { "source", source },
            });
        }
    }
}