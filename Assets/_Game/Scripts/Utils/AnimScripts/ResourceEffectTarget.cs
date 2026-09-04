using System;
using UnityEngine;

public class ResourceEffectTarget : MonoBehaviour
{
    [SerializeField] private Transform _position;
    
    public virtual Vector3 Position => _position.position;
    
    public virtual float PlaySendAnim(Action onComplete = null)
    {
        return 0f;
    }
    
    public virtual float PlayReceiveAnim(Action onComplete = null)
    {
        return 0f;
    }
}
