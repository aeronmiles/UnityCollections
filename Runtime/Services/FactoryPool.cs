
using System;
using System.Collections.Generic;
using UnityEngine.Pool;

public interface IFactoryPool<TFactory, TObject> : IEquatable<TFactory> where TObject : class
{
  TObject Create();
  TObject Get();
  void Release(TObject obj);
}

public class FactoryPool<TFactory, TObject> where TFactory : IFactoryPool<TFactory, TObject> where TObject : class
{
  private readonly Dictionary<TFactory, ObjectPool<TObject>> pools
      = new();

  public TObject Get(TFactory factory)
  {
    if (!pools.TryGetValue(factory, out var pool))
    {
      pool = new ObjectPool<TObject>(factory.Create);
      pools.Add(factory, pool);
    }
    return pool.Get();
  }

  public void Release(TObject obj, TFactory descriptor)
  {
    if (pools.ContainsKey(descriptor))
    {
      pools[descriptor].Release(obj);
    }
    // Otherwise, it's likely a new object type, let it be garbage collected
  }
}
