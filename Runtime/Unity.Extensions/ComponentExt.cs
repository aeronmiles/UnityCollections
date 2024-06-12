using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class ComponentExt
{
  public static T GetCopyOf<T>(this Component comp, T other) where T : Component
  {
    Type type = comp.GetType();
    if (type != other.GetType()) return null; // type mis-match
    BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
    PropertyInfo[] pinfos = type.GetProperties(flags);
    int l = pinfos.Length;
    for (int i = 0; i < l; i++)
    {
      if (pinfos[i].CanWrite)
      {
        try
        {
          pinfos[i].SetValue(comp, pinfos[i].GetValue(other, null), null);
        }
        catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
      }
    }
    FieldInfo[] finfos = type.GetFields(flags);
    l = finfos.Length;
    for (int i = 0; i < l; i++)
    {
      finfos[i].SetValue(comp, finfos[i].GetValue(other));
    }
    return comp as T;
  }

  public static T SetActive<T>(this T obj, bool active) where T : Component
  {
    obj.gameObject.SetActive(active);
    return obj;
  }

  public static IEnumerable<T> SetActive<T>(this IEnumerable<T> objs, bool active) where T : Component
  {
    foreach (var obj in objs)
    {
      if (obj != null)
        obj.gameObject.SetActive(active);
      else
        Debug.LogWarning("Trying to set active a null object");
    }
    return objs;
  }
}