using Design.DataHolder;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HomeFeatureButton : MonoBehaviour
{
    [SerializeField] private FeatureType _featureType = FeatureType.None;
    [SerializeField] private Button _btn;

    public UnityEvent OnClicked;

    protected virtual void Awake()
    {
        _btn.onClick.AddListener(HandleClick);
    }

    protected virtual void OnDestroy() { }

    protected virtual void HandleClick()
    {
        OnClicked?.Invoke();
    }

    public virtual void Setup(int currentLevel)
    {
        if (_featureType == FeatureType.None)
        {
            gameObject.SetActive(true);
            return;
        }

        var unlockData = Design.DesignDataHolder.Instance.FeatureUnlockData;
        gameObject.SetActive(unlockData.IsUnlocked(_featureType, currentLevel));
    }
}
