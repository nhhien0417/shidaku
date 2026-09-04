using System;
using System.Collections.Generic;
using Game.PromotionOffer;
using MEC;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PnlPromotionOfferButtons : MonoBehaviour
{
    [SerializeField] private Button _btn;
    [SerializeField] private Image _imgIcon;
    [SerializeField] private TextMeshProUGUI _txtTime;
    [SerializeField] private GameObject _pnlTime;

    private PromotionOfferManager.Offer _currentOffer;
    private CoroutineHandle _coolDownCoroutine;

    public void UpdateOffers()
    {
        var offers = PromotionOfferManager.Instance.ActivatedOffers;
        if (offers.Count > 0)
        {
            _currentOffer = offers[0];
            _imgIcon.sprite = _currentOffer.OfferDefinition.IconImage;

            if (_currentOffer.OfferData.LocalLimitedTimeData != null)
            {
                _pnlTime.SetActive(true);
                Timing.KillCoroutines(_coolDownCoroutine);
                _coolDownCoroutine = Timing.RunCoroutine(CoolDown());
            }
            else
            {
                _pnlTime.SetActive(false);
            }

            _btn.gameObject.SetActive(true);
        }
        else
        {
            _btn.gameObject.SetActive(false);
        }
    }

    private IEnumerator<float> CoolDown()
    {
        var limitedTimeData = _currentOffer.OfferData.LocalLimitedTimeData;
        if (limitedTimeData == null)
            yield break;

        var remainSeconds = limitedTimeData.GetRemainingSeconds();
        while (remainSeconds > 0)
        {
            var timeSpan = TimeSpan.FromSeconds(remainSeconds);
            _txtTime.text = timeSpan.TotalDays > 1 ? timeSpan.ToReadableString("[d] [hh]") : timeSpan.ToReadableString("[hh] [mm]");
            yield return Timing.WaitForSeconds(1f);
            remainSeconds = limitedTimeData.GetRemainingSeconds();
        }

        if (_currentOffer != null)
        {
            PromotionOfferManager.Instance.RemoveActiveOffer(_currentOffer, true);
        }
    }

    private void OnButtonClick()
    {
        if (_currentOffer != null)
        {
            Track.CustomEvent("promotion_offer_button_tap", new Dictionary<string, object>()
            {
                { "OfferId", _currentOffer.OfferData.Id },
            });
            UIManager.Instance.ShowUIGroupOverlay<UIPromotionOfferPopup>(new UIPromotionOfferPopup.Data()
            {
                Offer = _currentOffer,
            });
        }
    }

    private void Awake()
    {
        _btn.onClick.AddListener(OnButtonClick);
    }

    private void Start()
    {
        UpdateOffers();
    }

    private void OnDisable()
    {
        Timing.KillCoroutines(_coolDownCoroutine);
    }
}
