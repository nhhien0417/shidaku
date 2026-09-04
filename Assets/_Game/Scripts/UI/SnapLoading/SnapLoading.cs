using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public abstract class SnapLoading : MonoBehaviour
{
    public abstract void Show(string customId = "", bool useAnim = true, System.Action callback = null);

    public abstract void Hide();
}
