using System;
using System.Collections.Generic;
using Design.Conditions;
using Design.Ids;
using Design.Structures;
using SimpleJSON;
using UnityEngine;
using ProductType = UnityEngine.Purchasing.ProductType;

namespace Design.DataHolder
{
    [CreateAssetMenu(fileName = "ShopItemData", menuName = "Design/ShopItemData", order = 1)]
    [Serializable]
    public class ShopItemData : ScriptableObject
    {
        [SerializeField] public ShopItemGroup FreeCoin = new() { Id = ShopItemId.FreeCoin };
        [SerializeField] public List<ShopItemGroup> Groups = new();
        public event Action OnRemoteConfigApplied;

        public IEnumerable<string> GroupIds
        {
            get
            {
                foreach (var group in Groups)
                    yield return group.Id;
            }
        }

        public IEnumerable<ShopItem> GetAllShopItems()
        {
            foreach (var item in FreeCoin.GetAllShopItems())
                yield return item;

            foreach (var group in Groups)
                foreach (var item in group.GetAllShopItems())
                    yield return item;
        }

        public ShopItem GetShopItem(string id)
        {
            foreach (var item in GetAllShopItems())
            {
                if (item.Id == id)
                    return item;
            }

            return null;
        }

        public ShopItem GetShopItemHas(string itemId, string priceId)
        {
            foreach (var shopItem in GetAllShopItems())
            {
                if (shopItem.Price.Id == priceId && shopItem.ItemsReward.Exists(item => item.Id == itemId))
                    return shopItem;
            }

            return null;
        }

        private ShopItemGroup FindGroup(string groupId)
        {
            if (FreeCoin.Id == groupId)
                return FreeCoin;

            return Groups.Find(group => group.Id == groupId);
        }

        public IReadOnlyList<ShopItem> GetGroupItems(string groupId)
        {
            return FindGroup(groupId)?.GetAllShopItems();
        }

        public bool IsGroupUnlocked(string groupId)
        {
            return FindGroup(groupId)?.IsUnlocked() == true;
        }

        public int GetRefreshTimesPerDay(string shopItemId)
        {
            if (FreeCoin.FindShopItem(shopItemId) != null)
                return FreeCoin.RefreshTimesPerDay;

            var group = Groups.Find(g => g.FindShopItem(shopItemId) != null);
            return group?.RefreshTimesPerDay ?? 1;
        }

        public int GetGroupRefreshTimesPerDay(string groupId)
        {
            return FindGroup(groupId)?.RefreshTimesPerDay ?? 0;
        }

        public void ApplyRemoteConfig(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            JSONNode shopNode;
            try
            {
                shopNode = JSON.Parse(json)?["shop"];
            }
            catch (Exception exception)
            {
                Debug.LogError($"[ShopItemData] Failed to parse remote config json, keep current data: {exception.Message}");
                return;
            }

            if (shopNode == null || !shopNode.IsObject)
            {
                Debug.LogError("[ShopItemData] Missing shop node in remote config, keep current data");
                return;
            }

            var usedIds = new HashSet<string>();
            foreach (var item in GetAllShopItems())
                usedIds.Add(item.Id);

            var freeCoinNode = shopNode["free_coin"];
            if (freeCoinNode == null || !freeCoinNode.IsObject)
            {
                Debug.LogError("[ShopItemData] Missing/invalid free_coin, keep current free_coin data");
            }
            else
            {
                usedIds.ExceptWith(FreeCoin.GetAllShopItems().ConvertAll(item => item.Id));
                FreeCoin = ParseGroup(ShopItemId.FreeCoin, freeCoinNode, FreeCoin, usedIds);
            }

            var groupsNode = shopNode["groups"];
            if (groupsNode == null || !groupsNode.IsObject)
            {
                Debug.LogError("[ShopItemData] Missing/invalid groups, keep current groups data");
                return;
            }

            var newGroups = new List<ShopItemGroup>();
            var seenGroupIds = new HashSet<string> { FreeCoin.Id };

            foreach (var groupNode in groupsNode)
            {
                var groupId = groupNode.Key;
                if (groupId == ShopItemId.FreeCoin)
                {
                    Debug.LogError("[ShopItemData] free_coin must be outside groups, skip entry");
                    continue;
                }

                if (!seenGroupIds.Add(groupId))
                {
                    Debug.LogError($"[ShopItemData] Duplicated group id {groupId}, skip entry");
                    continue;
                }

                var existingGroup = Groups.Find(g => g.Id == groupId);
                if (existingGroup != null)
                    usedIds.ExceptWith(existingGroup.GetAllShopItems().ConvertAll(item => item.Id));

                newGroups.Add(ParseGroup(groupId, groupNode.Value, existingGroup, usedIds));
            }

            Groups = newGroups;
            OnRemoteConfigApplied?.Invoke();
        }

