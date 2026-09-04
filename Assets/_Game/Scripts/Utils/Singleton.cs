using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SingletonComponent<T> : MonoBehaviour where T : Component
{
    public static T Instance
    {
        get;
        private set;
    }

    protected virtual void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);

            Debug.Log("Duplicate singleton has been destroyed!");

            return;
        }
        else
        {
            Instance = GetComponent<T>();
        }
    }
}

public abstract class Singleton<T> where T: Singleton<T>, new()
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new T();
                instance.Init();
            }   
            return instance;
        }
    }

    protected virtual void Init()
    {

    }
}