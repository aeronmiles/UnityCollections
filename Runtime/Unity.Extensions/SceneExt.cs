#if !UNITY_EDITOR
using UnityEngine.SceneManagement;
#endif

public static class SceneExtensions
{
  public static bool SceneLoaded(string name)
  {
#if UNITY_EDITOR
    for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; ++i)
    {
      var scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);

      if (scene.name == name) return true;
    }
    return false;
#else
        for (int i = 0; i < SceneManager.sceneCount; ++i)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == name) return true;
        }
        return false;
#endif
  }
}