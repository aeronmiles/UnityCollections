using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class ClassExt
{
    public static T DeepCopy<T>(this T other)
    {
        // check T is serializable
        if (!typeof(T).IsSerializable)
        {
            throw new ArgumentException("The type must be serializable.", nameof(other));
        }
        using (MemoryStream ms = new MemoryStream())
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, other);
            ms.Position = 0;
            return (T)formatter.Deserialize(ms);
        }
    }
}
