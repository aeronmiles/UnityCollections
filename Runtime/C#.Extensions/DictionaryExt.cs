using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class DictionaryExt
{
  private static readonly StringBuilder _StringBuilder = new StringBuilder(256);

  public static int[] NotIn(this Dictionary<int, GameObject> dict, int[] inThis)
  {
    int[] outArray = new int[] { };
    int inThisLength = inThis.Length;
    bool notInThis;
    foreach (int key in dict.Keys)
    {
      notInThis = true;
      for (int i = 0; i < inThisLength; i++)
      {
        if (key == inThis[i])
        {
          notInThis = false;
          break;
        }
      }

      if (notInThis)
      {
        Array.Resize(ref outArray, outArray.Length + 1);
        outArray[outArray.Length - 1] = key;
      }
    }

    return outArray;
  }

  public static string ToKeyValueString<TKey, TValue>(this Dictionary<TKey, TValue> dict)
  {
    if (dict == null || dict.Count == 0)
      return string.Empty;

    _ = _StringBuilder.Clear();
    foreach (KeyValuePair<TKey, TValue> kvp in dict)
    {
      _ = _StringBuilder.Append(kvp.Key).Append(": ").Append(kvp.Value).Append(", ");
    }

    var str = _StringBuilder.ToString();
    _ = _StringBuilder.Clear();
    return str;
  }
}