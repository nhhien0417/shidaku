using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class UINotify : UIGroup
{
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _contentText;
    [SerializeField] private Button _btnOK;

    private void Awake()
    {
        if (_btnOK != null)
        {
            _btnOK.onClick.AddListener(OnOKClicked);
        }
    }

    public override void Show(object data = null, Action onCompleted = null)
    {
        base.Show(data, onCompleted);

        if (data is Data notifyData)
        {
            UpdateUI(notifyData);
        }
    }

    public override void UpdateUI(object data)
    {
        if (data is Data notifyData)
        {
            if (_contentText != null)
            {
                _contentText.text = notifyData.Text;
            }

            if (_titleText != null)
            {
                _titleText.text = notifyData.Title ?? "Notification";
            }
        }
    }

    private void OnOKClicked()
    {
        AudioManager.Instance.PlaySFXOneShot("button_click");
        GameVibration.Instance.Haptic(HapticFeedback.FeedbackType.Selection);
        Hide();
    }

    [Serializable]
    public class Data
    {
        public string Text;
        public string Title = "Notification";
    }
}
