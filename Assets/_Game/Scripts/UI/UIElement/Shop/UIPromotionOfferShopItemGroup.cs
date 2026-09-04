using System.Collections.Generic;
using _Game.UI.NotifyBadge;
using Game.PromotionOffer;
using UnityEngine;
using UserDataPack;

public class UIPromotionOfferShopItemGroup : MonoBehaviour
{
    [SerializeField] private Transform _shopItemContainer;
    [SerializeField] private UIShopItemPromotionOffer _shopItemPrefab;
    
    private List<UIShopItemPromotionOffer> _uiShopItems = new ();
    private string _placement;

    public void UpdateOfferList()
    {
        var activeOffers = PromotionOfferManager.Instance.ActivatedOffers;
        for (var i = 0; i < activeOffers.Count; i++)
        {
            var offer = activeOffers[i];
            UIShopItemPromotionOffer ui;
            if (i < _uiShopItems.Count)
            {
                ui = _uiShopItems[i];
            }
            else
            {
                ui = Instantiate(_shopItemPrefab, _shopItemContainer);
                _uiShopItems.Add(ui);
            }
            ui.gameObject.SetActive(true);
            ui.SetData(offer, _placement, OnRequestRemoteOffer);
        }
        
        for (var i = activeOffers.Count; i < _uiShopItems.Count; i++)
        {
            _uiShopItems[i].gameObject.SetActive(false);
        }
        
        gameObject.SetActive(activeOffers.Count > 0);
    }

    public void Initialize(string placement)
    {
        _placement = placement;
        UpdateOfferList();
    }

    private void OnRequestRemoteOffer(UIShopItemPromotionOffer ui)
    {
        if (ui != null)
        {
            PromotionOfferManager.Instance.RemoveActiveOffer(ui.Offer, true);
            ui.gameObject.SetActive(false);
            gameObject.SetActive(PromotionOfferManager.Instance.ActivatedOffers.Count > 0);
        }
    }
    
    public Dictionary<string, int> GetNotifyBadgeData()
    {
        var data = new Dictionary<string, int>()
        {
            {BadgeNotificationType.Shop_NewItems, 0},
        };
        
        foreach (var ui in _uiShopItems)
        {
            if (ui.IsNewItem)
            {
                data[BadgeNotificationType.Shop_NewItems]++;
            }
        }

        return data;
    }
    
    public void DismissBadge(string key)
    {
        var userData = UserData.Instance;
        var needSave = false;
        switch (key)
        {
            case BadgeNotificationType.Shop_NewItems:
                foreach (var ui in _uiShopItems)
                {
                    var isNewItem = ui.IsNewItem;
                    needSave |= isNewItem;
                    if (isNewItem)
                    {
                        userData.TrackingData.RemoveNewPromotionOfferId(ui.Offer.OfferData.Id);
                        ui.IsNewItem = false;
                    }
                }
                break;
        }
        
        if (needSave)
            userData.Save();
    }
}
