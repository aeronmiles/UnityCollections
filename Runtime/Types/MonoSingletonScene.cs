using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MonoSingletonScene<T> : MonoBehaviour where T : Component
{
  private static readonly Dictionary<string, T> _instances = new Dictionary<string, T>();
  private static readonly object _lock = new object(); // Synchronization lock object

  public static T I(Scene scene)
  {
    if (!scene.isLoaded)
    {
      return null;
    }
    string sceneName = scene.name;
    lock (_lock)
    {
      if (!_instances.ContainsKey(sceneName))
      {
        // Find the instance in the specified scene
        T instance = FindInstanceInScene(scene);
        if (instance == null)
        {
          // Create new instance if none was found
          instance = new GameObject($"{typeof(T).Name} (Scene Singleton)").AddComponent<T>();
          SceneManager.MoveGameObjectToScene(instance.gameObject, scene);
        }
        _instances[sceneName] = instance;
      }
    }

    return _instances[sceneName];
  }

  private static T FindInstanceInScene(Scene scene)
  {
    foreach (GameObject rootObj in scene.GetRootGameObjects())
    {
      T instance = rootObj.GetComponentInChildren<T>(true);
      if (instance != null)
      {
        return instance;
      }
    }
    return null;
  }

  protected virtual void Awake()
  {
    string sceneName = gameObject.scene.name;
    lock (_lock)
    {
      if (_instances.ContainsKey(sceneName) && _instances[sceneName] != this)
      {
        Debug.LogError($"Another instance of {typeof(T)} was attempted to be created in {sceneName}, which is not allowed.");
        DestroyImmediate(gameObject);
      }
      else
      {
        _instances[sceneName] = this as T;
      }
    }
  }

  protected virtual void OnDestroy()
  {
    string sceneName = gameObject.scene.name;
    lock (_lock)
    {
      if (_instances.ContainsKey(sceneName) && _instances[sceneName] == this)
      {
        _instances.Remove(sceneName);
      }
    }
  }
}
