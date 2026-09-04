using System;
using Analytics;
using AssetsHolder;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UserDataPack;

public class UIPnlUserProfile : MonoBehaviour
{
    [SerializeField] private Image _imgAvatar;
    [SerializeField] private Image _imgAvatarFrame;
    [SerializeField] private Image _imgBanner;
    [SerializeField] private TextMeshProUGUI _txtName;
    
    [Header("-----Nullable-----")]
    [SerializeField] private Button _btnCustom;
    
    public void UpdateUI()
    {
        var userProfile = UserData.Instance.UserProfile;
        _txtName.text = userProfile.Username;

        var resourceHolder = ResourcesHolder.Instance;
        resourceHolder.AvatarIcons.GetSprite(userProfile.GetAvatarIndex(), sprite =>
        {
            if (sprite != null)
                _imgAvatar.sprite = sprite;
        });
        
        resourceHolder.AvatarFrames.GetSprite(userProfile.GetAvatarFrameIndex(), sprite =>
        {
            if (sprite != null)
                _imgAvatarFrame.sprite = sprite;
        });
        
        resourceHolder.ProfileBanners.GetSprite(userProfile.GetBannerIndex(), sprite =>
        {
            if (sprite != null)
                _imgBanner.sprite = sprite;
        });
    }

    private void ShowCustomUI()
    {
        Track.Screen.Open(Placement.UIUserProfile);
        UIManager.Instance.ShowUIGroupOverlay<UIUserProfileSettings>(new UIUserProfileSettings.Data()
        {
            OnProfileChanged = UpdateUI
        });
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    private void Awake()
    {
        if (_btnCustom != null)
            _btnCustom.onClick.AddListener(ShowCustomUI);
    }
}
