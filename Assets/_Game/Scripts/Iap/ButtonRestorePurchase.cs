using System.Collections.Generic;
using MEC;
using Titipi.MocaLib.Runtime.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Iap
{
    [RequireComponent(typeof(Button))]
    public class ButtonRestorePurchase : MonoBehaviour
    {
        [SerializeField] private Button _btnRestorePurchase;
        [InspectorReadOnly, SerializeField] private bool _isRestorePurchaseAvailable = false;

        public void Show()
        {
            if (!_isRestorePurchaseAvailable)
                return;

#if UNITY_IOS
            gameObject.SetActive(true);
#endif
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnRestorePurchase()
        {
            AudioManager.Instance.PlaySFXOneShot("button_click");
            GameVibration.Instance.Haptic(HapticFeedback.FeedbackType.Selection);

#if UNITY_IOS
            if (MocaLib.Instance.IAPManager.OnRestorePurchases != null)
            {
                UIManager.Instance.ShowLoading(new UILoading.Data()
                {
                    TimeOut = 15f
                });
                MocaLib.Instance.IAPManager.RestorePurchases();
                Timing.RunCoroutine(WaitForPurchaseRestore(10f));
            }
#endif
        }

        private IEnumerator<float> WaitForPurchaseRestore(float timeout)
        {
            var iapManager = MocaLib.Instance.IAPManager;
            if (iapManager == null) yield break;

            while (iapManager.OnRestorePurchases != null && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return Timing.WaitForOneFrame;
            }

            UIManager.Instance.HideLoading();

            if (timeout > 0f)
            {
                UIManager.Instance.ShowUIGroupOverlay<UINotify>(new UINotify.Data()
                {
                    Text = "Purchase restored!",
                });
            }
        }

        private void Awake()
        {
            _btnRestorePurchase.onClick.AddListener(OnRestorePurchase);
            _isRestorePurchaseAvailable = MocaLib.Instance?.IAPManager?.OnRestorePurchases != null;
            gameObject.SetActive(_isRestorePurchaseAvailable);
        }
    }
}
