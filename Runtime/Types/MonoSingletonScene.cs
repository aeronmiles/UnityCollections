using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MonoSingletonScene<T> : MonoBehaviour where T : Component
{
    static readonly Type _type = typeof(T);
    static Dictionary<int, Dictionary<Type, object>> _instances = new();

    public static T I(Scene scene)
    {
        int sceneIndex = scene.buildIndex;

        CheckInstance(sceneIndex);

        if (_instances[sceneIndex][_type] != null)
            return _instances[sceneIndex][_type] as T;

        T i = null;
#if UNITY_EDITOR
        i = FindObjectOfType<T>();
#endif
        if (i == null)
            i = new GameObject(typeof(T).Name).AddComponent<T>();

        _instances[sceneIndex][_type] = i;

        return i;
    }

    public virtual void Awake()
    {
        _instances = new();
        CheckInstance(gameObject.scene.buildIndex, this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void CheckInstance(int sceneIndex, object instance = null)
    {
        if (!_instances.ContainsKey(sceneIndex))
            _instances.Add(sceneIndex, new());

        if (!_instances[sceneIndex].ContainsKey(_type))
            _instances[sceneIndex].Add(_type, instance);
    }

}