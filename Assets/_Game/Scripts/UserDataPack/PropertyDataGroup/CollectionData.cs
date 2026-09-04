using System;
using Design.Ids;
using Design.Structures;
using UnityEngine;
using UserDataPack.Structures.CollectionData;

namespace UserDataPack.PropertyDataGroup
{
    [Serializable]
    public class CollectionData : IUserDataPropertyDataGroup
    {
        public SingleIdCollectionData<int> Avatars = new();
        public SingleIdCollectionData<int> AvatarFrames = new();
        public SingleIdCollectionData<int> ProfileBanners = new();

        public SingleIdCollectionData<int> CustomizeQueen = new();
        public SingleIdCollectionData<int> CustomizeX = new();

        public void FixData()
        {
            if (Avatars == null)
                Avatars = new();
            if (!Avatars.HasOwned(0))
                Avatars.AddOwned(0);

            if (AvatarFrames == null)
                AvatarFrames = new();
            if (!AvatarFrames.HasOwned(0))
                AvatarFrames.AddOwned(0);

            if (ProfileBanners == null)
                ProfileBanners = new();
            if (!ProfileBanners.HasOwned(0))
                ProfileBanners.AddOwned(0);

            if (CustomizeQueen == null)
                CustomizeQueen = new();
            if (!CustomizeQueen.HasOwned(0))
                CustomizeQueen.AddOwned(0);

            if (CustomizeX == null)
                CustomizeX = new();
            if (!CustomizeX.HasOwned(0))
                CustomizeX.AddOwned(0);
        }

        public bool AddSingleIdCollection<T>(string collectionType, T id)
        {
            if (id is not int collectionId)
            {
                Debug.LogError($"Id type {id.GetType()} not supported.");
                return false;
            }

            switch (collectionType)
            {
                case ItemId.Avatar:
                    return Avatars.AddOwned(collectionId);

                case ItemId.AvatarFrame:
                    return AvatarFrames.AddOwned(collectionId);

                case ItemId.ProfileBanner:
                    return ProfileBanners.AddOwned(collectionId);

                case ItemId.CustomizeQueen:
                    return CustomizeQueen.AddOwned(collectionId);

                case ItemId.CustomizeX:
                    return CustomizeX.AddOwned(collectionId);

                default:
                    Debug.LogError($"Collection type {collectionType} not supported.");
                    return false;
            }
        }

        public bool AddCollectionItem(Item item)
        {
            if (item is not CollectionItem collectionItem)
            {
                Debug.LogError($"Item is not CollectionItem.");
                return false;
            }

            switch (collectionItem)
            {
                case SingleIdCollectionItem<int> singleIdCollectionItem:
                    return AddSingleIdCollection(singleIdCollectionItem.CollectionType, singleIdCollectionItem.CollectionId);

                default:
                    Debug.LogError($"Collection item type {collectionItem.GetType()} not supported.");
                    return false;
            }
        }
    }
}