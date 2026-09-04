namespace Game.InappMessageHandlers
{
    public abstract class InAppMessageDataKey
    {
        public const string Action = "Action";
        public const string OfferId = "OfferId";
        public const string Priority = "Priority";
        public const string DurationInHours = "DurationInHours";
        public const string CustomData = "CustomData";
        public const string ActiveConditions = "ActiveConditions";
        public const string RemoveConditions = "RemoveConditions";
        public const string UnlockLevel = "UnlockLevel";
        public const string Items = "Items";
        public const string PopupImageUrl = "PopupImageUrl";
    }

    public abstract class MessageActionKey
    {
        public const string ActiveOffer = "ActiveOffer";
        public const string FreeGift = "FreeGift";
    }
}