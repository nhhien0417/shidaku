using System;
using System.Collections.Generic;
using Design.Structures;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Game.PromotionOffer;
using MEC;
using UserDataPack;

public class UIShopItemPromotionOffer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _txtTitle;
    [SerializeField] private TextMeshProUGUI _txtTime;
    [SerializeField] private Image _imgItems;
    [SerializeField] private UIPriceTexts _txtPrices;
    [SerializeField] private UIIapPriceTexts _txtIapPrices;
    [SerializeField] private Button _btnBuy;
    [SerializeField] private GameObject _pnlTime;
    [SerializeField] private GameObject _pnlAvailable;
    [SerializeField] private GameObject _pnlSoldOut;
    [SerializeField] private GameObject _pnlNotifyBadge;

    [SerializeField, ReadOnly] private string _placement;

    public PromotionOfferManager.Offer Offer { get; private set; }

    public bool IsNewItem
    {
        get => _pnlNotifyBadge.activeSelf;
        set => _pnlNotifyBadge.SetActive(value);
    }

    private Action<UIShopItemPromotionOffer> _onRequestRemoveOffer;
    private CoroutineHandle _countdownCoroutine;

    public virtual void SetData(PromotionOfferManager.Offer offer, string placement = "", Action<UIShopItemPromotionOffer> onRequestRemoveOffer = null)
    {
        _onRequestRemoveOffer = onRequestRemoveOffer;
        _placement = placement;

        if (offer == Offer)
            return;

        Offer = offer;
        _txtTitle.text = Offer.OfferDefinition.ShopItem.Name;

        if (Offer.OfferDefinition.ShopItem is IapShopItem iapShopItem)
        {
            _txtIapPrices.SetPrice(iapShopItem.Price, iapShopItem.GetProductId(), Offer.OfferDefinition.IapProductIdBeforeDiscount.GetProductId());

            _txtPrices.gameObject.SetActive(false);
            _txtIapPrices.gameObject.SetActive(true);
        }
        else
        {
            _txtPrices.SetPrice(Offer.OfferDefinition.ShopItem.Price);

            _txtPrices.gameObject.SetActive(false);
            _txtIapPrices.gameObject.SetActive(true);
        }

        _imgItems.sprite = Offer.OfferDefinition.ShopImage;

        IsNewItem = UserData.Instance.TrackingData.IsNewPromotionOfferId(offer.OfferData.Id);

        SetAvailable(true);

        Timing.KillCoroutines(_countdownCoroutine);
        _countdownCoroutine = Timing.RunCoroutine(CoolDown());
    }

    public void SetAvailable(bool isAvailable)
    {
        if (isAvailable)
        {
            _pnlAvailable.SetActive(true);
            _pnlSoldOut.SetActive(false);
        }
        else
        {
            _pnlAvailable.SetActive(false);
            _pnlSoldOut.SetActive(true);
        }
    }

    private IEnumerator<float> CoolDown()
    {
        var limitedTimeData = Offer?.OfferData?.LocalLimitedTimeData;
        if (limitedTimeData == null)
        {
            _pnlTime.SetActive(false);
            yield break;
        }
        _pnlTime.SetActive(true);

        var remainSeconds = limitedTimeData.GetRemainingSeconds();
        while (remainSeconds > 0)
        {
            var timeSpan = TimeSpan.FromSeconds(remainSeconds);
            _txtTime.text = timeSpan.TotalDays > 1 ? timeSpan.ToReadableString("[d] [hh]") : timeSpan.ToReadableString("[hh] [mm]");
            yield return Timing.WaitForSeconds(1f);
            remainSeconds = limitedTimeData.GetRemainingSeconds();
        }

        SetAvailable(false);

        _onRequestRemoveOffer?.Invoke(this);
    }

    private void OnBtnBuyClick()
    {
        if (Offer == null || Offer.OfferDefinition?.ShopItem == null)
        {
            this.LogError("Shop item is null");
            return;
        }

        AudioManager.Instance.PlaySFXOneShot("button_click");
        GameVibration.Instance.Haptic(HapticFeedback.FeedbackType.Selection);

        PurchaseHandler.PurchaseItem(Offer.OfferDefinition?.ShopItem, _placement, "buy_offer", null, success =>
        {
            if (success)
            {
                UIManager.Instance.ShowUIGroupOverlay<UIRewards>(new UIRewards.Data()
                {
                    Rewards = Offer.OfferDefinition?.ShopItem.ItemsReward
                });
                this.Log("Purchase successful!");

                SetAvailable(false);

                _onRequestRemoveOffer?.Invoke(this);
            }
        });
    }

    private void Awake()
    {
        _btnBuy.onClick.AddListener(OnBtnBuyClick);
    }

    private void OnEnable()
    {
        if (Offer != null)
        {
            Timing.KillCoroutines(_countdownCoroutine);
            _countdownCoroutine = Timing.RunCoroutine(CoolDown());
        }
    }

    private void OnDisable()
    {
        Timing.KillCoroutines(_countdownCoroutine);
    }
}
