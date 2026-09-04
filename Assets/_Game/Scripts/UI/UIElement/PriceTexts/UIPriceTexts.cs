using System;
using Design.Structures;
using UnityEngine;

[Serializable]
public abstract class UIPriceTexts : MonoBehaviour
{
    public abstract void SetPrice(Price price, params object[] args);
}
