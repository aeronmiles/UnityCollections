<<<<<<< HEAD
using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : Component
{
    private static T _instance = null;
    public static T I
    {
        get
        {
            if (_instance != null) return _instance;
            _instance = new GameObject(typeof(T).Name).AddComponent<T>();
            return _instance;
        }
    }

    public virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(this);
            AfterAwake();
        }
        else
        {
            Debug.Log($"MonoSingleton<{typeof(T)}> Awake() :: Destroy", this);
            DestroyImmediate(gameObject);
        }
    }

    protected virtual void AfterAwake()
    {
        
    }

}
=======
using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : Component
{
    private static T _instance = null;

    private static readonly object _lock = new();
    public static T I
    {
        get
        {
            lock (_lock)
            {
                if (_instance != null) return _instance;
                if (Application.isPlaying)
                    _instance = new GameObject(typeof(T).Name).AddComponent<T>();
                return _instance;
            }
        }
    }

    public virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(this);
        }
        else
        {
            Debug.Log($"MonoSingleton<{typeof(T)}> Awake() :: Destroy", this);
            DestroyImmediate(gameObject);
        }
    }

}
>>>>>>> 9c31c7c3bd33c3d67b5c9a1a3d71d3be10f4427e
