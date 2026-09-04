using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

using Titipi.MocaLib.Runtime.Services.Internal;
using Titipi.MocaLib.Runtime.Common;
using TMPro;

namespace Titipi.MocaLib.Runtime.Services
{
    public class FIAMPopupUI : MonoBehaviour
    {
        private const string TAG = "FIAMPopupUI";

        [Header("UI References :")]
        [SerializeField] private GraphicRaycaster _modalPopupCanvasGraphicRaycaster;

        [SerializeField] private CanvasGroup _modalPopupCanvasGroup;

        [SerializeField] private GameObject _modalPopup;

        [SerializeField] private GameObject _modalPopupHeader;
        [SerializeField] private Text _modalPopupTitle;
        [SerializeField] private Image _modalPopupCloseButtonImage;

        [SerializeField] private Text _modalPopupBodyText;

        [SerializeField] private Image _modalPopupOkButtonImage;
        [SerializeField] private Text _modalPopupOkButtonText;

        private Button _modalPopupButtonOk;
        private Button _modalPopupButtonClose;

        [SerializeField] private GameObject _imagePopup;
        [SerializeField] private RawImage _imagePopupRawImage;
        [SerializeField] private Button _imagePopupOkButton;
        [SerializeField] private Button _imagePopupCloseButton;
        [SerializeField] private TMP_Text _imagePopupPriceTag;

        private IFIAMPopupHandler _imagePopupHandler;

        [Header("Popup Colors :")]
        [SerializeField] private Color[] _colors;

        [Header("Popup Fade Duration :")] [Range(.1f, .8f)]
        [SerializeField] private float _fadeInDuration = .3f;

        [Range(.1f, .8f)] [SerializeField] private float _fadeOutDuration = .3f;

        [Space] public int MaxTextLength = 200;

        private Action<PopupButtonActionType> _onOkAction;
        private Action<PopupButtonActionType> _onCloseAction;


        private void Awake()
        {
            _modalPopupCanvasGroup.alpha = 0f;
            _modalPopupCanvasGroup.interactable = false;
            _modalPopupCanvasGraphicRaycaster.enabled = false;
        }

        public void Show(ModalMessageData data, PopupColor color, IFIAMPopupHandler handler)
        {
            _modalPopupTitle.text = data.Title;

            var text = data.Message;
            _modalPopupBodyText.text = (text.Length > MaxTextLength) ? text.Substring(0, MaxTextLength) + "..." : text;

            _modalPopupOkButtonText.text = data.OkButton?.Text;

            var c = _colors[(int)color];
            var ct = c;
            ct.a = .75f;
            _modalPopupTitle.color = ct;
            _modalPopupOkButtonImage.color = c;

            _modalPopupButtonOk = _modalPopupOkButtonImage.GetComponent<Button>();
            _modalPopupButtonOk.onClick.RemoveAllListeners();
            _modalPopupButtonOk.onClick.AddListener(() =>
            {
                FIAMManager.Instance.OnOkClick();
                handler?.Process(data.OkButton, null);

                StartCoroutine(FadeOut(_fadeOutDuration));
            });

            var hasCloseButton = data.CloseButton != null;
            _modalPopupCloseButtonImage.gameObject.SetActive(hasCloseButton);

            if (hasCloseButton)
            {
                _modalPopupButtonClose = _modalPopupCloseButtonImage.GetComponent<Button>();
                _modalPopupButtonClose.onClick.RemoveAllListeners();
                _modalPopupButtonClose.onClick.AddListener(() =>
                {
                    FIAMManager.Instance.OnCloseClick();
                    handler?.Process(data.CloseButton, null);

                    StartCoroutine(FadeOut(_fadeOutDuration));
                });
            }

            Dismiss();

            _modalPopup.SetActive(true);
            StartCoroutine(FadeIn(_fadeInDuration));
        }

        private IEnumerator FadeIn(float duration)
        {
            _modalPopupCanvasGraphicRaycaster.enabled = true;
            yield return Fade(_modalPopupCanvasGroup, 0f, 1f, duration);
            _modalPopupCanvasGroup.interactable = true;
        }

        private IEnumerator FadeOut(float duration)
        {
            yield return Fade(_modalPopupCanvasGroup, 1f, 0f, duration);
            _modalPopupCanvasGroup.interactable = false;
            _modalPopupCanvasGraphicRaycaster.enabled = false;
        }

        private IEnumerator Fade(CanvasGroup cGroup, float startAlpha, float endAlpha, float duration)
        {
            float startTime = Time.time;
            float alpha = startAlpha;

            if (duration > 0f)
            {
                // Anim start
                while (alpha < endAlpha)
                {
                    alpha = Mathf.Lerp(startAlpha, endAlpha, (Time.time - startTime) / duration);
                    cGroup.alpha = alpha;

                    yield return null;
                }
            }

            cGroup.alpha = endAlpha;
        }

