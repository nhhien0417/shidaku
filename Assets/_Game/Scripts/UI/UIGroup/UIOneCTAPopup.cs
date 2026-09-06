using System;
using System.Threading;
using System.Threading.Tasks;
using Extensions.Unity.ImageLoader;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIOneCTAPopup : UIGroup
{
    [SerializeField] private TMP_Text _txtCtaBtn;
    [SerializeField] private Image _imgPopup;
    [SerializeField] private GameObject _loadingIcon;
    [SerializeField] private Button _btnCTA;
    [SerializeField] private Button _btnClose;

    private Data _data;
    private CancellationTokenSource _loadCts;

    public override void Show(object data = null, Action onCompleted = null)
    {
        if (data is not Data d)
            return;
        _data = d;

        base.Show(data, onCompleted);

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();

        _txtCtaBtn.text = _data.CTAButtonText;
        _btnCTA.gameObject.SetActive(false);
        _btnClose.gameObject.SetActive(false);
        _loadingIcon.SetActive(true);
        _imgPopup.gameObject.SetActive(false);
        _imgPopup.sprite = null;

        ImageLoader.LoadSprite(_data.PopupImageUrl, cancellationToken: _loadCts.Token)
            .Consume(_imgPopup)
            .Timeout(TimeSpan.FromSeconds(5))
            .Completed(isLoaded =>
            {
                _loadingIcon.SetActive(!isLoaded);
                _imgPopup.gameObject.SetActive(isLoaded);
                _btnCTA.gameObject.SetActive(isLoaded);
                _btnClose.gameObject.SetActive((_data?.ShowCloseButton ?? true) || !isLoaded);
            })
            .Forget();
    }

    private void OnCTAClicked()
    {
        _data?.OnCTAClicked?.Invoke();
    }

    private void OnCloseClicked()
    {
        Hide();
        _data?.OnCloseClicked?.Invoke();
    }

    private void Awake()
    {
        _btnCTA.onClick.AddListener(OnCTAClicked);
        _btnClose.onClick.AddListener(OnCloseClicked);
    }

    private void OnDisable()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
    }

    public class Data
    {
        public string PopupImageUrl;
        public string CTAButtonText;
        public bool ShowCloseButton;
        public Action OnCTAClicked;
        public Action OnCloseClicked;
    }
}
