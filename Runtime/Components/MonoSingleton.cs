using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : Component
{
  private static T _Instance;
  private static readonly object _Lock = new object();
  private static bool _applicationIsQuitting = false;

  public static T I
  {
    get
    {
      lock (_Lock)
      {
        if (_Instance == null)
        {
          if (_applicationIsQuitting)
          {
            // Return the existing instance if any, but do not create a new one
            return _Instance;
          }
          _Instance = FindObjectOfType<T>(true);
          if (_Instance == null)
          {
            GameObject singletonObject = new GameObject();
            _Instance = singletonObject.AddComponent<T>();
            singletonObject.name = typeof(T).ToString() + " (Singleton)";
            if (Application.isPlaying)
            {
              DontDestroyOnLoad(singletonObject);
            }
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
        if (Application.isPlaying)
        {
          DontDestroyOnLoad(gameObject);
        }
      }
      else if (_Instance != this)
      {
        ServiceManager.I.logger.LogWarning("MonoSingleton", $"Instance of {typeof(T)} already exists. Destroying duplicate instance.", this);
        Destroy(gameObject); // Destroy duplicate instance
      }
    }
  }

  protected virtual void OnDestroy()
  {
    if (!_applicationIsQuitting && _Instance == this)
    {
      _Instance = null;
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
