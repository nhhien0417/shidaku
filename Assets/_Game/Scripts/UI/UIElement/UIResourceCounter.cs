using System;
using System.Globalization;
using System.Threading.Tasks;
using AssetsHolder;
using Design;
using Design.Ids;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UserDataPack;

public class UIResourceCounter : MonoBehaviour
{
    [SerializeField, ValueDropdown("GetAllResourceId")] protected string _resourceId;
    [SerializeField] protected Button _btn;
    [SerializeField] protected TextMeshProUGUI _txtValue;
    [SerializeField] protected Image _imgResourceIcon;
    [SerializeField] protected IgnoreResourceChange _ignoreResourceChange = IgnoreResourceChange.None;

    public Transform CurrencyIcon => _imgResourceIcon.transform;
    public string ResourceId => _resourceId;

    public void SetTextValue(int amount, float duration = 0f)
    {
        _txtValue.DOKill();
        if (duration <= 0f)
        {
            _txtValue.text = amount.ToResourceValueString();
        }
        else
        {
            if (_txtValue.text.TryParseResourceValueInt(out var currentAmount))
            {
                DOTween.To(() => currentAmount, x => currentAmount = x, amount, duration)
                    .SetEase(Ease.Linear)
                    .OnUpdate(() =>
                    {
                        _txtValue.text = currentAmount.ToResourceValueString();
                    });
            }
            else
            {
                _txtValue.DOText(amount.ToResourceValueString(), duration, true, ScrambleMode.Numerals).SetEase(Ease.Linear);
            }
        }
    }

    public void IncreaseValueBy(int amount, float duration = 0f)
    {
        if (_txtValue.text.TryParseResourceValueInt(out var currentAmount))
        {
            SetTextValue(currentAmount + amount, duration);
        }
        else
        {
            SetTextValue(amount, duration);
        }
    }

    public void RefreshValue()
    {
        SetTextValue(UserData.Instance.GetResourceItemAmount(_resourceId));
    }

    protected virtual void OnResourceItemChanged(string resourceId, int amount)
    {
        if (resourceId == _resourceId)
        {
            if (_ignoreResourceChange != IgnoreResourceChange.None)
            {
                _txtValue.text.TryParseResourceValueInt(out var displayedAmount);
                if ((_ignoreResourceChange.HasFlag(IgnoreResourceChange.Increase) && amount > displayedAmount) ||
                    (_ignoreResourceChange.HasFlag(IgnoreResourceChange.Decrease) && amount < displayedAmount))
                {
                    return;
                }
            }

            SetTextValue(amount);
        }
    }

    private void OnClick()
    {
        AudioManager.Instance.PlaySFXOneShot("button_click");
        GameVibration.Instance.Haptic(HapticFeedback.FeedbackType.Selection);
        // UIManager.Instance?.ShowUIGroupOverlay<UIShop>(new UIShop.Data()
        // {
        //     FocusResourceId = _resourceId
        // });
    }

    protected virtual async void Awake()
    {
        _btn.onClick.AddListener(OnClick);
        SetTextValue(UserData.Instance.GetResourceItemAmount(_resourceId));

        var timeout = 5f;
        while (ResourcesHolder.Instance == null && timeout > 0f)
        {
            await Task.Yield();
            timeout -= Time.deltaTime;
        }
        if (ResourcesHolder.Instance == null)
        {
            Debug.LogError("ResourcesHolder instance not found. Cannot load resource icon.");
            return;
        }
        _imgResourceIcon.sprite = await ResourcesHolder.Instance.GetItemSpriteAsync(_resourceId);
    }

    protected virtual void OnEnable()
    {
        SetTextValue(UserData.Instance.GetResourceItemAmount(_resourceId));
        UserData.Instance.OnResourceItemChanged += OnResourceItemChanged;
    }

    protected virtual void OnDisable()
    {
        UserData.Instance.OnResourceItemChanged -= OnResourceItemChanged;
    }

#if UNITY_EDITOR
    private string[] GetAllResourceId()
    {
        return ItemId.All;
    }
#endif

    [Flags]
    protected enum IgnoreResourceChange
    {
        None = 0,
        Increase = 1 << 0,
        Decrease = 1 << 1
    }
}
