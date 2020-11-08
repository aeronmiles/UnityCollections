using System;
using System.Collections.Generic;

public class TypeHashSet<T0>
{
    public Dictionary<Type, HashSet<T0>> Set
    {
        get;
        private set;
    } = new Dictionary<Type, HashSet<T0>>();

    public HashSet<T0> All { get; private set; } = new HashSet<T0>();

    public void Add<T>(T item) where T : T0
    {
        if (!Set.ContainsKey(typeof(T)))
            Set.Add(typeof(T), new HashSet<T0>());

        Set[typeof(T)].Add(item);
        All.Add(item);
    }

    public void Remove<T>(T item) where T : T0
    {
        if (!Set.ContainsKey(typeof(T))) return;

        Set[typeof(T)].Remove(item);
        Set[typeof(T)].TrimExcess();
        All.Remove(item);
        All.TrimExcess();
    }

    public HashSet<T0> Get<T>() where T : T0
    {
        foreach (var k in Set.Keys)
        {
            if (k == typeof(T)) return Set[k];
        }

        return null;
    }

    public HashSet<T0> GetAll() => All;
}