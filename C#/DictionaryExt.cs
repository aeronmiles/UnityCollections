using System;
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

            if(notInThis)
            {
                Array.Resize(ref outArray, outArray.Length + 1);
                outArray[outArray.Length - 1] = key;
            }
        }

        return outArray;
    }
}