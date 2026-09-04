using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager : SingletonComponent<UIManager>
{
    [SerializeField] protected GraphicRaycaster _graphicRaycaster;
    [SerializeField] protected UITopBar _uiTopBar;
    [SerializeField] protected UILoading _uiLoading;
    [SerializeField] protected List<UIGroup> uiGroups = new List<UIGroup>();

    private GameObject _inputBlocker;

    protected UIGroup currentGroup = null;
    protected List<UIGroup> currentOverlayGroups = new ();
    protected Action<UIGroup> onUIGroupHided;
    protected Action<UIGroup> onUIGroupShowed;

    public UIGroup TopUIGroup => currentOverlayGroups.Count > 0 ? currentOverlayGroups[^1] : currentGroup;
    public UITopBar TopBar => _uiTopBar;

    public bool IsScreenPositionOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;
        var eventData = new PointerEventData(EventSystem.current) { position = screenPos };
        var results = new List<RaycastResult>();
        _graphicRaycaster.Raycast(eventData, results);
        return results.Count > 0;
    }

    public void SetInputBlocker(bool isActive)
    {
        if (_inputBlocker == null)
        {
            _inputBlocker = new GameObject("InputBlocker");
            var rect = _inputBlocker.AddComponent<RectTransform>();
            rect.SetParent(_graphicRaycaster.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            var img = _inputBlocker.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            img.raycastTarget = true;
        }
        
        if (isActive)
        {
            _inputBlocker.transform.SetAsLastSibling();
            _inputBlocker.SetActive(true);
        }
        else
        {
            _inputBlocker.SetActive(false);
        }
    }

    public void ShowLoading(UILoading.Data data = null)
    {
        _uiLoading.Show(data);
    }

    public void HideLoading()
    {
        _uiLoading.Hide();
    }

    public async void ShowUIGroup<T>(object data = null, Action onComplete = null) where T : UIGroup
    {
        var g = await FindGroup<T>();
        if (g != null)
        {
            currentGroup?.Hide();
            g.Show(data, onComplete);
            currentGroup = g;
            OnUIGroupShowed(currentGroup);
        }
        else
        {
            Debug.LogError("Not found group type: " + typeof(T));
        }
    }

    public void ShowUIGroupOverlay<T>(object data = null, Action onComplete = null) where T : UIGroup
    {
        ShowUIGroupOverlay(typeof(T), data, onComplete);
    }

    public async void ShowUIGroupOverlay(Type type, object data = null, Action onComplete = null)
    {
        var g = await FindGroup(type);
        if (g != null)
        {
            g.Show(data, onComplete);
            g.transform.SetAsLastSibling();
            currentOverlayGroups.Remove(g);
            currentOverlayGroups.Add(g);
            OnUIGroupShowed(g);
        }
        else
        {
            Debug.LogError("Not found group type: " + type);
        }
    }

    public void ShowUIGroupUnderlay<T>(object data = null, Action onComplete = null) where T : UIGroup
    {
        ShowUIGroupUnderlay(typeof(T), data, onComplete);
    }

    public async void ShowUIGroupUnderlay(Type type, object data = null, Action onComplete = null)
    {
        var g = await FindGroup(type);
        if (g != null)
        {
            g.Show(data, onComplete);
            g.transform.SetAsFirstSibling();
            currentOverlayGroups.Remove(g);
            currentOverlayGroups.Insert(0, g);
            OnUIGroupShowed(g);
        }
        else
        {
            Debug.LogError("Not found group type: " + type);
        }
    }

    public async void HideUIGroup<T>() where T : UIGroup
    {
        var g = await FindGroup<T>();
        if (g != null)
        {
            g.Hide();
        }
        else
        {
            Debug.LogError("Not found group type: " + typeof(T));
        }
    }

    public void HideAllUI()
    {
        currentGroup?.Hide();
        currentOverlayGroups.ForEach(x => x.Hide());
    }

    public virtual UniTask<UIGroup> FindGroup(Type type)
    {
        var result = uiGroups.Find(x => x.GetType() == type);
        return UniTask.FromResult(result);
    }

    public async UniTask<T> FindGroup<T>() where T : UIGroup
    {
        var result = await FindGroup(typeof(T));
        return result == null ? null : (T)result;
    }

    #region Register/Unregister Events
    public void RegisterOnUIGroupShowed(Action<UIGroup> action)
    {
        onUIGroupShowed += action;
    }

    public void UnRegisterOnUIGroupShowed(Action<UIGroup> action)
    {
        onUIGroupShowed -= action;
    }

    public void RegisterOnUIGroupHided(Action<UIGroup> action)
    {
        onUIGroupHided += action;
    }

    public void UnRegisterOnUIGroupHided(Action<UIGroup> action)
    {
        onUIGroupHided -= action;
    }
    #endregion

    protected void OnUIGroupShowed(UIGroup uiGroup)
    {
        #if UNITY_EDITOR
        PrintUIGroupStack($"<color=#59FF00>Show</color>: {uiGroup?.GetType().Name}");
        #endif
        
        onUIGroupShowed?.Invoke(uiGroup);
    }

    protected void OnUIGroupHided(UIGroup uiGroup)
    {
        if (!currentOverlayGroups.Remove(uiGroup))
            if (currentGroup == uiGroup)
                currentGroup = null;

        #if UNITY_EDITOR
        PrintUIGroupStack($"<color=#AD25EA>Hide</color>: {uiGroup?.GetType().Name}");
        #endif
        
        onUIGroupHided?.Invoke(uiGroup);
    }

    protected void PrintUIGroupStack(string prefix = "")
    {
        #if UNITY_EDITOR
        Debug.Log($"[UIManager] -- {prefix} -- Stack: {(currentGroup != null ? currentGroup.GetType().Name + ", " : string.Empty)}{string.Join(", ", currentOverlayGroups.ConvertAll(x => x.GetType().Name))}");
        #endif
    }

    protected override void Awake()
    {
        base.Awake();
        uiGroups.ForEach(x =>
        {
            x.RegisterOnHide(OnUIGroupHided);
            x.Hide();
        });
    }
}