        private static ShopItemGroup ParseGroup(string groupId, JSONNode node, ShopItemGroup existingGroup, HashSet<string> usedIds)
        {
            var group = new ShopItemGroup
            {
                Id = groupId,
                RefreshTimesPerDay = ParseRefreshTimesPerDay(node["refresh_times_per_day"], groupId, existingGroup?.RefreshTimesPerDay ?? 1),
                UnlockConditions = ParseConditions(node["unlock_condition"], $"group {groupId}", existingGroup?.UnlockConditions ?? new List<string>()),
            };

            var shopItemsNode = node["shop_items"];
            if (shopItemsNode == null || !shopItemsNode.IsArray)
            {
                Debug.LogError($"[ShopItemData] Invalid shop_items in group {groupId}, keep current items");
                group.ShopItems = existingGroup?.ShopItems ?? new List<ShopItem>();
                group.IapShopItems = existingGroup?.IapShopItems ?? new List<IapShopItem>();
                return group;
            }

            var seenItemIds = new HashSet<string>();
            foreach (JSONNode itemNode in shopItemsNode.AsArray)
            {
                var id = itemNode["id"].Value;
                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogError($"[ShopItemData] Missing shop item id in group {groupId}, skip entry");
                    continue;
                }

                if (!seenItemIds.Add(id) || !usedIds.Add(id))
                {
                    Debug.LogError($"[ShopItemData] Duplicated shop item id {id} in group {groupId}, skip entry");
                    continue;
                }

                var existingItem = existingGroup?.FindShopItem(id);
                var item = ParseShopItem(itemNode, id, existingItem);
                if (item == null)
                    continue;

                if (item is IapShopItem iapItem)
                    group.IapShopItems.Add(iapItem);
                else
                    group.ShopItems.Add(item);
            }

            return group;
        }

        private static ShopItem ParseShopItem(JSONNode node, string id, ShopItem existingItem)
        {
            var priceValue = node["price"].Value;
            if (!string.IsNullOrEmpty(priceValue) && priceValue.StartsWith(PriceId.IAP + "_", StringComparison.Ordinal))
                return ParseIapShopItem(node, id, priceValue, existingItem as IapShopItem);

            var price = ParsePrice(priceValue, id, existingItem?.Price);
            if (!price.HasValue)
            {
                Debug.LogError($"[ShopItemData] Cannot resolve price for shop item {id}, skip entry");
                return null;
            }

            var itemsReward = ParseRewards(node["items"], id, existingItem?.ItemsReward);
            if (itemsReward == null)
            {
                Debug.LogError($"[ShopItemData] Cannot resolve reward for shop item {id}, skip entry");
                return null;
            }

            return new ShopItem
            {
                Id = id,
                Name = ParseString(node["name"], existingItem?.Name ?? ""),
                IconId = ParseString(node["icon_id"], existingItem?.IconId ?? ""),
                Price = price.Value,
                PurchaseLimit = ParseNonNegativeInt(node["purchase_limit"], $"purchase_limit of {id}", existingItem?.PurchaseLimit ?? 0),
                DailyPurchaseLimit = ParseNonNegativeInt(node["daily_purchase_limit"], $"daily_purchase_limit of {id}", existingItem?.DailyPurchaseLimit ?? 0),
                ItemsReward = itemsReward,
                UnlockConditions = ParseConditions(node["unlock_condition"], $"shop item {id}", existingItem?.UnlockConditions ?? new List<string>()),
            };
        }

        private static IapShopItem ParseIapShopItem(JSONNode node, string id, string priceValue, IapShopItem existingItem)
        {
            if (IsBuiltInIapShopItem(id))
            {
                Debug.LogError($"[ShopItemData] IAP shop item id '{id}' is reserved for a build-time item in IapShopItemData, skip entry");
                return null;
            }

            var productId = ParseIapProductId(priceValue, id, existingItem?.ProductId);
            if (!productId.HasValue)
            {
                Debug.LogError($"[ShopItemData] Cannot resolve product id for IAP shop item {id}, skip entry");
                return null;
            }

            var itemsReward = ParseRewards(node["items"], id, existingItem?.ItemsReward);
            if (itemsReward == null)
            {
                Debug.LogError($"[ShopItemData] Cannot resolve reward for IAP shop item {id}, skip entry");
                return null;
            }

            return new IapShopItem
            {
                Id = id,
                Name = ParseString(node["name"], existingItem?.Name ?? ""),
                IconId = ParseString(node["icon_id"], existingItem?.IconId ?? ""),
                PurchaseLimit = ParseNonNegativeInt(node["purchase_limit"], $"purchase_limit of {id}", existingItem?.PurchaseLimit ?? 0),
                DailyPurchaseLimit = ParseNonNegativeInt(node["daily_purchase_limit"], $"daily_purchase_limit of {id}", existingItem?.DailyPurchaseLimit ?? 0),
                UnlockConditions = ParseConditions(node["unlock_condition"], $"shop item {id}", existingItem?.UnlockConditions ?? new List<string>()),
                ItemsReward = itemsReward,
                ProductId = productId.Value,
                ProductType = ProductType.Consumable,
            };
        }

