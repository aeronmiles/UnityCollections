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
          _Instance = FindObjectOfType<T>();
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

  private void OnValidate()
  {
    if (_Instance == null)
    {
      _Instance = this as T;
    }
  }

  protected virtual void Awake()
  {
    lock (_Lock)
    {
      if (_Instance == null)
      {
        _Instance = this as T;
      }
      else if (FindObjectsOfType<MonoSingleton<T>>().Length > 1)
      {
        Debug.LogError($"Another instance of {typeof(MonoSingleton<T>)} was attempted to be created, which is not allowed.");
        DestroyImmediate(gameObject);
      }
    }
  }

  public virtual void OnDestroy()
  {
    Debug.Log($"MonoSingleton<{typeof(T)}> OnDestroy()");
    if (_Instance == this)
    {
      _Instance = null;
    }
  }
}
