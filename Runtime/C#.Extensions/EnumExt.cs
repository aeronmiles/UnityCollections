using System;
using System.Collections.Generic;

public static class EnumExt
{
  public static T Next<T>(this T src) where T : struct
  {
    if (!typeof(T).IsEnum)
    {
      throw new ArgumentException($"Argument {typeof(T).FullName} is not an Enum");
    }

    T[] Arr = (T[])Enum.GetValues(src.GetType());
    int j = Array.IndexOf(Arr, src) + 1;
    return (Arr.Length == j) ? Arr[0] : Arr[j];
  }

  public static string GetNameFromIndex(this Type type, int index)
  {
    if (!type.IsEnum)
    {
      throw new ArgumentException($"Argument {0} is not an Enum", type.FullName);
    }

    return Enum.GetNames(type)[index];
  }

  public static List<string> GetNamesFromIndexes(this Type type, IEnumerable<int> indexes)
  {
    if (!type.IsEnum)
    {
      throw new ArgumentException($"Argument {type.FullName} is not an Enum");
    }

    var names = new List<string>();
    foreach (var index in indexes)
    {
      names.Add(Enum.GetNames(type)[index]);
    }

    return names;
  }

  public static int GetIndexFromName(this Type type, string name)
  {
    if (!type.IsEnum)
    {
      throw new ArgumentException($"Argument {type.FullName} is not an Enum");
    }

    return Array.IndexOf(Enum.GetNames(type), name);
  }

  public static List<int> GetIndexesFromNames(this Type type, IEnumerable<string> names)
  {
    if (!type.IsEnum)
    {
      throw new ArgumentException($"Argument {type.FullName} is not an Enum");
    }

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