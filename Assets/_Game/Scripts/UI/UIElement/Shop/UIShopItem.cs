using System;
using System.Threading.Tasks;
using _Game.UI.NotifyBadge;
using Analytics;
using AssetsHolder;
using Design.Ids;
using Design.Structures;
using Iap;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UserDataPack;

public class UIShopItem : MonoBehaviour, IHasNotifyBadgeData
{
    [SerializeField] private TextMeshProUGUI _txtTitle;
    [SerializeField] private TextMeshProUGUI _txtValue;
    [SerializeField] private UIPriceTexts _txtPrices;
    [SerializeField] private Image _imgIcon;
    [SerializeField] private Button _btnBuy;
    [SerializeField] private GameObject _pnlAvailable;
    [SerializeField] private GameObject _pnlSoldOut;
    [SerializeField] private GameObject _pnlNotifyBadge;

    [SerializeField, ReadOnly] private string _placement;

    private ShopItem _shopItem;

    public virtual async Task SetData(ShopItem shopItem, string placement = "")
    {
        _shopItem = shopItem;
        _placement = placement;
        _txtTitle.text = shopItem.Name;

        var value = 0;
        foreach (var item in shopItem.ItemsReward)
        {
            value += item.Amount;
        }
        _txtValue.text = shopItem.ItemsReward.Count == 1 ? GetItemAmountText(shopItem.ItemsReward[0]) : value.ToResourceValueString();

        if (shopItem is IapShopItem iapShopItem)
        {
            _txtPrices.SetPrice(iapShopItem.Price, iapShopItem.GetProductId());
        }
        else
        {
            _txtPrices.SetPrice(shopItem.Price);
        }

        _imgIcon.sprite = await ResourcesHolder.Instance.GetShopItemSpriteAsync(_shopItem.DisplayIconId);
    }

    protected static string GetItemAmountText(Item item)
    {
        switch (item.Id)
        {
            case ItemId.AutoXLimitedTime:
            case ItemId.NoAdsLimitedTime:
            {
                var t = TimeSpan.FromSeconds(item.Amount);
                if (t.TotalDays >= 1) return $"{(int)t.TotalDays}d";
                if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h";
                if (t.TotalMinutes >= 1) return $"{(int)t.TotalMinutes}m";
                return $"{item.Amount}s";
            }

            case ItemId.NoAds_24h:
                return $"{item.Amount * 24}h";

            case ItemId.NoAds_7Days:
                return $"{item.Amount * 7}d";

            case ItemId.Avatar:
            case ItemId.AvatarFrame:
            case ItemId.ProfileBanner:
            case ItemId.CustomizeQueen:
            case ItemId.CustomizeX:
                return "";

            default:
                return item.Amount.ToResourceValueString();
        }
    }

    public void UpdateUI()
    {
        var isAvailable = IsAvailable();
        if (isAvailable)
        {
            _pnlAvailable.SetActive(true);
            _pnlSoldOut.SetActive(false);
            _pnlNotifyBadge?.SetActive(CanActiveNotifyBadge());
            gameObject.SetActive(true);
        }
        else
        {
            _pnlAvailable.SetActive(false);
            _pnlSoldOut.SetActive(true);
            _pnlNotifyBadge?.SetActive(false);
            gameObject.SetActive(_shopItem != null && _shopItem.IsUnlocked());
        }

        if (gameObject.activeSelf && _shopItem != null)
        {
            ResourcesHolder.Instance.GetShopItemSprite(_shopItem.DisplayIconId, sprite =>
            {
                if (sprite != null && _imgIcon != null)
                {
                    _imgIcon.sprite = sprite;
                }
            });
        }
    }

    public bool IsAvailable()
    {
        if (_shopItem == null || !_shopItem.IsUnlocked())
            return false;

        var userData = UserData.Instance;
        if (_shopItem.ItemsReward is { Count: 1 } && ItemId.IsNoAdsItem(_shopItem.ItemsReward[0].Id) && userData.NoAdsActivated())
            return false;

        if (_shopItem.HasPurchaseLimit)
        {
            var purchasedCount = userData.TrackingData.GetShopItemBoughtTimes(_shopItem);
            if (purchasedCount >= _shopItem.PurchaseLimit)
                return false;
        }

        if (_shopItem.HasDailyPurchaseLimit)
        {
            var purchasedCount = userData.TrackingData.GetDailyBoughtTimes(_shopItem.Id);
            if (purchasedCount >= _shopItem.DailyPurchaseLimit)
                return false;
        }

        return true;
    }

    public bool IsUnlocked()
    {
        return _shopItem == null || _shopItem.IsUnlocked();
    }

    private bool CanActiveNotifyBadge()
    {
        return _shopItem is { HasDailyPurchaseLimit: true } && !UserData.Instance.TrackingData.IsDailyBadgeDismissedToday(_shopItem.Id);
    }

    private void OnBtnBuyClick()
    {
        if (_shopItem == null)
        {
            Debug.LogError("Shop item is null");
            return;
        }

        AudioManager.Instance.PlaySFXOneShot("button_click");
        GameVibration.Instance.Haptic(HapticFeedback.FeedbackType.Selection);

        PurchaseHandler.PurchaseItem(_shopItem, _placement, _btnBuy?.name ?? "", null, success =>
        {
            if (success)
            {
                UIManager.Instance.ShowUIGroupOverlay<UIRewards>(new UIRewards.Data()
                {
                    Rewards = _shopItem.ItemsReward
                });
                Debug.Log("Purchase successful!");
                UpdateUI();
            }
        });
    }

    private void Awake()
    {
        _btnBuy.onClick.AddListener(OnBtnBuyClick);
    }

    #region IHasNotifyBadgeData

    public string GetNotifyBadgeKey()
    {
        return BadgeNotificationType.Shop_NewItems;
    }

    public bool ShouldShowBadge()
    {
        return CanActiveNotifyBadge() && IsAvailable();
    }

    public void DismissBadge()
    {
        if (_shopItem == null)
            return;

        if (CanActiveNotifyBadge() && IsUnlocked())
        {
            var userData = UserData.Instance;
            userData.TrackingData.DismissDailyBadge(_shopItem.Id);
            userData.Save();
        }
    }

    #endregion
}
