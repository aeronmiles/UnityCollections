using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : Component
{
  private static T _Instance = null;
  public static T I
  {
    get
    {
      if (_Instance != null) return _Instance;
      _Instance = FindObjectOfType<T>();
      if (_Instance != null) return _Instance;
      _Instance = new GameObject(typeof(T).Name).AddComponent<T>();
      return _Instance;
    }
  }

  public virtual void Awake()
  {
    if (_Instance == null)
    {
      _Instance = this as T;
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
