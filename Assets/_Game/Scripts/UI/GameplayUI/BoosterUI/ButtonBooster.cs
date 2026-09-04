using System.Threading.Tasks;
using Analytics;
using AssetsHolder;
using Boosters;
using Design;
using Design.DataHolder;
using Design.Ids;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UserDataPack;

namespace UI.UIElements
{
    public class ButtonBooster : MonoBehaviour
    {
        [SerializeField, ValueDropdown("GetAllBoosterIds")] private string _boosterId;
        [SerializeField] private TextMeshProUGUI _plus;
        [SerializeField] private TextMeshProUGUI _txtAmount;
        [SerializeField] private TextMeshProUGUI _txtUnlockLevel;
        [SerializeField] private Image _imgBoosterIcon;
        [SerializeField] private Image _imgBackground;
        [SerializeField] private GameObject _lockGroup;
        [SerializeField] private GameObject _amountHolder;
        [SerializeField] private Button _btnBooster;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private UIResourceEffectTarget _target;

        private int _virtualSubtractAmount;

        private BoosterManager _boosterManager;
        private BoosterUnlockedData _boosterUnlockedData => DesignDataHolder.Instance.BoosterUnlockedData;

        public System.Action OnClickedForTutorial;

        private bool _isLocked;
        public bool IsLocked => _isLocked;

        private bool _isFreeUse;
        public bool IsFreeUse
        {
            get => _isFreeUse;
            set
            {
                _isFreeUse = value;
                _amountHolder.SetActive(!value && !_isLocked);
            }
        }

        public void SetBoosterManager(BoosterManager boosterManager)
        {
            _boosterManager = boosterManager;
        }

        public Tween PlayHighlight()
        {
            if (_isLocked) return null;

            StopHighlight();

            if (_btnBooster != null)
            {
                var seq = DOTween.Sequence();
                seq.SetLink(gameObject);
                seq.Append(_btnBooster.transform.DOScale(1.15f, 0.15f).SetEase(Ease.OutQuad));
                seq.Append(_btnBooster.transform.DOScale(0.95f, 0.15f).SetEase(Ease.InOutQuad));
                seq.Append(_btnBooster.transform.DOScale(1.05f, 0.1f).SetEase(Ease.InOutQuad));
                seq.Append(_btnBooster.transform.DOScale(1f, 0.1f).SetEase(Ease.InQuad));
                return seq;
            }

            return null;
        }

        public void StopHighlight()
        {
            if (_btnBooster != null)
            {
                _btnBooster.transform.DOKill();
                _btnBooster.transform.localScale = Vector3.one;
            }
        }

        private async void Awake()
        {
            if (_btnBooster != null)
            {
                _btnBooster.onClick.AddListener(OnBoosterClicked);
            }

            await LoadBoosterIcon();
            RefreshState();
        }

        private void OnEnable()
        {
            RefreshState();
            UserData.Instance.OnResourceItemChanged += OnResourceItemChanged;

            if (_boosterManager != null && _boosterManager.BoardManager != null)
            {
                _boosterManager.BoardManager.OnBoardChanged += RefreshInteractable;
            }
        }

        private void OnDisable()
        {
            if (UserData.Instance != null)
            {
                UserData.Instance.OnResourceItemChanged -= OnResourceItemChanged;
            }

            if (_boosterManager != null && _boosterManager.BoardManager != null)
            {
                _boosterManager.BoardManager.OnBoardChanged -= RefreshInteractable;
            }
        }

        private void OnDestroy()
        {
            if (_btnBooster != null)
            {
                _btnBooster.onClick.RemoveListener(OnBoosterClicked);
            }
        }

        private async Task LoadBoosterIcon()
        {
            if (string.IsNullOrEmpty(_boosterId))
            {
                Debug.LogWarning("Booster ID is not set!");
                return;
            }

            if (ResourcesHolder.Instance == null)
            {
                Debug.LogError("ResourcesHolder instance not found!");
                return;
            }

            var sprite = await ResourcesHolder.Instance.GetItemSpriteAsync(_boosterId);
            if (_imgBoosterIcon != null)
            {
                _imgBoosterIcon.sprite = sprite;
            }
        }

        public void UpdateAmount()
        {
            if (string.IsNullOrEmpty(_boosterId)) return;

            var amount = UserData.Instance.GetResourceItemAmount(_boosterId) - _virtualSubtractAmount;
            if (amount > 0)
            {
                _plus.gameObject.SetActive(false);
                _txtAmount.gameObject.SetActive(true);
                _txtAmount.text = amount.ToString();
            }
            else
            {
                _plus.gameObject.SetActive(true);
                _txtAmount.gameObject.SetActive(false);
            }
        }

        public void RefreshInteractable()
        {
            if (_isLocked || _boosterManager == null) return;

            _canvasGroup.alpha = _boosterManager.CanActivateBooster(_boosterId) ? 1f : 0.75f;
            _canvasGroup.blocksRaycasts = _boosterManager.CanActivateBooster(_boosterId);
        }

