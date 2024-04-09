
using System;
using System.Collections.Generic;
using UnityEngine;

public interface IID
{
  string id { get; }
}

[Serializable]
public class IDData<T> : IID
{
  public IDData(string id, T data)
  {
    _id = id;
    this.data = data;
  }

  [SerializeField] private string _id;
  public string id => _id.ToString();
  public T data;

  public static implicit operator T(IDData<T> idData)
  {
    return idData.data;
  }
}

public static class InterfaceExt
{
  public static void CheckDuplicates(this IEnumerable<IID> ids)
  {
    var idList = new List<string>();
    foreach (var id in ids)
    {
      if (idList.Contains(id.id))
      {
        Debug.LogError($"Duplicate id: {id.id}");
      }
      idList.Add(id.id);
    }
  }

  // @TODO: optimize to lookup by id
  public static void Set<T>(this List<T> ids, T obj) where T : IID
  {
    for (int i = 0; i < ids.Count; i++)
    {
      if (ids[i].id == obj.id)
      {
        ids[i] = obj;
        return;
      }
    }
    ids.Add(obj);
  }

  // @TODO: optimize to lookup by id
  public static bool TryGet<T>(this IEnumerable<IID> ids, string id, out T obj)
  {
    foreach (var i in ids)
    {
      if (i.id == id)
      {
        obj = (T)i;
        return true;
      }
    }
    obj = default;
    return false;
  }
}