using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class UIGroup : MonoBehaviour
{
    [SerializeField] protected UIGroupTransition _displayTransition;

    protected Action<UIGroup> _onHide;

    public virtual void Show(object data = null, Action onCompleted = null)
    {
        if (gameObject.activeSelf)
            return;

        if (_displayTransition != null)
        {
            gameObject.SetActive(true);
            _displayTransition.Show(() =>
            {
                onCompleted?.Invoke();
            });
        }
        else
        {
            gameObject.SetActive(true);
            onCompleted?.Invoke();
        }
    }

    public virtual void Hide()
    {
        if (!gameObject.activeSelf)
            return;

        if (_displayTransition != null)
        {
            _onHide?.Invoke(this);
            _displayTransition.Hide(() =>
            {
                gameObject.SetActive(false);
            });
        }
        else
        {
            gameObject.SetActive(false);
            _onHide?.Invoke(this);
        }
    }

    public virtual void UpdateUI(object data)
    {

    }

    public virtual int GetTransitionPhase()
    {
        if (_displayTransition != null)
            return _displayTransition.GetTransitionPhase();

        return UITransitionPhase.Complete;
    }

    public void RegisterOnHide(Action<UIGroup> onHide)
    {
        this._onHide += onHide;
    }

    public void UnregisterOnHide(Action<UIGroup> onHide)
    {
        this._onHide -= onHide;
    }
}