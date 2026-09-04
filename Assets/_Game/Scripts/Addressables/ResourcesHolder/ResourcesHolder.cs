using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Design.DataHolder;
using Design.Ids;
using Design.Structures;
using UnityEngine;
using UserDataPack;

namespace AssetsHolder
{
    public class ResourcesHolder : SingletonComponent<ResourcesHolder>
    {
        public ThemeData ThemeData;
        public ItemIcons ItemIcons;
        public ShopItemIcons ShopItemIcons;
        public CurrencyIcons CurrencyIcons;
        public EffectPrefabs EffectPrefabs;

        // Profile images
        public SpriteDataList AvatarIcons;
        public SpriteDataList AvatarFrames;
        public SpriteDataList ProfileBanners;
        public SpriteDataList RankBadges;

        // Customize items
        public SpriteDataList CustomizeQueen;
        public SpriteDataList CustomizeX;

        // SkeletonData
        public SkeletonDataList QueenSkeletonDatas;

        public Task<Sprite> GetItemSpriteAsync(Item item)
        {
            var tcs = new TaskCompletionSource<Sprite>();
            GetItemSprite(item, tcs.SetResult);
            return tcs.Task;
        }

        public Task<Sprite> GetItemSpriteAsync(string itemId)
        {
            var tcs = new TaskCompletionSource<Sprite>();
            GetItemSprite(itemId, tcs.SetResult);
            return tcs.Task;
        }

        public void GetItemSprite(Item item, Action<Sprite> callback)
        {
            switch (item)
            {
                case CollectionItem collectionItem:
                {
                    GetCollectionItemSprite(collectionItem, callback);
                    break;
                }

                default:
                {
                    GetItemSprite(item.Id, callback);
                    break;
                }
            }
        }

        public void GetItemSprite(string itemId, Action<Sprite> callback)
        {
            switch (itemId)
            {
                case ItemId.Booster_1:
                case ItemId.CustomizeQueen:
                    CustomizeQueen.GetSprite(UserData.Instance.CustomizeData.QueenId, callback);
                    break;

                default:
                    ItemIcons.GetSprite(itemId, callback);
                    break;
            }
        }

        public void GetCollectionItemSprite(CollectionItem item, Action<Sprite> callback)
        {
            switch (item)
            {
                case SingleIdCollectionItem<int> intIdItem:
                {
                    switch (intIdItem.CollectionType)
                    {
                        case ItemId.Avatar:
                            AvatarIcons.GetSprite(intIdItem.CollectionId, callback);
                            break;

                        case ItemId.AvatarFrame:
                            AvatarFrames.GetSprite(intIdItem.CollectionId, callback);
                            break;

                        case ItemId.ProfileBanner:
                            ProfileBanners.GetSprite(intIdItem.CollectionId, callback);
                            break;

                        case ItemId.CustomizeQueen:
                            CustomizeQueen.GetSprite(intIdItem.CollectionId, callback);
                            break;

                        case ItemId.CustomizeX:
                            CustomizeX.GetSprite(intIdItem.CollectionId, callback);
                            break;

                        default:
                            Debug.LogError($"Unsupported collection type: {intIdItem.CollectionType}");
                            break;
                    }
                    break;
                }

                default:
                    Debug.LogError($"Unsupported collection item type: {item.GetType()}");
                    break;
            }
        }

        public Task<Sprite> GetShopItemSpriteAsync(string shopItemId)
        {
            var tcs = new TaskCompletionSource<Sprite>();
            GetShopItemSprite(shopItemId, tcs.SetResult);
            return tcs.Task;
        }

        public void GetShopItemSprite(string shopItemId, Action<Sprite> callback)
        {
            if (ShopItemIcons.HasSpecialIdMapping(shopItemId, out var mappedId))
            {
                GetItemSprite(mappedId, callback);
            }
            else
            {
                ShopItemIcons.GetSprite(shopItemId, callback);
            }
        }
    }
}