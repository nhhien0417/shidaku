using _Game.UI.NotifyBadge;
using Analytics;
using Design;
using Design.Ids;
using Design.Structures;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UserDataPack;

public class UIFreeCoin : UIElement, IHasNotifyBadgeData
{
    [SerializeField] private TextMeshProUGUI _txtValue;
    [SerializeField] private TextMeshProUGUI _txtNextClaimTime;
    [SerializeField] private UINormalPriceTexts _priceTexts;
    [SerializeField] private Button _btnGet;
    [SerializeField] private GameObject _pnlSoldOut;

    private ShopItem _currentShopItem;
    private bool IsCurrentRewardFree => _currentShopItem?.Price.Id == PriceId.Free;

    public override void UpdateUI()
    {
        _pnlSoldOut.SetActive(false);
        _btnGet.gameObject.SetActive(true);
        if (!DesignDataHolder.Instance.HasFreeCoinRewardInShop(out _currentShopItem) || _currentShopItem == null)
        {
            _pnlSoldOut.SetActive(true);
            _btnGet.gameObject.SetActive(false);
        }

        if (_currentShopItem != null)
        {
            var shopItem = _currentShopItem;

            if (shopItem.HasItem(ItemId.Coin, out var item))
            {
                _txtValue.text = $"{item.Amount.ToResourceValueString()}";
            }
            else
            {
                _txtValue.text = shopItem.Name;
                Debug.LogError($"ShopItem {shopItem.Id} does not have Coin item reward");
            }

            _ = _priceTexts.SetPrice(shopItem.Price);
        }

        gameObject.SetActive(true);
    }

    private void OnRequestGetReward()
    {
        if (_currentShopItem == null)
            return;

        AudioManager.Instance.PlaySFXOneShot("button_click");
        GameVibration.Instance.Haptic(HapticFeedback.FeedbackType.Selection);

        _currentShopItem.AdReason = AdReason.RvCoinsReward;
        PurchaseHandler.PurchaseItem(_currentShopItem, Placement, _btnGet?.name ?? "", null, success =>
        {
            if (success)
            {
                UIManager.Instance.ShowUIGroupOverlay<UIRewards>(new UIRewards.Data()
                {
                    Rewards = _currentShopItem.ItemsReward
                });

                UpdateUI();
                BadgeNotificationManager.Instance.RefreshShopBadges();
                Debug.Log($"Get free coin reward success");
            }
            else
            {
                Debug.LogError($"Get free coin reward failed");
            }
        });
    }

    private void Awake()
    {
        _btnGet.onClick.AddListener(OnRequestGetReward);
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    #region IHasNotifyBadgeData
    public string GetNotifyBadgeKey()
    {
        return BadgeNotificationType.Shop_FreeReward;
    }

    public bool ShouldShowBadge()
    {
        return gameObject.activeInHierarchy &&
               (IsCurrentRewardFree || !UserData.Instance.TrackingData.IsDailyBadgeDismissedToday(BadgeNotificationType.Shop_FreeReward));
    }

    public void DismissBadge()
    {
        if (_currentShopItem == null || IsCurrentRewardFree)
            return;

        var userData = UserData.Instance;
        if (!userData.TrackingData.IsDailyBadgeDismissedToday(BadgeNotificationType.Shop_FreeReward))
        {
            userData.TrackingData.DismissDailyBadge(BadgeNotificationType.Shop_FreeReward);
            userData.Save();
        }
    }
    #endregion
}
