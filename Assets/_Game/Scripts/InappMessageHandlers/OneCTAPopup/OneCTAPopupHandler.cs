using Titipi.MocaLib.Runtime.Services;
using UserDataPack;

namespace Game.InappMessageHandlers
{
    public class OneCTAPopupHandler
    {
        public static void HandleReceivedBannerMessage(BannerMessageData messageData)
        {
            var data = messageData.CustomData;
            if (data == null)
                return;

            if (!data.TryGetValue(InAppMessageDataKey.Action, out var action))
                return;

            switch (action)
            {
                case MessageActionKey.ActiveOffer:
                    break;

                case MessageActionKey.FreeGift:
                {
                    var popupData = OnCTAPopupData.FreeGiftData.Parse(data);
                    if (popupData != null)
                    {
                        var items = popupData.Items;
                        if (items is { Count: > 0 })
                        {
                            var userData = UserData.Instance;
                            userData.AddItems(items, "free_gift_fiam");
                            userData.Save();
                            UIManager.Instance.ShowUIGroupOverlay<UIOneCTAPopup>(new UIOneCTAPopup.Data()
                            {
                                CTAButtonText = "Owesome!",
                                PopupImageUrl = popupData.PopupImageUrl,
                                ShowCloseButton = false,
                                OnCTAClicked = () =>
                                {
                                    var uiManager = UIManager.Instance;
                                    uiManager.HideUIGroup<UIOneCTAPopup>();
                                    uiManager.ShowUIGroupOverlay<UIRewards>(new UIRewards.Data()
                                    {
                                        Rewards = items
                                    });
                                },
                                OnCloseClicked = () =>
                                {
                                    UIManager.Instance.ShowUIGroupOverlay<UIRewards>(new UIRewards.Data()
                                    {
                                        Rewards = items
                                    });
                                }
                            });
                        }
                    }
                    else
                    {
                        CustomLogger.LogError("OneCTAPopupHandler", "Failed to parse FreeGift data.");
                    }
                }
                    break;

                default:
                    CustomLogger.LogError("OneCTAPopupHandler", $"Action {action} not supported.");
                    break;
            }
        }
    }
}