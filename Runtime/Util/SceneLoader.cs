using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoSingleton<SceneLoader>
{
    [SerializeField] GameObject m_loadingScreen;
    public static event Action<Scene, LoadSceneMode> OnSceneLoaded;
    public static event Action<Scene> OnSceneUnloaded;

    private new void Awake()
    {
        base.Awake();
        SceneManager.sceneLoaded += onSceneLoadedEvent;
        SceneManager.sceneUnloaded += onSceneUnloadedEvent;
        m_loadingScreen.SetActive(false);
    }

    private void Destroy()
    {
        SceneManager.sceneLoaded -= onSceneLoadedEvent;
        SceneManager.sceneUnloaded -= onSceneUnloadedEvent;
    }

    private void onSceneLoadedEvent(Scene scene, LoadSceneMode mode)
    {
        OnSceneLoaded?.Invoke(scene, mode);
    }

    private void onSceneUnloadedEvent(Scene scene)
    {
        OnSceneUnloaded?.Invoke(scene);
    }

    public static void Load(string sceneName, bool showLoading)
    {
        Debug.Log($"SceneLoader -> Load(sceneName = {sceneName})");
        I.StartCoroutine(beginLoadScene(sceneName, showLoading));
    }

    public static void UnloadScene(string sceneName)
    {
        Debug.Log($"SceneLoader -> UnloadScene(sceneName = {sceneName})");
        I.StartCoroutine(beginUnloadScene(sceneName));
    }

    private static IEnumerator beginUnloadScene(string sceneName)
    {
        if (SceneExtensions.SceneLoaded(sceneName))
        {
            yield return SceneManager.UnloadSceneAsync(sceneName);
            print($"RenderableSceneLoader ** Scene '{sceneName}' Unloaded");
        }
    }

    public static void AddScene(string sceneName)
    {
        Debug.Log($"SceneLoader -> AddScene(sceneName = {sceneName})");
        I.StartCoroutine(beginAddScene(sceneName));
    }

    private static IEnumerator beginAddScene(string sceneName)
    {
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    private static IEnumerator beginLoadScene(string sceneName, bool showLoading)
    {
        I.m_loadingScreen.SetActive(showLoading);
        if (showLoading)
            yield return new WaitForSeconds(0.25f);
            
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        I.m_loadingScreen.SetActive(false);
    }
}
