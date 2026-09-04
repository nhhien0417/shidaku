using System;
using System.Threading.Tasks;
using AssetsHolder;
using Design.Ids;
using Design.Structures;
using TMPro;
using Titipi.MocaLib.Runtime.Services;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class UINormalPriceTexts : UIPriceTexts
{
    [SerializeField] private Image _imgPriceIcon;
    [SerializeField] private TextMeshProUGUI _txtPriceValue;

    public async Task SetPrice(Price price)
    {
        _txtPriceValue.text = price.GetPriceAsString();

        var priceImage = await ResourcesHolder.Instance.CurrencyIcons.GetSpriteAsync(price.Id);
        _imgPriceIcon.sprite = priceImage;
        _imgPriceIcon.gameObject.SetActive(priceImage != null);
    }

    public override void SetPrice(Price price, params object[] args)
    {
        if (price.Id == PriceId.IAP && args.Length > 0 && args[0] is string productId)
        {
            _txtPriceValue.text = MocaLib.Instance.IAPManager.GetProductPriceString(productId);
            _imgPriceIcon.gameObject.SetActive(false);
            return;
        }

        _ = SetPrice(price);
    }
}
