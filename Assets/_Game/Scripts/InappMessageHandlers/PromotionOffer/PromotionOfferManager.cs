using System.Collections.Generic;
using _Game.UI.NotifyBadge;
using Design;
using Titipi.MocaLib.Runtime.Services;
using UnityEngine;
using UserDataPack;
using UserDataPack.Structures;
using Game.InappMessageHandlers;

namespace Game.PromotionOffer
{
    public class PromotionOfferManager : Singleton<PromotionOfferManager>
    {
        private List<Offer> _pendingOffers = new();
        public List<Offer> ActivatedOffers { get; private set; } = new();

        public bool CheckAndActivePromotionOffer()
        {
            if (_pendingOffers.Count <= 0)
                return false;

            var offer = _pendingOffers[0];
            _pendingOffers.RemoveAt(0);
            if (offer.OfferData.ActiveOffer())
            {
                ActivatedOffers.Add(offer);
                ActivatedOffers.Sort((x, y) => y.OfferData.Priority - x.OfferData.Priority);
                UIManager.Instance.ShowUIGroupOverlay<UIPromotionOfferPopup>(new UIPromotionOfferPopup.Data()
                {
                    Offer = offer,
                });

                Object.FindAnyObjectByType<PnlPromotionOfferButtons>()?.UpdateOffers();

                Track.CustomEvent("promotion_offer_activated", new Dictionary<string, object>()
                {
                    { "OfferId", offer.OfferData.Id },
                    { "DurationInSeconds", offer.OfferData.LimitedTimeInSeconds },
                });

                if (UserData.Instance.TrackingData.AddNewPromotionOfferId(offer.OfferData.Id))
                {
                    UserData.Instance.Save();
                    BadgeNotificationManager.Instance?.IncreaseNotification(BadgeNotificationType.Shop_NewItems);
                }

                return true;
            }
            else
            {
                // this.LogError($"Failed to active offer {offer.OfferData.Id}.");

                if (offer.OfferData.IsNeedToBeRemoved())
                {
                    var userData = UserData.Instance;
                    userData.PromotionOfferData.RemoveOffer(offer.OfferData);
                    userData.Save();
                }
                else
                {
                    _pendingOffers.Add(offer);
                }
                return false;
            }
        }

        public void ActiveAllOffersSilently()
        {
            var activatedOffers = new List<Offer>();
            foreach (var offer in _pendingOffers)
            {
                if (offer.OfferData.ActiveOffer())
                {
                    ActivatedOffers.Add(offer);
                    activatedOffers.Add(offer);
                }
            }

            foreach (var offer in activatedOffers)
                _pendingOffers.Remove(offer);
        }

        public bool RemoveActiveOffer(Offer offer, bool removeUserData)
        {
            if (ActivatedOffers.Remove(offer))
            {
                if (removeUserData)
                {
                    var userData = UserData.Instance;
                    userData.PromotionOfferData.RemoveOffer(offer.OfferData);
                    userData.Save();
                }

                Object.FindAnyObjectByType<PnlPromotionOfferButtons>()?.UpdateOffers();

                return true;
            }

            return false;
        }

        public bool RemoveActiveOffer(string offerId, bool removeUserData)
        {
            var removeds = ActivatedOffers.RemoveAll(x => x.OfferData.Id == offerId);
            if (removeds > 0)
            {
                if (removeUserData)
                {
                    var userData = UserData.Instance;
                    userData.PromotionOfferData.RemoveOffer(offerId);
                    userData.Save();
                }

                Object.FindAnyObjectByType<PnlPromotionOfferButtons>()?.UpdateOffers();

                return true;
            }

            return false;
        }

