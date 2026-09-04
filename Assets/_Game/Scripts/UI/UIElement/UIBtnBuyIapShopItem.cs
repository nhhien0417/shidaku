using System;
using Design;
using Design.Ids;
using Design.Structures;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class UIBtnBuyIapShopItem : MonoBehaviour
{
    [ValueDropdown("GetAllShopItemIds")]
    [SerializeField] private string _shopItemId;
    [SerializeField] private UIIapPriceTexts _priceTexts;
    [SerializeField] private Button _btnBuy;
    [SerializeField] private string _placement;

    public Action OnBuySuccess;
    public Action OnBuyFailed;

    private IapShopItem _shopItem;

    public void SetPlacement(string placement)
    {
        _placement = placement;
    }

    private void OnBuy()
    {
        if (_shopItem == null)
            return;

        PurchaseHandler.PurchaseItem(_shopItem, _placement, "", null, success =>
        {
            if (success)
            {
                UIManager.Instance.ShowUIGroupOverlay<UIRewards>(new UIRewards.Data()
                {
                    Rewards = _shopItem.ItemsReward
                });
                OnBuySuccess?.Invoke();
            }
            else
            {
                OnBuyFailed?.Invoke();
            }
        });
    }

    private void Awake()
    {
        _btnBuy.onClick.AddListener(OnBuy);

        _shopItem = DesignDataHolder.Instance?.IapShopItemData?.GetShopItem(_shopItemId);
        if (_shopItem != null)
        {
            _priceTexts.SetPrice(_shopItem.Price, _shopItem.GetProductId());
        }
    }

#if UNITY_EDITOR
    private string[] GetAllShopItemIds()
    {
        return ShopItemId.All;
    }
    #endif
}
