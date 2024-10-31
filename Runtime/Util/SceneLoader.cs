using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoSingleton<SceneLoader>
{
  [SerializeField] private GameObject m_loadingScreen;
  public static event Action<Scene, LoadSceneMode> OnSceneLoaded;
  public static event Action<Scene> OnSceneUnloaded;

  private new void Awake()
  {
    SceneManager.sceneLoaded += OnSceneLoadedEvent;
    SceneManager.sceneUnloaded += OnSceneUnloadedEvent;
    m_loadingScreen.SetActive(false);
  }

  private new void OnDestroy()
  {
    SceneManager.sceneLoaded -= OnSceneLoadedEvent;
    SceneManager.sceneUnloaded -= OnSceneUnloadedEvent;
  }

  private void OnSceneLoadedEvent(Scene scene, LoadSceneMode mode)
  {
    OnSceneLoaded?.Invoke(scene, mode);
  }

  private void OnSceneUnloadedEvent(Scene scene)
  {
    OnSceneUnloaded?.Invoke(scene);
  }

  public static void Load(string sceneName, bool showLoading, Action<Scene, LoadSceneMode> onSceneLoaded = null)
  {
    Debug.Log($"SceneLoader -> Load(sceneName = {sceneName})");
    _ = I.StartCoroutine(BeginLoadScene(sceneName, showLoading, onSceneLoaded));
  }

  public static void UnloadScene(string sceneName, Action<Scene> onSceneUnloaded = null)
  {
    Debug.Log($"SceneLoader -> UnloadScene(sceneName = {sceneName})");
    _ = I.StartCoroutine(BeginUnloadScene(sceneName, onSceneUnloaded));
  }

  private static IEnumerator BeginUnloadScene(string sceneName, Action<Scene> onSceneUnloaded = null)
  {
    if (SceneExtensions.SceneLoaded(sceneName))
    {
      yield return SceneManager.UnloadSceneAsync(sceneName);
      onSceneUnloaded?.Invoke(SceneManager.GetSceneByName(sceneName));
      print($"RenderableSceneLoader ** Scene '{sceneName}' Unloaded");
    }
  }

  public static void AddScene(string sceneName, Action<Scene, LoadSceneMode> onSceneLoaded = null)
  {
    Debug.Log($"SceneLoader -> AddScene(sceneName = {sceneName})");
    _ = I.StartCoroutine(BeginAddScene(sceneName, onSceneLoaded));
  }

  private static IEnumerator BeginAddScene(string sceneName, Action<Scene, LoadSceneMode> onSceneLoaded = null)
  {
    yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    onSceneLoaded?.Invoke(SceneManager.GetSceneByName(sceneName), LoadSceneMode.Additive);
  }

  private static IEnumerator BeginLoadScene(string sceneName, bool showLoading, Action<Scene, LoadSceneMode> onSceneLoaded = null)
  {
    I.m_loadingScreen.SetActive(showLoading);
    if (showLoading)
      yield return new WaitForSeconds(0.25f);

    yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    I.m_loadingScreen.SetActive(false);
    onSceneLoaded?.Invoke(SceneManager.GetSceneByName(sceneName), LoadSceneMode.Single);
  }
}
