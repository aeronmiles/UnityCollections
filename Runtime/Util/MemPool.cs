using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MemPool
{
    static Dictionary<Type, HashSet<object>> m_Free = new();
    static Dictionary<Type, HashSet<object>> m_Used = new();


    public static T Add<T>(T item, int count = 1) where T : class
    {
        var type = typeof(T);
#if DEBUG
        if (item == null)
            Debug.LogError($"Pool -> Add(item=null) :: Trying to add null to pool");
#endif
        if (!m_Free.ContainsKey(type))
        {
            m_Free.Add(type, new HashSet<object>());
            m_Used.Add(type, new HashSet<object>());
        }
        for (var i = 0; i < count; i++)
            m_Free[type].Add(item.DeepCopy());

        return item;
    }

    public static T Remove<T>(T item) where T : class
    {
        var type = typeof(T);
        if (m_Free.ContainsKey(type))
        {
            m_Free[type].Remove(item);
            m_Used[type].Remove(item);
        }
        return item;
    }

    public static T Get<T>() where T : class
    {
        var type = typeof(T);
        if (!m_Free.ContainsKey(type))
        {
            m_Free.Add(type, new HashSet<object>());
            m_Used.Add(type, new HashSet<object>());
        }

        if (m_Free[type].Count == 0)
        {
            if (type.IsSubclassOf(typeof(MonoBehaviour)))
            {
                Debug.LogError("MemPool -> Get<T>() :: Pool doesn't support instantiating MonoBehaviour types");
                return null;
            }
            var newItem = Activator.CreateInstance<T>();
            m_Used[type].Add(newItem);
            return newItem;
        }
        else
        {
            var item = m_Free[type].ElementAt(0);
            m_Free[type].Remove(item);
            m_Used[type].Add(item);
            var itemOut = (T)item;
#if DEBUG
            if (itemOut == null)
                Debug.LogError($"MemPool -> Get<{type}>() :: return item is NULL");
#endif
            return itemOut;
        }
    }

    public static T Free<T>(T item) where T : class
    {
        var type = typeof(T);
        m_Used[type].Remove(item);
        m_Free[type].Add(item);
        return item;
    }

    public static void DisposeAllOfType<T>() where T : class
    {
        var type = typeof(T);
        if (m_Free.ContainsKey(type))
        {
            m_Free[type].Clear();
            m_Used[type].Clear();
        }
    }

    public static void DisposeAll()
    {
        m_Used = new();
        m_Free = new();
    }
}

public static class MemPoolExt
{
    public static T AddTo_MemPool<T>(this T item) where T : class
    {
        MemPool.Add(item);
        return item;
    }

    public static IEnumerable<T> AddTo_MemPool_Many<T>(this IEnumerable<T> items) where T : class
    {
        foreach (var i in items) MemPool.Add(i);
        return items;
    }

    public static T RemoveFrom_MemPool<T>(this T item) where T : class
    {
        MemPool.Remove(item);
        return item;
    }

    public static IEnumerable<T> RemoveFrom_MemPool_Many<T>(this IEnumerable<T> items) where T : class
    {
        foreach (var i in items) MemPool.Remove(i);
        return items;
    }

    public static T FreeTo_MemPool<T>(this T item) where T : class
    {
        MemPool.Free(item);
        return item;
    }

    public static IEnumerable<T> FreeTo_MemPool_Many<T>(this IEnumerable<T> items) where T : class
    {
        foreach (var i in items) MemPool.Free(i);
        return items;
    }

}