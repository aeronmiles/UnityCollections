using UnityEngine;

public class MonoSingletonScene<T> : MonoBehaviour where T : Component
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
