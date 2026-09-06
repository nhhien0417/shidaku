using System;
using Design.Structures;
using Game.PromotionOffer;
using UnityEngine;
using UnityEngine.UI;

public class UIPromotionOfferPopup : UIGroup
{
    [SerializeField] private Image _imgPopup;
    [SerializeField] private UIIapPriceTexts _uiIapPriceTexts;
    [SerializeField] private UIPriceTexts _uiPriceTexts;
    [SerializeField] private Button _btnBuy;
    [SerializeField] private Button _btnClose;

    private Data _data;
    
    public override void Show(object data = null, Action onCompleted = null)
    {
        if (data is not Data d)
        {
            this.LogError($"Data is not valid.");
            return;
        }
        _data = d;

        _imgPopup.sprite = _data.Offer.OfferDefinition.PopupImage;

        if (_data.Offer.OfferDefinition.ShopItem is IapShopItem iapShopItem)
        {
            _uiIapPriceTexts.SetPrice(iapShopItem.Price, iapShopItem.GetProductId(), _data.Offer.OfferDefinition.IapProductIdBeforeDiscount.GetProductId());
            
            _uiIapPriceTexts.gameObject.SetActive(true);
            _uiPriceTexts.gameObject.SetActive(false);
        }
        else
        {
            _uiPriceTexts.SetPrice(_data.Offer.OfferDefinition.ShopItem.Price);
            
            _uiIapPriceTexts.gameObject.SetActive(false);
            _uiPriceTexts.gameObject.SetActive(true);
        }
        
        base.Show(data, onCompleted);
    }

    private void OnBuy()
    {
        var shopItem = _data.Offer.OfferDefinition.ShopItem;
        if (shopItem == null)
            return;
        
        PurchaseHandler.PurchaseItem(shopItem, "promotion_offer_popup", "buy_offer", null, success =>
        {
            if (success)
            {
                UIManager.Instance.ShowUIGroupOverlay<UIRewards>(new UIRewards.Data()
                {
                    Rewards = _data.Offer.OfferDefinition.ShopItem.ItemsReward
                });
                PromotionOfferManager.Instance.RemoveActiveOffer(_data.Offer, true);
                Hide();
            }
        });
    }

    private void Awake()
    {
        _btnBuy.onClick.AddListener(OnBuy);
        _btnClose.onClick.AddListener(Hide);
    }

    public class Data
    {
        public PromotionOfferManager.Offer Offer;
    }
}
