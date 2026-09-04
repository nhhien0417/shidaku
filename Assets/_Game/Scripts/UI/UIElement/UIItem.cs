using AssetsHolder;
using Design.Ids;
using Design.Structures;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIItem : MonoBehaviour
{
    [SerializeField] private Image _imgIcon;
    [SerializeField] private TextMeshProUGUI _txtAmount;

    [SerializeField] private GameObject _bonusIndicator;

    public Item Item { get; private set; }

    public async void SetItem(Item item)
    {
        Item = item;

        if (_bonusIndicator != null)
            _bonusIndicator.SetActive(item is BonusItem);

        _imgIcon.sprite = await ResourcesHolder.Instance.GetItemSpriteAsync(item);

        switch (item.Id)
        {
            case ItemId.AutoXLimitedTime:
            case ItemId.NoAdsLimitedTime:
            {
                var t = System.TimeSpan.FromSeconds(item.Amount);
                if (t.TotalDays >= 1) _txtAmount.text = $"{(int)t.TotalDays}d";
                else if (t.TotalHours >= 1) _txtAmount.text = $"{(int)t.TotalHours}h";
                else if (t.TotalMinutes >= 1) _txtAmount.text = $"{(int)t.TotalMinutes}m";
                else _txtAmount.text = $"{item.Amount}s";
            }
                break;

            case ItemId.NoAds_24h:
                _txtAmount.text = $"{item.Amount*24}h";
                break;

            case ItemId.NoAds_7Days:
                _txtAmount.text = $"{item.Amount*7}d";
                break;

            case ItemId.Avatar:
            case ItemId.AvatarFrame:
            case ItemId.ProfileBanner:
            case ItemId.CustomizeQueen:
            case ItemId.CustomizeX:
                _txtAmount.text = "";
                break;

            default:
                _txtAmount.text = $"x{item.Amount.ToResourceValueString()}";
                break;
        }
    }
}
