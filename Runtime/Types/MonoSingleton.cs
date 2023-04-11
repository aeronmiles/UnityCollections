using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : Component
{
    private static T s_instance = null;
    public static T I
    {
        get
        {
            if (s_instance != null) return s_instance;
            s_instance = new GameObject(typeof(T).Name).AddComponent<T>();
            return s_instance;
        }
    }

    public virtual void Awake()
    {
        if (s_instance == null)
        {
            s_instance = this as T;
            if (Application.isPlaying)
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
