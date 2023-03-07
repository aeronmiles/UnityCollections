using System;
using System.IO;
using UnityEngine;

public static class AppDataSerializer
{
    public static event Action OnDataReady = null;
    public static string FileName { get; private set; }

    public static AppData AppData { get; private set; } = null;
    public static bool DataReady { get; private set; } = AppData != null;

    private static string m_path => Path.Combine(Application.persistentDataPath, FileName);

    public static void Load<T>(string fileName) where T : AppData, new()
    {
        Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes"); // for ios
        FileName = fileName;
        loadData<T>();
    }

    private static void loadData<T>() where T : AppData, new()
    {
        if (DataUtil.Load(out T data, m_path))
            AppData = data;
        else
            AppData = new T();

#if UNITY_EDITOR
        // AppData = new AppData();
        // PlayerPrefs.DeleteAll();
#endif
        DataReady = true;
        OnDataReady?.Invoke();
    }

    public static void Save()
    {
        DataUtil.Save(AppData, m_path);
    }

    public static void Reset<T>() where T : AppData, new()
    {
        Debug.LogWarning("DataSerializer ** Resetting AppData");
        AppData = new T();
        Save();
    }
}
