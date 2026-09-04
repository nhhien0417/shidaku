using System.Collections.Generic;
using System.Threading.Tasks;
using AssetsHolder;
using Design.Structures;
using UnityEngine;

public class UIShopItemBundle : UIShopItem
{
    [SerializeField] private UIRewardSlot _rewardSlotTemplate;

    private readonly List<UIRewardSlot> _rewardSlots = new();

    public override async Task SetData(ShopItem shopItem, string placement = "")
    {
        ClearRewards();
        _rewardSlotTemplate.gameObject.SetActive(false);
        await base.SetData(shopItem, placement);

        var container = _rewardSlotTemplate.transform.parent;
        foreach (var reward in shopItem.ItemsReward)
        {
            var slot = Instantiate(_rewardSlotTemplate, container);
            slot.gameObject.SetActive(true);
            slot.Icon.sprite = await ResourcesHolder.Instance.GetItemSpriteAsync(reward);
            slot.Amount.text = GetItemAmountText(reward);
            _rewardSlots.Add(slot);
        }
    }

    private void ClearRewards()
    {
        foreach (var slot in _rewardSlots)
            Destroy(slot.gameObject);

        _rewardSlots.Clear();
    }
}
