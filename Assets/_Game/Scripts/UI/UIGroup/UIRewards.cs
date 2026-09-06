using System;
using System.Collections.Generic;
using Design.Ids;
using Design.Structures;
using DG.Tweening;
using Lean.Pool;
using MEC;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class UIRewards : UIGroup
{
    [SerializeField] private Transform _rewardContainer;
    [SerializeField] private UIItem _rewardPrefab;
    [SerializeField] private UIItem _profileBannerRewardPrefab;
    [SerializeField] private Button _btnClose;

    [Header("Animation Settings")]
    [SerializeField] private float _scaleAnimDuration = 0.3f;
    [SerializeField] private float _delayBetweenItems = 0.1f;
    [SerializeField] private Ease _scaleEase = Ease.OutBack;

    [Header("Claim Effect Settings")]
    [SerializeField] private UIResourceEffectTarget _defaultTarget;
    [SerializeField] private float _flyDuration = 1f;

    private List<UIItem> _rewardItems = new();
    private CoroutineHandle _animationHandle;
    private Data _currentData;

    public override void Show(object data = null, Action onCompleted = null)
    {
        base.Show(data, onCompleted);

        if (data is Data d)
        {
            _currentData = d;

            foreach (var item in _rewardItems)
            {
                if (item != null) Destroy(item.gameObject);
            }

            _rewardItems.Clear();

            for (var i = 0; i < d.Rewards.Count; i++)
            {
                var rewardData = d.Rewards[i];
                var prefabToUse = _rewardPrefab;
                if (rewardData is CollectionItem { CollectionType: ItemId.ProfileBanner })
                    prefabToUse = _profileBannerRewardPrefab;

                var rewardItem = Instantiate(prefabToUse, _rewardContainer);
                _rewardItems.Add(rewardItem);

                rewardItem.SetItem(rewardData);
                rewardItem.transform.localScale = Vector3.zero;
                rewardItem.transform.localPosition = Vector3.zero;
                rewardItem.gameObject.SetActive(true);
            }

            // Start the animation
            Timing.KillCoroutines(_animationHandle);
            _animationHandle = Timing.RunCoroutine(StartShowRewardsAnimation());
        }
    }

    private IEnumerator<float> StartShowRewardsAnimation()
    {
        if (_currentData == null)
        {
            _btnClose.gameObject.SetActive(true);
            yield break;
        }
        _btnClose.gameObject.SetActive(false);

        AudioManager.Instance?.PlaySFXOneShot("item_spawn_uirewards");

        var uiRewardsToShow = new List<UIItem>();
        for (int i = 0; i < _currentData.Rewards.Count && i < _rewardItems.Count; i++)
        {
            var rewardItem = _rewardItems[i];
            rewardItem.transform.DOScale(Vector3.one, _scaleAnimDuration).SetEase(_scaleEase);
            uiRewardsToShow.Add(rewardItem);
            yield return Timing.WaitForSeconds(_delayBetweenItems);
        }
        yield return Timing.WaitForSeconds(_scaleAnimDuration);

        if (uiRewardsToShow.Count > 0)
        {
            EffectHelper.SpawnClaimItemsEffect(uiRewardsToShow, _defaultTarget, _flyDuration, transform, Hide);
        }
        else
        {
            _btnClose.gameObject.SetActive(true);
        }
    }

    public override void Hide()
    {
        Timing.KillCoroutines(_animationHandle);
        _animationHandle = Timing.RunCoroutine(StartHideAnimation());
    }

    private IEnumerator<float> StartHideAnimation()
    {
        _btnClose.gameObject.SetActive(false);
        if (_currentData == null)
        {
            base.Hide();
            yield break;
        }

        for (int i = 0; i < _currentData.Rewards.Count && i < _rewardItems.Count; i++)
        {
            var rewardItem = _rewardItems[i];
            rewardItem.transform.DOScale(0, _scaleAnimDuration).SetEase(Ease.InBack);
        }
        yield return Timing.WaitForSeconds(_scaleAnimDuration);

        _currentData?.OnClosed?.Invoke();
        base.Hide();
    }

    private void OnDisable()
    {
        Timing.KillCoroutines(_animationHandle);
    }

    public class Data
    {
        public List<Item> Rewards;
        public Action OnClosed;
    }

#if UNITY_EDITOR
    [Button]
    private void ShowWithRewards(params Item[] rewards)
    {
        Show(new Data() { Rewards = new(rewards) });
    }
#endif
}
