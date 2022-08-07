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
