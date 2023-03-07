using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class DataUtil
{
    public static bool Load<T>(out T dataOut, string path) where T : class
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
            LogAsJson(path, data);
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
            LogAsJson(path, data);
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

    private static bool SaveJson<T>(T data, string path) where T : class
    {
        try
        {
            Debug.Log($"DataUtil -> Save(path={path}) :: Data saving:\n" + JsonUtility.ToJson(data, true));
            using StreamWriter writer = new(path);
            writer.Write(JsonUtility.ToJson(data, true));
            writer.Close();
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log($"DataUtil -> Save(path={path}) :: Error saving data: " + ex.Message);
            return false;
        }
    }
    
    private static bool SaveBinary<T>(T data, string path) where T : class
    {
        try
        {
            Debug.Log($"DataUtil -> Save(path={path}) :: Data saving:\n" + JsonUtility.ToJson(data));

            BinaryFormatter b = new BinaryFormatter();
            FileStream dataFile = File.Create(path);
            b.Serialize(dataFile, data);
            dataFile.Close();
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log($"DataUtil -> Save(path={path}) :: Error saving data: " + ex.Message);
            return false;
        }
    }

    private static void LogAsJson(string path, object data)
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
