using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class DataLoader
{
  private static Data m_data = new Data();

  private static string DataFilePath => Path.Combine(Application.persistentDataPath, "data.dat");

  public static void Load<T>(Action<T> onDataLoaded) where T : class, new()
  {
    Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes"); // for ios
    LoadData(onDataLoaded);
  }

  private static void LoadData<T>(Action<T> onDataLoaded) where T : class, new()
  {
    if (m_data.HasType<T>())
    {
      onDataLoaded?.Invoke(m_data.GetDataOfType<T>());
      Save();
      return;
    }

    DataUtil.Load(ref m_data, DataFilePath);

    if (m_data == null)
      m_data = new Data();

    if (!m_data.HasType<T>())
      m_data.SetDataOfType(new T());

    onDataLoaded?.Invoke(m_data.GetDataOfType<T>());
    Save();
  }

  public static void Save() => DataUtil.Save(m_data, DataFilePath);

  public static void Save<T>(T data) where T : class, new()
  {
    m_data.SetDataOfType(data);
    Save();
  }

  public static void Reset<T>() where T : class, new()
  {
    Debug.LogWarning("DataSerializer -> reset() :: Resetting data!");
    m_data.SetDataOfType(new T());
    Save();
  }
}

[Serializable]
public class Data
{
  public List<object> Classes = new List<object>();
}

public static class DataExt
{
  public static T GetDataOfType<T>(this Data data) where T : class, new()
  {
    foreach (var d in data.Classes)
    {
      if (d is T dt)
        return dt;
    }

    return null;
  }

  public static void SetDataOfType<T>(this Data data, T newData) where T : class, new()
  {
    int l = data.Classes.Count;
    for (int i = 0; i < l; i++)
    {
      if (data.Classes[i] is T)
      {
        data.Classes[i] = newData;
        return;
      }
    }

    data.Classes.Add(newData);
  }

  public static void RemoveDataOfType<T>(this Data data)
  {
    object toRemove = null;
    foreach (var d in data.Classes)
    {
      if (d is T)
      {
        toRemove = d;
        return;
      }
    }

    data.Classes.Remove(toRemove);
  }

  public static bool HasType<T>(this Data data) where T : class, new()
  {
    foreach (var d in data.Classes)
      if (d is T)
        return true;

    return false;
  }
}
