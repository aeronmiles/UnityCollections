using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class SerializationUtil
{

    public static void SaveTo_ApplicationDataPath<T>(T obj, string fileName) where T : class
    {
        if (!IsSerializable(obj)) return;

        BinaryFormatter bf = new();
        FileStream fs = new(Application.dataPath + fileName, FileMode.Create);
        bf.Serialize(fs, obj);
        fs.Close();
    }

    public static T LoadFrom_ApplicationDataPath<T>(string fileName) where T : class
    {
        if (!File.Exists(Application.dataPath + fileName))
        {
            Debug.LogError(fileName + " file does not exist.");
            return null;
        }

        BinaryFormatter bf = new();
        FileStream fs = new(Application.dataPath + fileName, FileMode.Open);
        T obj = (T)bf.Deserialize(fs);
        fs.Close();

        return obj;
    }

    public static string Serialize_AsString(this object o)
    {
        using MemoryStream stream = new();
        new BinaryFormatter().Serialize(stream, o);
        return Convert.ToBase64String(stream.ToArray());
    }

    public static bool Deserialize_As<T>(this string str, out T obj) where T : class
    {
        byte[] bytes = Convert.FromBase64String(str);

        using MemoryStream stream = new(bytes);
        obj = new BinaryFormatter().Deserialize(stream) as T;

        return obj != null;
    }

    public static bool IsSerializable(object o)
    {
        if (!o.GetType().IsSerializable)
        {
            Debug.Log($"SerializationUtil -> IsSerializable() :: Object of type: {o.GetType()} is not serializable");
            return false;
        }
        Debug.Log($"SerializationUtil -> IsSerializable() :: Object of type: {o.GetType()} is serializable");

        return true;
    }
}
