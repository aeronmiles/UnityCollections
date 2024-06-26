using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Newtonsoft.Json;
using System.Threading.Tasks;

public static class DataUtil
{
  public static bool Load<T>(ref T dataOut, string path) where T : class
  {
    if (path.ToLower().Contains(".json"))
      return LoadJson(out dataOut, path);
    else
      return LoadBinary(out dataOut, path);
  }

  private static bool LoadJson<T>(out T dataOut, string path) where T : class
  {
    object data = new { };
    if (File.Exists(path))
    {
      Debug.Log($"DataUtil -> Load(path={path}) :: Data loading");
      try
      {
        using StreamReader r = new StreamReader(path);
        var _data = JsonConvert.DeserializeObject<T>(r.ReadToEnd());
        data = _data;
        Debug.Log($"DataUtil -> Load(path={path}) :: Data loaded:\n" + _data);
      }
      catch (Exception ex)
      {
        Debug.Log($"DataUtil -> Load(path={path}) :: Error loading data: " + ex.Message);
        dataOut = null;
        return false;
      }
    }

    if (data is T t)
    {
      LogAsJson(path, t);
      dataOut = t;
      return true;
    }
    else
    {
      Debug.Log($"DataUtil -> Load(path={path}) :: Data is empty");
      dataOut = null;
      return false;
    }
  }

  public static bool LoadTexture(out Texture2D tex, string path)
  {
    try
    {
      byte[] fileData = File.ReadAllBytes(path);
      Texture2D texture = new Texture2D(2, 2);
      _ = texture.LoadImage(fileData); //..this will auto-resize the texture dimensions.
      tex = texture;
      return true;
    }
    catch (Exception ex)
    {
      Debug.LogError($"DataUtil -> LoadTexture(path={path}) :: error: " + ex.Message);
      tex = default;
      return false;
    }
  }

  private static bool LoadBinary<T>(out T dataOut, string path) where T : class
  {
    object data = new { };
    if (File.Exists(path))
    {
      FileStream dataFile = null;
      try
      {
        BinaryFormatter b = new BinaryFormatter();
        dataFile = File.Open(path, FileMode.Open);
        data = b.Deserialize(dataFile);
        dataFile.Close();
      }
      catch (Exception ex)
      {
        Debug.Log($"DataUtil -> Load(path={path}) :: Error loading data: " + ex.Message);
        if (dataFile != null) dataFile.Close();
        dataOut = null;
        return false;
      }
    }

    if (data is T t)
    {
      LogAsJson(path, t);
      dataOut = t;
      return true;
    }
    else
    {
      Debug.Log($"DataUtil -> Load(path={path}) :: Data is empty");
      dataOut = null;
      return false;
    }
  }

  public static bool Save<T>(T data, string path) where T : class
  {
    if (path.ToLower().Contains(".json"))
      return SaveJson(data, path);
    else
      return SaveBinary(data, path);
  }

  public static bool SaveString(string jsonString, string filePath)
  {
    try
    {
#if DEBUG
      Debug.Log($"DataUtil -> Save(path={filePath}) :: Data saving\n" + jsonString);
#endif
      using StreamWriter writer = new StreamWriter(filePath);
      writer.Write(jsonString);
      writer.Close();
      return true;
    }
    catch (Exception ex)
    {
      Debug.Log($"DataUtil -> Save(path={filePath}) :: Error saving data: " + ex.Message);
      return false;
    }
  }

  public static async Task<bool> SaveStringAsync(string jsonString, string filePath)
  {
    try
    {
#if DEBUG
      Debug.Log($"DataUtil -> Save(path={filePath}) :: Data saving\n" + jsonString);
#endif
      using (StreamWriter writer = new StreamWriter(filePath))
      {
        await writer.WriteAsync(jsonString);
      }
      Debug.Log($"DataUtil -> Save(path={filePath}) :: Data saved successfully");
      return true;
    }
    catch (Exception ex)
    {
      Debug.Log($"DataUtil -> Save(path={filePath}) :: Error saving data: " + ex.Message);
      return false;
    }
  }

  public static void Delete(string dataPath)
  {
    if (File.Exists(dataPath))
      File.Delete(dataPath);
  }

  private static bool SaveJson<T>(T data, string path) where T : class
  {
    try
    {
#if DEBUG
      Debug.Log($"DataUtil -> SaveJson(path={path}) :: Data saving, Type: {typeof(T)}\n" + JsonUtility.ToJson(data, true));
#endif
      using StreamWriter writer = new StreamWriter(path);
      writer.Write(JsonConvert.SerializeObject(data));
      writer.Close();
      return true;
    }
    catch (Exception ex)
    {
      Debug.Log($"DataUtil -> SaveJson(path={path}) :: Error saving data: " + ex.Message);
      return false;
    }
  }

  private static bool SaveBinary<T>(T data, string path) where T : class
  {
    try
    {
#if DEBUG
      Debug.Log($"DataUtil -> SaveBinary(path={path}) :: Data saving, Type: {typeof(T)}\n" + JsonUtility.ToJson(data));
#endif
      BinaryFormatter b = new BinaryFormatter();
      FileStream dataFile = File.Create(path);
      b.Serialize(dataFile, data);
      dataFile.Close();
      return true;
    }
    catch (Exception ex)
    {
      Debug.Log($"DataUtil -> SaveBinary(path={path}) :: Error saving data: " + ex.Message);
      return false;
    }
  }

  private static void LogAsJson<T>(string path, T data)
  {
#if UNITY_EDITOR
    // Attempt to output app data to console; don't break if we fail
    try
    {
      // Debug.Log($"DataUtil -> Load(path={path}) :: Data loaded:\n" + JsonConvert.SerializeObject(data));
    }
    catch
    {
    }
#endif
  }
}

public interface IDataSerializer
{
  bool Save<T>(T data, string filePath) where T : class, new();
  bool Load<T>(ref T data, string filePath) where T : class, new();
}

public class DataSerializer : IDataSerializer
{
  public bool Save<T>(T data, string filePath) where T : class, new()
  {
    return DataUtil.Save(data, filePath);
  }

  public bool Load<T>(ref T data, string filePath) where T : class, new()
  {
    return DataUtil.Load(ref data, filePath);
  }
}