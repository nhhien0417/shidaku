using System;
using Design.Structures;
using Iap;
using Titipi.MocaLib.Runtime.Services;
using TMPro;
using UnityEngine;

[Serializable]
public class UIIapPriceTexts : UIPriceTexts
{
    [SerializeField] private TextMeshProUGUI _txtPrice;
    [SerializeField] private TextMeshProUGUI _txtOriginPrice;
    [SerializeField] private GameObject _pnlOriginPrice;

    public void SetProductId(string productId, string productIdBeforeDiscount = "")
    {
        if (string.IsNullOrEmpty(productId))
        {
            Debug.LogError("[UIIapPriceTexts] Product id is empty");
            return;
        }

        var iapManager = MocaLib.Instance.IAPManager;
        _txtPrice.text = iapManager.GetProductPriceString(productId);
        
        if (!string.IsNullOrEmpty(productIdBeforeDiscount))
        {
            _txtOriginPrice.text = iapManager.GetProductPriceString(productIdBeforeDiscount);
            _pnlOriginPrice.SetActive(true);
        }
        else
        {
            _pnlOriginPrice.SetActive(false);
        }
    }

    public override void SetPrice(Price price, params object[] args)
    {
        if (args.Length == 0 || args[0] is not string productId)
        {
            Debug.LogError("[UIIapPriceTexts] Product id is missing");
            return;
        }

        string productIdDiscount = null;
        if (args.Length > 1 && args[1] is string discountId)
        {
            productIdDiscount = discountId;
        }

        SetProductId(productId, productIdDiscount);
    }
}