        private static IapProductId? ParseIapProductId(string priceValue, string shopItemId, IapProductId? fallbackValue)
        {
            var productId = priceValue[(PriceId.IAP.Length + 1)..];
            if (string.IsNullOrEmpty(productId))
            {
                Debug.LogError($"[ShopItemData] Invalid product id in price '{priceValue}' for IAP shop item {shopItemId}, fallback to current product id");
                return fallbackValue;
            }

            return new IapProductId { IOSId = productId, AndroidId = productId };
        }

        private static bool IsBuiltInIapShopItem(string id)
        {
            return DesignDataHolder.Instance?.IapShopItemData?.Items.Exists(item => item.Id == id) == true;
        }

        private static List<Item> ParseRewards(JSONNode node, string shopItemId, List<Item> fallbackValue)
        {
            if (node == null || !node.IsArray || node.Count == 0)
            {
                Debug.LogError($"[ShopItemData] Missing items for shop item {shopItemId}, fallback to current reward");
                return fallbackValue;
            }

            var rewards = new List<Item>();
            foreach (JSONNode rewardNode in node.AsArray)
            {
                var rewardId = rewardNode["id"].Value;
                var amount = rewardNode["amount"].AsInt;
                if (Array.IndexOf(ItemId.All, rewardId) < 0 || amount <= 0)
                {
                    Debug.LogError($"[ShopItemData] Invalid reward entry in shop item {shopItemId}, skip entry");
                    continue;
                }

                rewards.Add(new Item { Id = rewardId, Amount = amount });
            }

            if (rewards.Count == 0)
            {
                Debug.LogError($"[ShopItemData] No valid reward entries for shop item {shopItemId}, fallback to current reward");
                return fallbackValue;
            }

            return rewards;
        }

        private static Price? ParsePrice(string value, string shopItemId, Price? fallbackValue)
        {
            var separatorIndex = value?.LastIndexOf('_') ?? -1;
            if (separatorIndex <= 0 || !int.TryParse(value[(separatorIndex + 1)..], out var amount) || amount < 0)
            {
                Debug.LogError($"[ShopItemData] Invalid price '{value}' for shop item {shopItemId}, fallback to current price");
                return fallbackValue;
            }

            var priceId = value[..separatorIndex];
            if (priceId == PriceId.IAP || Array.IndexOf(PriceId.All, priceId) < 0)
            {
                Debug.LogError($"[ShopItemData] Unsupported price id '{priceId}' for shop item {shopItemId}, fallback to current price");
                return fallbackValue;
            }

            return new Price { Id = priceId, Value = amount };
        }

        private static int ParseRefreshTimesPerDay(JSONNode node, string groupId, int fallbackValue)
        {
            if (node == null)
                return fallbackValue;

            var value = node.AsInt;
            if (value == 0)
                return 0;

            if (value < 0 || value > 24 || 24 % value != 0)
            {
                Debug.LogError($"[ShopItemData] Invalid refresh_times_per_day in group {groupId}, fallback to {fallbackValue}");
                return fallbackValue;
            }

            return value;
        }

        private static string ParseString(JSONNode node, string fallbackValue)
        {
            return node == null ? fallbackValue : node.Value;
        }

        private static int ParseNonNegativeInt(JSONNode node, string context, int fallbackValue)
        {
            if (node == null)
                return fallbackValue;

            var value = node.AsInt;
            if (value < 0)
            {
                Debug.LogError($"[ShopItemData] Invalid {context}, fallback to {fallbackValue}");
                return fallbackValue;
            }

            return value;
        }

        private static List<string> ParseConditions(JSONNode node, string context, List<string> fallbackValue)
        {
            if (node == null)
                return fallbackValue;

            if (!node.IsArray)
            {
                Debug.LogError($"[ShopItemData] Invalid unlock_condition in {context}, fallback to current conditions");
                return fallbackValue;
            }

            var conditions = new List<string>();
            foreach (JSONNode conditionNode in node.AsArray)
            {
                var value = conditionNode.Value;
                if (!ConditionHelper.TryParseToConditions(value, out _))
                {
                    Debug.LogError($"[ShopItemData] Invalid condition '{value}' in {context}, skip entry");
                    continue;
                }

                conditions.Add(value);
            }

            return conditions;
        }
    }
}
