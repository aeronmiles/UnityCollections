using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MonoSingletonScene<T> : MonoBehaviour where T : Component
{
    static Type s_type = typeof(T);

    static Dictionary<int, Dictionary<Type, object>> s_instances = new();

    public static T I(Scene scene) => s_instances[scene.buildIndex][s_type] as T;

    public virtual void Awake()
    {
        CheckSceneInstance(s_type, gameObject.scene.buildIndex);
        AfterAwake();
    }

    protected virtual void AfterAwake()
    {

    }

    void CheckSceneInstance<T>(T typeOf, int sceneIndex) where T : Type
    {
        Debug.Log($"MonoSingleton<{typeOf}> -> CheckSceneInstance(sceneIndex = {sceneIndex})");
        if (s_instances.ContainsKey(sceneIndex))
        {
            if (s_instances[sceneIndex].ContainsKey(typeOf))
            {
                Debug.Log($"MonoSingletonScene<{typeOf}> Awake() :: Destroy", this);
                DestroyImmediate(gameObject);
            }
        }

        s_instances.Add(sceneIndex, new());
        s_instances[sceneIndex].Add(typeOf, this);
    }

}