        public void HandleReceivedBannerMessage(BannerMessageData messageData)
        {
            var data = messageData.CustomData;
            if (data == null)
                return;

            if (!data.TryGetValue(InAppMessageDataKey.Action, out var action))
                return;

            switch (action)
            {
                case MessageActionKey.ActiveOffer:
                {
                    var offer = GetOfferData(data);
                    if (string.IsNullOrEmpty(offer.Id))
                    {
                        this.LogError("[ActiveOffer] Offer id is missing in the message data.");
                        return;
                    }

                    if (offer.IsNeedToBeRemoved())
                    {
                        this.Log($"Offer {offer.Id} is met removed condition.");
                        return;
                    }

                    var offerDefinition = GetOfferDefinition(offer);
                    if (offerDefinition == null)
                    {
                        this.LogError($"[ActiveOffer] Offer Definition for {offer.Id} is not found.");
                        return;
                    }

                    var userData = UserData.Instance;
                    var existedOffers = userData.PromotionOfferData;
                    if (existedOffers.AddOffer(offer))
                    {
                        _pendingOffers.Add(new Offer
                        {
                            OfferData = offer,
                            OfferDefinition = offerDefinition
                        });
                        _pendingOffers.Sort((x, y) => y.OfferData.Priority - x.OfferData.Priority);

                        this.Log($"Offer {offer.Id} added successfully.");
                    }
                    else
                    {
                        this.LogWarning($"Offer {offer.Id} already exists.");
                    }
                }
                    break;

                case MessageActionKey.FreeGift:
                    break;

                default:
                    this.LogWarning($"Action {action} not supported.");
                    break;
            }
        }

        protected override void Init()
        {
            _pendingOffers = new();
            ActivatedOffers = new();

            var userData = UserData.Instance;
            var existedOffers = userData.PromotionOfferData;
            var offersToBeRemoved = new List<OfferData>();

            foreach (var data in existedOffers.Offers)
            {
                if (data.IsNeedToBeRemoved())
                {
                    offersToBeRemoved.Add(data);
                    continue;
                }

                var definition = GetOfferDefinition(data);
                if (definition == null)
                {
                    this.LogError($"Offer Definition for {data.Id} is not found.");
                    continue;
                }

                if (data.IsActivated && data.IsExpired())
                    continue;

                var offer = new Offer()
                {
                    OfferData = data,
                    OfferDefinition = definition
                };

                if (data.IsActivated)
                    ActivatedOffers.Add(offer);
                else
                    _pendingOffers.Add(offer);
            }

            ActivatedOffers.Sort((x, y) => y.OfferData.Priority - x.OfferData.Priority);
            _pendingOffers.Sort((x, y) => y.OfferData.Priority - x.OfferData.Priority);

            foreach (var offer in offersToBeRemoved)
            {
                existedOffers.RemoveOffer(offer);
            }
            if (offersToBeRemoved.Count > 0)
                userData.Save();
        }

        private OfferData GetOfferData(Dictionary<string, string> data)
        {
            // Offer id
            data.TryGetValue(InAppMessageDataKey.OfferId, out var offerId);

            // Priority
            data.TryGetValue(InAppMessageDataKey.Priority, out var priorityStr);
            int.TryParse(priorityStr, out var priority);

            // Duration in hours
            data.TryGetValue(InAppMessageDataKey.DurationInHours, out var durationStr);
            int.TryParse(durationStr, out var durationInHours);
            var durationInSeconds = durationInHours > 0 ? durationInHours * 3600 : 0;

            // Unlock level
            data.TryGetValue(InAppMessageDataKey.UnlockLevel, out var unlockLevelStr);
            int.TryParse(unlockLevelStr, out var unlockLevel);

            // Conditions
            data.TryGetValue(InAppMessageDataKey.ActiveConditions, out var activeConditions);
            data.TryGetValue(InAppMessageDataKey.RemoveConditions, out var removeConditions);

            // Custom Data
            data.TryGetValue(InAppMessageDataKey.CustomData, out var customData);

            return new OfferData(offerId, priority, durationInSeconds, unlockLevel, activeConditions, removeConditions, customData);
        }

        private OfferDefinition GetOfferDefinition(OfferData offerData)
        {
            if (offerData == null)
                return null;

            OfferDefinition offerDefinition = null;
            if (!string.IsNullOrEmpty(offerData.CustomData))
                offerDefinition = CreateOfferDefinition(offerData.CustomData);

            if (offerDefinition == null)
                offerDefinition = DesignDataHolder.Instance?.PredefinedOfferData?.Get(offerData.Id);

            return offerDefinition;
        }

        private OfferDefinition CreateOfferDefinition(string customData)
        {
            this.LogError("Custom offer is not supported yet.");
            return null;
        }

        public class Offer
        {
            public OfferData OfferData;
            public OfferDefinition OfferDefinition;
        }
    }
}
