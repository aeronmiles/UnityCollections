using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct DataVersion
{
  public int major { get; set; }
  public int minor { get; set; }
  public int patch { get; set; }

  public DataVersion(int major = 1, int minor = 0, int patch = 0)
  {
    this.major = major;
    this.minor = minor;
    this.patch = patch;
  }

  public readonly bool IsNewer(DataVersion other) =>
      (major > other.major) ||
      (major == other.major && minor > other.minor) ||
      (major == other.major && minor == other.minor && patch > other.patch);
}

[Serializable]
public class Data
{
  public DataVersion version { get; private set; } = new DataVersion();
  public List<object> classes { get; private set; } = new List<object>();

  public void UpdateVersion(DataVersion newVersion)
  {
    if (newVersion.IsNewer(version))
    {
      version = newVersion;
    }
  }
}

public interface IVersionedData
{
  int dataVersion { get; }
  void Migrate(int fromVersion);
  string ToJson();
  void FromJson(string json);
  bool Validate();
}

public static class DataLoader
{
  private static readonly object _Lock = new object();
  private static Data _Data = new Data();

  public static DataVersion CurrentVersion { get; } = new DataVersion(1, 0, 0);

  private static string DataFilePath => Path.Combine(Application.persistentDataPath, "data.dat");
  private static string BackupFilePath => DataFilePath + ".backup";
  private static string JsonConfigPath => Path.Combine(Application.persistentDataPath, "config.json");
  private static string JsonBackupPath => JsonConfigPath + ".backup";

  public static void Load<T>(Action<T> onDataLoaded, bool useJsonConfig = false) where T : class, IVersionedData, new()
  {
    if (onDataLoaded == null)
    {
      throw new ArgumentNullException(nameof(onDataLoaded));
    }

    try
    {
      Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes"); // for iOS
      if (useJsonConfig)
      {
        LoadJsonConfig(onDataLoaded);
      }
      else
      {
        LoadData(onDataLoaded);
      }
    }
    catch (Exception ex)
    {
      Debug.LogError($"Failed to load data: {ex.Message}");
      AttemptDataRecovery(onDataLoaded, useJsonConfig);
    }
  }

  private static void LoadData<T>(Action<T> onDataLoaded) where T : class, IVersionedData, new()
  {
    lock (_Lock)
    {
      if (_Data.HasType<T>())
      {
        var data = _Data.GetDataOfType<T>();
        if (data != null)
        {
          MigrateDataIfNeeded(data);
          onDataLoaded(data);
          Save();
          return;
        }
      }

      try
      {
        CreateBackup(DataFilePath, BackupFilePath);
        _ = IOUtil.Load(ref _Data, DataFilePath);
        _Data ??= new Data();

        // Update data version
        _Data.UpdateVersion(CurrentVersion);

        if (!_Data.HasType<T>())
        {
          _Data.SetDataOfType(new T());
        }

        var loadedData = _Data.GetDataOfType<T>();
        MigrateDataIfNeeded(loadedData);
        onDataLoaded(loadedData);
        Save();
      }
      catch (Exception ex)
      {
        Debug.LogError($"Error in LoadData<{typeof(T).Name}>: {ex.Message}");
        throw;
      }
    }
  }

  private static void LoadJsonConfig<T>(Action<T> onDataLoaded) where T : class, IVersionedData, new()
  {
    lock (_Lock)
    {
      try
      {
        if (File.Exists(JsonConfigPath))
        {
          CreateBackup(JsonConfigPath, JsonBackupPath);
          string jsonContent = File.ReadAllText(JsonConfigPath);
          var data = new T();
          data.FromJson(jsonContent);
          MigrateDataIfNeeded(data);
          onDataLoaded(data);

          // Update the binary data store as well
          _Data.SetDataOfType(data);
          Save();
        }
        else
        {
          // If no JSON config exists, try loading from binary
          LoadData(onDataLoaded);
        }
      }
      catch (Exception ex)
      {
        Debug.LogError($"Error loading JSON config: {ex.Message}");
        throw;
      }
    }
  }

  private static void MigrateDataIfNeeded<T>(T data) where T : IVersionedData
  {
    if (data == null)
    {
      return;
    }

    int currentDataVersion = data.dataVersion;
    if (currentDataVersion < CurrentVersion.major)
    {
      try
      {
        Debug.Log($"Migrating data from version {currentDataVersion} to {CurrentVersion.major}");
        data.Migrate(currentDataVersion);
        Save(); // Save after successful migration
      }
      catch (Exception ex)
      {
        Debug.LogError($"Migration failed: {ex.Message}");
        throw;
      }
    }
  }

  private static void CreateBackup(string sourcePath, string backupPath)
  {
    if (File.Exists(sourcePath))
    {
      try
      {
        File.Copy(sourcePath, backupPath, true);
      }
      catch (Exception ex)
      {
        Debug.LogError($"Failed to create backup: {ex.Message}");
      }
    }
  }