        public void Dismiss()
        {
            StopAllCoroutines();

            _modalPopupCanvasGroup.alpha = 0f;
            _modalPopupCanvasGroup.interactable = false;
            _modalPopupCanvasGraphicRaycaster.enabled = false;

            _modalPopup?.SetActive(false);
            _imagePopup?.SetActive(false);
        }

        private void OnDestroy()
        {
            FIAMPopup.IsLoaded = false;
        }

        public void ShowImagePopup(ImageMessageData data, IFIAMPopupHandler handler)
        {
            Dismiss(); // hide other UI

            _imagePopupPriceTag.text = "";
            _imagePopupPriceTag.gameObject.SetActive(false);

            _imagePopup.SetActive(true);
            _imagePopupHandler = handler;

            // Setup tap-to-confirm
            _imagePopupOkButton.onClick.RemoveAllListeners();
            _imagePopupOkButton.onClick.AddListener(() =>
            {
                FIAMManager.Instance.OnOkClick();
                _imagePopupHandler?.Process(data.OkButton, data.Metadata);

                StartCoroutine(FadeOutImagePopup(_fadeOutDuration));
            });

            // Setup close (X) button
            _imagePopupCloseButton.gameObject.SetActive(true);
            _imagePopupCloseButton.onClick.RemoveAllListeners();
            _imagePopupCloseButton.onClick.AddListener(() =>
            {
                FIAMManager.Instance.OnCloseClick();
                _imagePopupHandler?.Process(data.CloseButton, data.Metadata);

                StartCoroutine(FadeOutImagePopup(_fadeOutDuration));
            });

            // Set up Price Tag if applicable
            if (data.OkButton?.ActionType == PopupButtonActionType.Purchase)
            {
                _imagePopupPriceTag.text = MocaLib.Instance.IAPManager.GetProductPriceString(data.OkButton.Value.ActionParam);
            }

            StartCoroutine(FetchAndShowImage(data.Metadata.ImageUrl));
            StartCoroutine(FadeIn(_fadeInDuration));
        }

        /// <summary>
        /// If pathOrUrl points to a local file, load it from disk.
        /// Otherwise use UnityWebRequest to download it.
        /// </summary>
        private IEnumerator FetchAndShowImage(string pathOrUrl)
        {
            if (string.IsNullOrEmpty(pathOrUrl))
            {
                Utils.MocaLibLogWarning(TAG, "Empty image path/url");
                yield break;
            }

            // If it's a local file path, load directly
            if (File.Exists(pathOrUrl))
            {
                byte[] bytes = null;
                try
                {
                    bytes = File.ReadAllBytes(pathOrUrl);
                }
                catch (Exception ex)
                {
                    Utils.MocaLibLogWarning(TAG, $"Failed to read local image {pathOrUrl}: {ex}");
                }

                if (bytes != null && bytes.Length > 0)
                {
                    var tex = new Texture2D(2, 2);
                    if (tex.LoadImage(bytes))
                    {
                        ApplyImage(tex);
                        yield break;
                    }
                }

                // fallthrough to attempt network load if local load failed
            }

            // Not a local file (or failed) -> treat as URL
            using var request = UnityWebRequestTexture.GetTexture(pathOrUrl);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Utils.MocaLibLogWarning(TAG, $"Failed to load image from {pathOrUrl}: {request.error}");
                yield break;
            }

            var downloaded = ((DownloadHandlerTexture)request.downloadHandler).texture;
            ApplyImage(downloaded);
        }

        private void ApplyImage(Texture2D texture)
        {
            _imagePopupRawImage.color = Color.white;
            _imagePopupRawImage.texture = texture;

            // If using AspectRatioFitter
            var fitter = _imagePopupRawImage.GetComponent<AspectRatioFitter>();
            if (fitter != null)
            {
                float aspect = (float)texture.width / texture.height;
                fitter.aspectRatio = aspect;
            }

            _imagePopupOkButton.transform.SetAsLastSibling();

            if (_imagePopupPriceTag.text.Length > 0)
            {
                _imagePopupPriceTag.gameObject.SetActive(true);
            }
        }

        private IEnumerator FadeOutImagePopup(float duration)
        {
            yield return Fade(_modalPopupCanvasGroup, 1f, 0f, duration);
            _modalPopupCanvasGroup.interactable = false;
            _modalPopupCanvasGraphicRaycaster.enabled = false;

            _imagePopupRawImage.color = Color.clear;
            _imagePopup.SetActive(false);
        }
    }
}
