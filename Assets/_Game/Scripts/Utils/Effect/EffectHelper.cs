using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AssetsHolder;
using Design.Ids;
using Design.Structures;
using Lean.Pool;
using UnityEngine;
using UnityEngine.UI;

public class EffectHelper
{
    public static async void SpawnClaimItemEffect(GameObject spawner, IEffectTarget target, Item item, int amount, float duration, Transform parent = null, Action onComplete = null)
    {
        var effect = await ResourcesHolder.Instance.EffectPrefabs.GetPrefabAsync(EffectPrefabs.EffectCollectItem);
        if (effect == null)
        {
            Debug.LogError($"Effect prefab not found: {EffectPrefabs.EffectCollectItem}");
            onComplete?.Invoke();
            return;
        }

        var effectInstance = LeanPool.Spawn(effect, parent);
        effectInstance.transform.position = spawner.transform.position;
        var effectComponent = effectInstance.GetComponent<CollectItemEffect>();
        if (effectComponent != null)
        {
            var hasUIItem = spawner != null && spawner.GetComponent<UIItem>() != null;
            var customSprite = hasUIItem || spawner == null ? null : spawner.GetComponent<Image>()?.sprite;
            if (customSprite != null)
            {
                effectComponent.SetCustomSprite(customSprite);
            }

            effectComponent.Play(target, item, amount, duration, () =>
            {
                LeanPool.Despawn(effectInstance);
                onComplete?.Invoke();
            });
        }
        else
        {
            Debug.LogError($"CollectItemEffect component not found on prefab: {EffectPrefabs.EffectCollectItem}");
            LeanPool.Despawn(effectInstance);
            onComplete?.Invoke();
        }
    }

    public static async void SpawnClaimItemsEffect(List<UIItem> uiItems, IEffectTarget defautTarget, float duration, Transform parent = null, Action onComplete = null, Func<string, IEffectTarget> getEffectTarget = null)
    {
        var uiTopBar = UIManager.Instance.TopBar;
        var uiResourceCounters = uiTopBar?.TryShow(new UITopBar.Data()
        {
            SubtractValues = uiItems.ConvertAll(r => new KeyValue<string, int>()
            {
                Key = r.Item.Id,
                Value = r.Item.Amount
            })
        });

        var effectCount = uiItems.Count;
        foreach (var ui in uiItems)
        {
            var target = getEffectTarget?.Invoke(ui.Item.Id);
            target ??= uiResourceCounters?.Find(c => c.ResourceId == ui.Item.Id)?.GetComponent<IEffectTarget>() ?? defautTarget;
            var amount = ItemId.IsTimedItem(ui.Item.Id) || ItemId.IsCollectionItem(ui.Item.Id) ? 1 : ui.Item.Amount;

            SpawnClaimItemEffect(ui.gameObject, target, ui.Item, amount, duration, parent, () =>
            {
                effectCount--;
            });
        }

        var timeout = duration + 10f;
        while (effectCount > 0 && timeout > 0f)
        {
            await Task.Yield();
            timeout -= Time.deltaTime;
        }

        if (timeout > 0f)
            await Task.Delay(500); // Extra delay to ensure all effects are completed

        onComplete?.Invoke();
    }
}