  private static void AttemptDataRecovery<T>(Action<T> onDataLoaded, bool useJsonConfig = false) where T : class, IVersionedData, new()
  {
    try
    {
      if (useJsonConfig && File.Exists(JsonBackupPath))
      {
        Debug.Log("Attempting to recover from JSON backup...");
        File.Copy(JsonBackupPath, JsonConfigPath, true);
        LoadJsonConfig(onDataLoaded);
        return;
      }
      else if (File.Exists(BackupFilePath))
      {
        Debug.Log("Attempting to recover from binary backup...");
        File.Copy(BackupFilePath, DataFilePath, true);
        LoadData(onDataLoaded);
        return;
      }
    }
    catch (Exception ex)
    {
      Debug.LogError($"Recovery failed: {ex.Message}");
    }

    // If recovery fails or no backup exists, create new data
    var newData = new T();
    onDataLoaded(newData);
    Save();
  }

  public static void Save()
  {
    lock (_Lock)
    {
      try
      {
        CreateBackup(DataFilePath, BackupFilePath);
        _ = IOUtil.Save(_Data, DataFilePath);
      }
      catch (Exception ex)
      {
        Debug.LogError($"Failed to save data: {ex.Message}");
      }
    }
  }

  public static void SaveAsJson<T>(T data) where T : class, IVersionedData, new()
  {
    if (data == null)
    {
      throw new ArgumentNullException(nameof(data));
    }

    lock (_Lock)
    {
      try
      {
        CreateBackup(JsonConfigPath, JsonBackupPath);
        string jsonContent = data.ToJson();
        File.WriteAllText(JsonConfigPath, jsonContent);

        // Also update the binary store
        _Data.SetDataOfType(data);
        Save();
      }
      catch (Exception ex)
      {
        Debug.LogError($"Failed to save JSON config: {ex.Message}");
      }
    }
  }

  public static void Save<T>(T data) where T : class, IVersionedData, new()
  {
    if (data == null)
    {
      throw new ArgumentNullException(nameof(data));
    }

    lock (_Lock)
    {
      _Data.SetDataOfType(data);
      Save();
    }
  }

  public static void Reset<T>() where T : class, IVersionedData, new()
  {
    lock (_Lock)
    {
      Debug.LogWarning($"DataLoader -> Reset<{typeof(T).Name}>() :: Resetting data!");
      _Data.SetDataOfType(new T());
      Save();
    }
  }
}

public static class DataExt
{
  public static T GetDataOfType<T>(this Data data) where T : class, IVersionedData, new()
  {
    if (data?.classes == null)
    {
      return null;
    }

    return data.classes.Find(d => d is T) as T;
  }

  public static void SetDataOfType<T>(this Data data, T newData) where T : class, IVersionedData, new()
  {
    if (data?.classes == null)
    {
      throw new ArgumentNullException(nameof(data));
    }

    if (newData == null)
    {
      throw new ArgumentNullException(nameof(newData));
    }

    int index = data.classes.FindIndex(d => d is T);
    if (index >= 0)
    {
      data.classes[index] = newData;
    }
    else
    {
      data.classes.Add(newData);
    }
  }

  public static void RemoveDataOfType<T>(this Data data) where T : class, IVersionedData
  {
    if (data?.classes == null)
    {
      return;
    }

    _ = data.classes.RemoveAll(d => d is T);
  }

  public static bool HasType<T>(this Data data) where T : class, IVersionedData, new()
  {
    if (data?.classes == null)
    {
      return false;
    }

    return data.classes.Exists(d => d is T);
  }
}

// Example implementation of a versioned data class with JSON support
[Serializable]
public class PlayerDataMigrationExampleClass : IVersionedData
{
  public int dataVersion { get; private set; } = 1;
  public string playerName { get; set; }
  public int score { get; set; }
  public int level { get; set; }
  public List<string> achievements { get; set; } = new List<string>();

  public string ToJson()
  {
    var settings = new JsonSerializerSettings
    {
      Formatting = Formatting.Indented,
      NullValueHandling = NullValueHandling.Ignore
    };
    return JsonConvert.SerializeObject(this, settings);
  }

  public void FromJson(string json)
  {
    var jsonData = JsonConvert.DeserializeObject<PlayerDataMigrationExampleClass>(json);
    if (jsonData != null)
    {
      dataVersion = jsonData.dataVersion;
      playerName = jsonData.playerName;
      score = jsonData.score;
      level = jsonData.level;
      achievements = jsonData.achievements ?? new List<string>();
    }
  }

  public void Migrate(int fromVersion)
  {
    switch (fromVersion)
    {
      case 1:
        MigrateV1ToV2();
        goto case 2;
      case 2:
        // MigrateV2ToV3();
        break;
      default:
        throw new ArgumentException($"Unknown version {fromVersion}");
    }
    dataVersion = DataLoader.CurrentVersion.major;
  }

  private void MigrateV1ToV2()
  {
    level = 1;
    achievements ??= new List<string>();
    Debug.Log($"Migrated PlayerData from V1 to V2 for player {playerName}");
  }

  public bool Validate() => throw new NotImplementedException();
}