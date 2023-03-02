using System;
using System.Collections.Generic;

public static class EnumExt
{
    public static T Next<T>(this T src) where T : struct
    {
        if (!typeof(T).IsEnum)
            throw new ArgumentException(String.Format("Argument {0} is not an Enum", typeof(T).FullName));

        T[] Arr = (T[])Enum.GetValues(src.GetType());
        int j = Array.IndexOf<T>(Arr, src) + 1;
        return (Arr.Length == j) ? Arr[0] : Arr[j];
    }

    public static List<string> GetNamesFromIndexes(this Type type, IEnumerable<int> indexes)
    {
        if (!type.IsEnum)
            throw new ArgumentException(String.Format("Argument {0} is not an Enum", type.FullName));

        var names = new List<string>();
        foreach (var index in indexes)
            names.Add(Enum.GetNames(type)[index]);

        return names;
    }

    public static List<int> GetIndexesFromNames(this Type type, IEnumerable<string> names)
    {
        if (!type.IsEnum)
            throw new ArgumentException(String.Format("Argument {0} is not an Enum", type.FullName));
        var indexes = new List<int>();
        foreach (var name in names)
        {
            var n = name.Replace(" ", "_");
            var index = Array.IndexOf(Enum.GetNames(type), n);
            indexes.Add(index);
        }
        return indexes;
    }
}