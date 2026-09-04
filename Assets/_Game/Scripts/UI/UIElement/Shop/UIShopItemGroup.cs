using System;
using System.Collections.Generic;
using System.Globalization;
using _Game.UI.NotifyBadge;
using Design;
using Design.Ids;
using Design.Structures;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class UIShopItemGroup : MonoBehaviour
{
    [SerializeField] private string _groupId;
    [SerializeField, ValueDropdown("GetAllShopItemIds")] private List<string> _shopItemIds;
    [SerializeField] private CustomGridLayout _customGridLayout;
    [SerializeField] private UIShopItem _shopItemPrefab;
    [SerializeField] private UIShopItem _bundleShopItemPrefab;
    [SerializeField] private bool _hideIfNoItemAvailable;
    [SerializeField] private List<UIElement> _extraUIElements = new();
    [SerializeField] private TextMeshProUGUI _txtRefresh;

    private HashSet<string> _itemIdsSet = new();
    private readonly Dictionary<string, UIShopItem> _shopItemsById = new();

    public string GroupId => _groupId;

    public void SetGroupId(string groupId)
    {
        _groupId = groupId;
    }

    public bool HasItem(string id)
    {
        return _itemIdsSet.Contains(id);
    }

    public void UpdateUI()
    {
        var shopItemData = DesignDataHolder.Instance?.NormalShopItemData;
        if (shopItemData != null && !string.IsNullOrEmpty(_groupId) && !shopItemData.IsGroupUnlocked(_groupId))
        {
            gameObject.SetActive(false);
            return;
        }

        var hasAvailableItem = false;
        var hasUnlockedItem = false;
        foreach (var shopItem in _shopItemsById.Values)
        {
            shopItem.UpdateUI();

            if (!hasAvailableItem && shopItem.IsAvailable())
            {
                hasAvailableItem = true;
                hasUnlockedItem = true;
            }

            if (!hasUnlockedItem && shopItem.IsUnlocked())
                hasUnlockedItem = true;
        }

        _customGridLayout.RefreshRowsVisibility();

        foreach (var extraElement in _extraUIElements)
        {
            extraElement.UpdateUI();
        }

        UpdateRefreshNote();

        gameObject.SetActive((!_hideIfNoItemAvailable && hasUnlockedItem) || hasAvailableItem);
    }

    private void UpdateRefreshNote()
    {
        if (_txtRefresh == null)
            return;

        var text = GetRefreshNoteText();
        _txtRefresh.gameObject.SetActive(!string.IsNullOrEmpty(text));
        _txtRefresh.text = text;
    }

    private string GetRefreshNoteText()
    {
        if (string.IsNullOrEmpty(_groupId) || !DateTimeManager.IsUpToDate)
            return "";

        var refreshTimesPerDay = DesignDataHolder.Instance?.NormalShopItemData?.GetGroupRefreshTimesPerDay(_groupId) ?? 0;
        if (refreshTimesPerDay <= 0)
            return "";

        var slotHours = 24.0 / refreshTimesPerDay;
        var now = DateTimeManager.Now;
        var slotIndex = (int)(now.TimeOfDay.TotalHours / slotHours);
        var nextRefreshUtc = now.Date.AddHours(slotHours * (slotIndex + 1));
        var nextRefreshLocal = TimeZoneInfo.ConvertTimeFromUtc(nextRefreshUtc, TimeZoneInfo.Local);

        return $"Restocks at {TMPHelper.WithCustomFont(nextRefreshLocal.ToString("h:mm tt", CultureInfo.InvariantCulture), "#FFFFFF")}";
    }

    public void Initialize(string placement)
    {
        var designDataHolder = DesignDataHolder.Instance;
        if (designDataHolder == null)
        {
            Debug.LogError("DesignDataHolder instance is null");
            return;
        }

        var seenIds = new HashSet<string>();
        _itemIdsSet.Clear();

        foreach (var shopItem in GetShopItems(designDataHolder))
        {
            seenIds.Add(shopItem.Id);
            foreach (var reward in shopItem.ItemsReward)
                _itemIdsSet.Add(reward.Id);

            if (_shopItemsById.TryGetValue(shopItem.Id, out var existingInstance))
                _ = existingInstance.SetData(shopItem, placement);
            else
                AddShopItem(shopItem, placement);
        }

        RemoveStaleShopItems(seenIds);

        foreach (var extraElement in _extraUIElements)
        {
            extraElement.Placement = placement;
        }
    }

    private void RemoveStaleShopItems(HashSet<string> currentIds)
    {
        List<string> staleIds = null;
        foreach (var id in _shopItemsById.Keys)
        {
            if (!currentIds.Contains(id))
                (staleIds ??= new List<string>()).Add(id);
        }

        if (staleIds == null)
            return;

        foreach (var id in staleIds)
        {
            var instance = _shopItemsById[id];
            _customGridLayout.Remove(instance.transform);
            Destroy(instance.gameObject);
            _shopItemsById.Remove(id);
        }
    }

    private IEnumerable<ShopItem> GetShopItems(DesignDataHolder designDataHolder)
    {
        var shopItemData = designDataHolder.NormalShopItemData;
        if (!string.IsNullOrEmpty(_groupId))
        {
            var groupItems = shopItemData.GetGroupItems(_groupId);
            if (groupItems != null)
            {
                foreach (var shopItem in groupItems)
                    yield return shopItem;
            }

            yield break;
        }

        foreach (var id in _shopItemIds)
        {
            var shopItem = shopItemData.GetShopItem(id) ?? designDataHolder.IapShopItemData.GetShopItem(id);
            if (shopItem != null)
                yield return shopItem;
            else
                Debug.LogError($"Shop item with id {id} not found");
        }
    }

    private void AddShopItem(ShopItem shopItem, string placement)
    {
        var isBundle = shopItem.ItemsReward.Count > 1;
        var prefab = isBundle ? _bundleShopItemPrefab : _shopItemPrefab;
        var shopItemInstance = Instantiate(prefab, null);
        _ = shopItemInstance.SetData(shopItem, placement);
        _customGridLayout.Add(shopItemInstance.transform, isBundle);

        _shopItemsById[shopItem.Id] = shopItemInstance;
    }

    public Dictionary<string, int> GetNotifyBadgeData()
    {
        var data = new Dictionary<string, int>();
        foreach (var shopItem in _shopItemsById.Values)
        {
            if (shopItem is IHasNotifyBadgeData hasNotifyBadgeData)
            {
                var key = hasNotifyBadgeData.GetNotifyBadgeKey();
                data.TryAdd(key, 0);

                if (hasNotifyBadgeData.ShouldShowBadge())
                    data[key]++;
            }
        }

        foreach (var ui in _extraUIElements)
        {
            if (ui is IHasNotifyBadgeData hasNotifyBadgeData)
            {
                var key = hasNotifyBadgeData.GetNotifyBadgeKey();
                data.TryAdd(key, 0);

                if (hasNotifyBadgeData.ShouldShowBadge())
                    data[key]++;
            }
        }

        return data;
    }

    public void DismissBadge(string key)
    {
        foreach (var shopItem in _shopItemsById.Values)
        {
            if (shopItem is IHasNotifyBadgeData hasNotifyBadgeData)
            {
                if (hasNotifyBadgeData.GetNotifyBadgeKey() == key)
                    hasNotifyBadgeData.DismissBadge();
            }
        }

        foreach (var ui in _extraUIElements)
        {
            if (ui is IHasNotifyBadgeData hasNotifyBadgeData)
            {
                if (hasNotifyBadgeData.GetNotifyBadgeKey() == key)
                    hasNotifyBadgeData.DismissBadge();
            }
        }
    }

#if UNITY_EDITOR
    private string[] GetAllShopItemIds()
    {
        return ShopItemId.All;
    }
#endif
}
