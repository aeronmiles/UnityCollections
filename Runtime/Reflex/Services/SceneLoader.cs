using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DI
{
  public interface ISceneLoader
  {
    /// <summary>
    /// Fired when a scene is loaded.
    /// </summary>
    event Action<Scene, LoadSceneMode> OnSceneLoaded;

    /// <summary>
    /// Fired when a scene is unloaded.
    /// </summary>
    event Action<Scene> OnSceneUnloaded;

    /// <summary>
    /// Loads the specified scene (single mode) and optionally shows a loading screen.
    /// </summary>
    void Load(string sceneName, bool showLoading, Action<Scene, LoadSceneMode> onSceneLoaded = null);

    /// <summary>
    /// Unloads the specified scene.
    /// </summary>
    void UnloadScene(string sceneName, Action<Scene> onSceneUnloaded = null);

    /// <summary>
    /// Adds a scene additively.
    /// </summary>
    void AddScene(string sceneName, Action<Scene, LoadSceneMode> onSceneLoaded = null);
  }

  public class SceneLoader : MonoBehaviour, ISceneLoader
  {
    [SerializeField] private GameObject _loadingScreen;

    // Instance events; subscribers will receive notifications.
    public event Action<Scene, LoadSceneMode> OnSceneLoaded = delegate { };
    public event Action<Scene> OnSceneUnloaded = delegate { };

    private void Awake()
    {
      // Subscribe to SceneManager events.
      SceneManager.sceneLoaded += OnSceneLoadedEvent;
      SceneManager.sceneUnloaded += OnSceneUnloadedEvent;
      if (_loadingScreen != null)
        _loadingScreen.SetActive(false);
    }

    private void OnDestroy()
    {
      SceneManager.sceneLoaded -= OnSceneLoadedEvent;
      SceneManager.sceneUnloaded -= OnSceneUnloadedEvent;
    }

    private void OnSceneLoadedEvent(Scene scene, LoadSceneMode mode) =>
        OnSceneLoaded?.Invoke(scene, mode);

    private void OnSceneUnloadedEvent(Scene scene) =>
        OnSceneUnloaded?.Invoke(scene);

    /// <summary>
    /// Loads a scene in Single mode.
    /// </summary>
    public void Load(string sceneName, bool showLoading, Action<Scene, LoadSceneMode> onSceneLoadedCallback = null)
    {
      Debug.Log($"SceneLoader -> Load(sceneName = {sceneName})");
      StartCoroutine(BeginLoadScene(sceneName, showLoading, onSceneLoadedCallback));
    }

    /// <summary>
    /// Unloads the specified scene.
    /// </summary>
    public void UnloadScene(string sceneName, Action<Scene> onSceneUnloadedCallback = null)
    {
      Debug.Log($"SceneLoader -> UnloadScene(sceneName = {sceneName})");
      StartCoroutine(BeginUnloadScene(sceneName, onSceneUnloadedCallback));
    }

    /// <summary>
    /// Loads a scene additively.
    /// </summary>
    public void AddScene(string sceneName, Action<Scene, LoadSceneMode> onSceneLoadedCallback = null)
    {
      Debug.Log($"SceneLoader -> AddScene(sceneName = {sceneName})");
      StartCoroutine(BeginAddScene(sceneName, onSceneLoadedCallback));
    }

    private IEnumerator BeginUnloadScene(string sceneName, Action<Scene> onSceneUnloadedCallback = null)
    {
      if (SceneExtensions.SceneLoaded(sceneName))
      {
        yield return SceneManager.UnloadSceneAsync(sceneName);
        // Note: After unloading, GetSceneByName might return an invalid Scene.
        onSceneUnloadedCallback?.Invoke(SceneManager.GetSceneByName(sceneName));
        Debug.Log($"SceneLoader -> Scene '{sceneName}' Unloaded");
      }
    }

    private IEnumerator BeginAddScene(string sceneName, Action<Scene, LoadSceneMode> onSceneLoadedCallback = null)
    {
      yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
      onSceneLoadedCallback?.Invoke(SceneManager.GetSceneByName(sceneName), LoadSceneMode.Additive);
    }

    private IEnumerator BeginLoadScene(string sceneName, bool showLoading, Action<Scene, LoadSceneMode> onSceneLoadedCallback = null)
    {
      if (_loadingScreen != null)
        _loadingScreen.SetActive(showLoading);

      if (showLoading)
        yield return new WaitForSeconds(0.25f);

      yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

      if (_loadingScreen != null)
        _loadingScreen.SetActive(false);

      onSceneLoadedCallback?.Invoke(SceneManager.GetSceneByName(sceneName), LoadSceneMode.Single);
    }
  }
}
