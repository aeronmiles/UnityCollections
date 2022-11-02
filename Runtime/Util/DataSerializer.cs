using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class DataSerializer
{
    public static event Action OnDataReady = null;
    public static string FileName { get; private set; }

    public static AppData AppData { get; private set; } = null;
    public static bool DataReady { get; private set; } = AppData != null;

    public static void Load<T>(string fileName = "data.db") where T : AppData, new()
    {
        Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes"); // for ios
        FileName = fileName;
        loadData<T>();
    }

    private static void loadData<T>() where T : AppData, new()
    {
        string path = Path.Combine(Application.persistentDataPath, FileName);
        Debug.Log(path);
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
                Debug.Log("DataSerializer ** Error loading data: " + ex.Message);
                if (dataFile != null) dataFile.Close();
            }
        }

        if (data is T t)
        {
#if UNITY_EDITOR
            // Attempt to output app data to console; don't break if we fail
            try
            {
                Debug.Log("DataSerializer ** Data loaded: " + JsonUtility.ToJson(data));
            }
            catch
            {
            }
#endif
            AppData = t;
        }
        else
        {
            Debug.Log("DataSerializer ** Data is empty");
            AppData = new T();
            Save();
        }
#if UNITY_EDITOR
        // AppData = new AppData();
        // PlayerPrefs.DeleteAll();
#endif
        DataReady = true;
        OnDataReady?.Invoke();
    }

    public static void Save()
    {
        string path = Path.Combine(Application.persistentDataPath, FileName);
        try
        {
            Debug.Log("DataSerializer ** Data saving: " + JsonUtility.ToJson(AppData));

            BinaryFormatter b = new BinaryFormatter();
            FileStream dataFile = File.Create(path);
            b.Serialize(dataFile, AppData);
            dataFile.Close();
        }
        catch (Exception ex)
        {
            Debug.Log("DataSerializer ** Error saving data: " + ex.Message);
        }
    }

    public static void Reset<T>() where T : AppData, new()
    {
        Debug.LogWarning("DataSerializer ** Resetting AppData");
        AppData = new T();
        Save();
    }
}
