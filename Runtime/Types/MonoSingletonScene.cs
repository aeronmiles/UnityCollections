using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MonoSingletonScene<T> : MonoBehaviour where T : Component
{
    static Type s_Type = typeof(T);
    static Dictionary<int, Dictionary<Type, object>> s_Instances = new();

    public static T I(Scene scene)
    {
        int sceneIndex = scene.buildIndex;

        CheckInstance(sceneIndex);

        if (s_Instances[sceneIndex][s_Type] != null)
            return s_Instances[sceneIndex][s_Type] as T;

        T i = null;
#if UNITY_EDITOR
        i = FindObjectOfType<T>();
#endif
        if (i == null)
            i = new GameObject(typeof(T).Name).AddComponent<T>();

        s_Instances[sceneIndex][s_Type] = i;

        return i;
    }

    public virtual void Awake()
    {
        s_Instances = new();
        CheckInstance(gameObject.scene.buildIndex, this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void CheckInstance(int sceneIndex, object instance = null)
    {
        if (!s_Instances.ContainsKey(sceneIndex))
            s_Instances.Add(sceneIndex, new());

        if (!s_Instances[sceneIndex].ContainsKey(s_Type))
            s_Instances[sceneIndex].Add(s_Type, instance);
    }

}