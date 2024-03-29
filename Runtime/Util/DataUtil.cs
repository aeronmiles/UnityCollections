using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

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
            try
            {
                using StreamReader r = new StreamReader(path);
                data = JsonUtility.FromJson<T>(r.ReadToEnd());
            }
            catch (Exception ex)
            {
                Debug.Log($"DataUtil -> Load(path={path}) Error loading data: " + ex.Message);
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
                Debug.Log($"DataUtil -> Load(path={path}) Error loading data: " + ex.Message);
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
            Debug.Log($"DataUtil -> Save(path={filePath}) :: Data saving\n" + jsonString);
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

    public static void Delete(string dataPath)
    {
        if (File.Exists(dataPath))
            File.Delete(dataPath);
    }

    private static bool SaveJson<T>(T data, string path) where T : class
    {
        try
        {
            Debug.Log($"DataUtil -> SaveJson(path={path}) :: Data saving, Type: {typeof(T)}\n" + JsonUtility.ToJson(data, true));
            using StreamWriter writer = new StreamWriter(path);
            writer.Write(JsonUtility.ToJson(data, true));
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
            Debug.Log($"DataUtil -> SaveBinary(path={path}) :: Data saving, Type: {typeof(T)}\n" + JsonUtility.ToJson(data));

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
            Debug.Log($"DataUtil -> Load(path={path}) :: Data loaded:\n" + JsonUtility.ToJson(data));
        }
        catch
        {
        }
#endif
    }
}
