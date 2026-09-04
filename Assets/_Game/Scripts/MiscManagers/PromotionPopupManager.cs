using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Design;

public class PromotionPopupManager : Singleton<PromotionPopupManager>
{
    private ShopPopupData[] shopPopupIds = new ShopPopupData[]
    {
        // new ShopPopupData()
        // {
        //     Id = "no_ads", 
        //     PopupType = typeof(UIPopupShopItem),
        //     Data = new UIPopupShopItem.Data() { ShopItemId = "no_ads" }
        // },
        // new ShopPopupData()
        // {
        //     Id = "rewards_master", 
        //     PopupType = typeof(UIPopupShopItem),
        //     Data = new UIPopupShopItem.Data() { ShopItemId = "" }
        // },
        // new ShopPopupData()
        // {
        //     Id = "score_madness", 
        //     PopupType = typeof(UIPopupShopItem),
        //     Data = new UIPopupShopItem.Data() { ShopItemId = "" }
        // },
        // new ShopPopupData()
        // {
        //     Id = "hammer", 
        //     PopupType = typeof(UIPopupTheHammer),
        //     Data = new UIPopupTheHammer.Data() { ShopItemId = "" }
        // },
        // new ShopPopupData()
        // {
        //     Id = "high_roller", 
        //     PopupType = typeof(UIPopupHighRoller),
        //     Data = new UIPopupHighRoller.Data() { ShopItemId = "" }
        // },
        // new ShopPopupData()
        // {
        //     Id = "FirstlySubscription", 
        //     PopupType = typeof(UISubscription),
        //     Data = new UISubscription.Data()
        //     {
        //         Place = "offer"
        //     }
        // },
    };
    
    private List<int> shopPopupIndexes = new List<int>();
    
    public void CheckAndShowPromotionPopup()
    {
        ShowRandomPromotionPopup();
    }

    protected override void Init()
    {
        base.Init();
    }
    
    private void ShowRandomPromotionPopup()
    {
        if (shopPopupIndexes.Count == 0)
        {
            for (int i = 0; i < shopPopupIds.Length; i++)
            {
                var popupData = shopPopupIds[i];
                shopPopupIndexes.Add(i);
            }
            shopPopupIndexes.Shuffle();
        }
        
        if (shopPopupIndexes.Count > 0)
        {
            var index = shopPopupIndexes[0];
            shopPopupIndexes.RemoveAt(0);
            var popupData = shopPopupIds[index];
            UIManager.Instance.ShowUIGroupOverlay(popupData.PopupType, popupData.Data);
        }
    }
    
    [Serializable]
    public class ShopPopupData
    {
        public string Id;
        public Type PopupType;
        public object Data;
    }
}
