using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : Component
{
  private static T _Instance;
  private static readonly object _Lock = new object();

  public static T I
  {
    get
    {
      lock (_Lock)
      {
        if (_Instance == null)
        {
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
        Debug.LogWarning($"[Singleton] An instance of {typeof(T)} already exists.");
      }
    }
  }

  protected virtual void OnDestroy()
  {
    if (_Instance == this)
    {
      _Instance = null;
    }
    Debug.Log($"MonoSingleton<{typeof(T)}> OnDestroy()");
  }

  private void OnValidate()
  {
    if (_Instance == null)
    {
      _Instance = this as T;
    }
  }
}
