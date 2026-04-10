using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : Component
{
  private static T _Instance;
  private static readonly object _Lock = new object();
  private static bool _applicationIsQuitting = false;
  private static bool _isDestroyed = false;

  public static T I
  {
    get
    {
      lock (_Lock)
      {
        if (_Instance == null)
        {
          if (_applicationIsQuitting || _isDestroyed)
          {
            return null;
          }
          _Instance = FindObjectOfType<T>(true);
          if (_Instance == null && Application.isPlaying)
          {
            GameObject singletonObject = new GameObject();
            _Instance = singletonObject.AddComponent<T>();
            singletonObject.name = typeof(T).ToString() + " (Singleton)";
            DontDestroyOnLoad(singletonObject);
          }
        }
        return _Instance;
      }
    }
  }

  protected virtual void Awake()
  {
    lock (_Lock)
    {
      if (_Instance == null)
      {
        _Instance = this as T;
        _isDestroyed = false;
        if (Application.isPlaying)
        {
          DontDestroyOnLoad(gameObject);
        }
      }
      else if (_Instance != this)
      {
        Debug.LogWarning($"[MonoSingleton] Instance of {typeof(T)} already exists. Destroying duplicate instance.");
        Destroy(gameObject);
      }
    }
  }

  protected virtual void OnDestroy()
  {
    if (_Instance == this)
    {
      _Instance = null;
      _isDestroyed = true;
    }
    // ServiceManager.I.logger.Log($"MonoSingleton<{typeof(T)}>", "OnDestroy()", this);
  }

  protected virtual void OnApplicationQuit()
  {
    _applicationIsQuitting = true;
  }

  private void OnValidate()
  {
    if (_Instance == null)
    {
      _Instance = this as T;
    }
  }
}
