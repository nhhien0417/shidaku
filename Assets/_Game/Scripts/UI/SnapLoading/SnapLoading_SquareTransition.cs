using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SnapLoading_SquareTransition : SnapLoading
{
    [SerializeField] private Material _renderer;
    [SerializeField] private float _showHideDuration;
    [SerializeField] private List<Data> _textures = new ();

    private Tween _tween;

    public override void Show(string customId = "", bool useAnim = true, Action callback = null)
    {
        gameObject.SetActive(true);
        _tween?.Kill();

        var data = _textures.Find(x => x.id == customId);
        if (data != null)
        {
            _renderer.SetTexture("_TransitionTex", data.texture);
        }

        if (useAnim)
        {
            _renderer.SetFloat("_Progress", 0f);
            _tween = DOVirtual.Float(0, 0.5f, _showHideDuration, (x) => _renderer.SetFloat("_Progress", x))
                .OnComplete(() => callback?.Invoke());
        }
        else
        {
            _renderer.SetFloat("_Progress", 0.5f);
            callback?.Invoke();
        }
    }

    public override void Hide()
    {
        _tween?.Kill();
        _renderer.SetFloat("_Progress", 0.5f);
        _tween = DOVirtual.Float(0.5f, 1f, _showHideDuration, (x) => _renderer.SetFloat("_Progress", x))
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
    }

    [Serializable]
    private class Data
    {
        public string id;
        public Texture texture;
    }
}
