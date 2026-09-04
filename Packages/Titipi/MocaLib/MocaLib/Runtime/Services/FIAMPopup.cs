using UnityEngine;
using UnityEngine.EventSystems;

using Titipi.MocaLib.Runtime.Services.Internal;

namespace Titipi.MocaLib.Runtime.Services
{
    public enum PopupColor
    {
        Black,
        Red,
        Purple,
        Magenta,
        Blue,
        Green,
        Yellow,
        Orange
    }

    public static class FIAMPopup
    {
        public static bool IsLoaded;
        private static FIAMPopupUI _fiamPopupUI;

        private static void Prepare()
        {
            if (IsLoaded) return;

            var instance = Object.Instantiate(Resources.Load<GameObject>("FIAMPopupUI"));
            instance.name = "FIAMPopupUI";

            _fiamPopupUI = instance.GetComponent<FIAMPopupUI>();
            IsLoaded = true;

            CheckForEventSystem();
        }

        private static void CheckForEventSystem()
        {
            // Check if there is an EventSystem in the scene; if not, add one.
            var es = Object.FindFirstObjectByType<EventSystem>();
            if (ReferenceEquals(es, null))
            {
                var esGameObject = new GameObject("EventSystem");
                esGameObject.AddComponent<EventSystem>();
                esGameObject.AddComponent<StandaloneInputModule>();
            }
        }

        public static void ShowModalPopup(ModalMessageData? popupData, IFIAMPopupHandler handler)
        {
            if (popupData == null) return;

            Prepare();

            var data = (ModalMessageData) popupData;

            _fiamPopupUI.Show(data, PopupColor.Red, handler);;
        }

        public static void ShowImagePopup(ImageMessageData messageData, IFIAMPopupHandler handler)
        {
            Prepare();
            _fiamPopupUI.ShowImagePopup(messageData, handler);
        }

        private static void Dismiss()
        {
            if (IsLoaded)
                _fiamPopupUI.Dismiss();
        }
    }
}