        public void SubtractAmount(int amount)
        {
            _virtualSubtractAmount += amount;
            UpdateAmount();
        }

        public void ClearVirtualSubtract()
        {
            _virtualSubtractAmount = 0;
            UpdateAmount();
        }

        private void OnResourceItemChanged(string resourceId, int amount)
        {
            if (resourceId == _boosterId)
            {
                UpdateAmount();
            }
        }

        public void RefreshState()
        {
            if (string.IsNullOrEmpty(_boosterId) || _boosterUnlockedData == null)
            {
                SetUnlocked();
                return;
            }

            var currentLevel = UserData.Instance.GameplayData.CurrentGameplayLevel;
            if (_boosterUnlockedData.BoosterUnlockedAtLevel(_boosterId, currentLevel, out int levelUnlocked))
            {
                SetUnlocked();
            }
            else
            {
                SetLocked(levelUnlocked);
            }
        }

        public void AnimateUnlock(System.Action onMidpoint, System.Action onComplete)
        {
            var seq = DOTween.Sequence();
            seq.SetLink(gameObject);
            seq.Append(transform.DOScale(0f, 0.5f).SetEase(Ease.InBack));
            seq.AppendCallback(() =>
            {
                SetUnlocked();
                onMidpoint?.Invoke();
            });
            seq.Append(transform.DOScale(1.25f, 0.5f).SetEase(Ease.OutBack));
            seq.AppendInterval(1f);
            seq.Append(transform.DOScale(1f, 0.5f).SetEase(Ease.InOutSine));
            seq.OnComplete(() => onComplete?.Invoke());
        }

        private void SetLocked(int unlockLevel)
        {
            _isLocked = true;
            _txtUnlockLevel.text = $"Level {unlockLevel}";
            _imgBackground.color = new Color(1f, 1f, 1f, 0f);

            _lockGroup.SetActive(true);
            _amountHolder.SetActive(false);
            _imgBoosterIcon.gameObject.SetActive(false);
        }

        private void SetUnlocked()
        {
            _isLocked = false;
            _imgBackground.color = Color.white;

            _lockGroup.SetActive(false);
            _amountHolder.SetActive(true);
            _imgBoosterIcon.gameObject.SetActive(true);

            UpdateAmount();
        }

        public void ForceLocked(int level)
        {
            _boosterUnlockedData.BoosterUnlockedAtLevel(_boosterId, level, out int levelUnlocked);

            var firstTimeData = UserData.Instance.FirstTimeData;
            var migrateData = UserData.Instance.NewVersionMigrateData;
            if (level == levelUnlocked &&
                !firstTimeData.HasShownBoosterTutorial(_boosterId) &&
                !migrateData.MissedBoosterIds.Contains(_boosterId))
            {
                SetLocked(levelUnlocked);
            }
        }

        public UIResourceEffectTarget GetEffectTarget()
        {
            return _target;
        }

        private void OnBoosterClicked()
        {
            if (_isLocked) return;
            StopHighlight();

            if (string.IsNullOrEmpty(_boosterId))
                return;

            GameVibration.Instance.Haptic(HapticFeedback.FeedbackType.Selection);

            var isFreeUse = IsFreeUse;
            OnClickedForTutorial?.Invoke();

            var userData = UserData.Instance;
            var currentAmount = userData.GetResourceItemAmount(_boosterId);

            if (!isFreeUse && currentAmount <= 0)
            {
                AudioManager.Instance.PlaySFXOneShot("button_click");

                Track.Screen.Open(Placement.UIGetMoreBooster);
                UIManager.Instance.ShowUIGroupOverlay<UIBuyBooster>(new UIBuyBooster.Data
                {
                    BoosterId = _boosterId,
                });

                return;
            }

            if (isFreeUse || PurchaseHandler.SpendResource(_boosterId, 1, Placement.MainGameplay, "use_booster"))
            {
                Debug.Log($"Used {_boosterId}! Remaining: {userData.GetResourceItemAmount(_boosterId)}");

                // Activate booster based on type
                if (_boosterManager != null)
                {
                    if (_boosterId == ItemId.Booster_1)
                    {
                        _boosterManager.ActivateQueen();
                    }
                    else if (_boosterId == ItemId.Booster_2)
                    {
                        _boosterManager.ActivateHint();
                    }
                    else if (_boosterId == ItemId.Booster_3)
                    {
                        _boosterManager.ActivateRandomMark();
                    }

                    if (userData.GameplayData.TutorialCompleted)
                    {
                        userData.Save();
                    }
                }
                else
                {
                    Debug.LogError("BoosterManager reference is missing! Call SetBoosterManager() first.");
                }
            }
        }

#if UNITY_EDITOR
        private string[] GetAllBoosterIds()
        {
            return ItemId.All;
        }
#endif
    }
}
