using System;
using System.Text;

public static class ArrayExt
{
  private static readonly StringBuilder _StringBuilder = new StringBuilder(256);
  public static string ToValueString<TValue>(this TValue[] array)
  {
    if (array == null || array.Length == 0)
      return string.Empty;

    _ = _StringBuilder.Clear();
    for (int i = 0; i < array.Length; i++)
    {
      _ = _StringBuilder.Append(array[i]).Append(", ");
    }

    var str = _StringBuilder.ToString();
    _ = _StringBuilder.Clear();
    return str;
  }

  public static int[] NotIn(this int[] values, int[] notIn)
  {
    int[] valuesNotIn = new int[] { };
    bool isIn;
    int l = values.Length;
    int m = notIn.Length;
    for (int i = 0; i < l; i++)
    {
      isIn = false;
      for (int j = 0; j < m; j++)
      {
        if (notIn[j] == values[i])
        {
          isIn = true;
          break;
        }
      }

      if (!isIn)
      {
        Array.Resize(ref valuesNotIn, valuesNotIn.Length + 1);
        valuesNotIn[valuesNotIn.Length - 1] = values[i];
      }
    }

    return valuesNotIn;
  }

  public static T[] SubArray<T>(this T[] data, int index, int length)
  {
    T[] result = new T[length];
    Array.Copy(data, index, result, 0, length);
    return result;
  }

  public static T[] ForEach<T>(this T[] items, ref Action<T> action)
  {
    int l = items.Length;
    for (int i = 0; i < l; i++)
    {
      action(items[i]);
    }
    return items;
  }
}