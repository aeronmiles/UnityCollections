using System;
using System.CodeDom;
using System.Collections.Generic;
using UnityEngine;

public static class DictionaryExt
{
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
    string str = "";
    foreach (KeyValuePair<TKey, TValue> kvp in dict)
    {
      str += kvp.Key + ": " + kvp.Value + "\n";
    }

    return str;
  }
}