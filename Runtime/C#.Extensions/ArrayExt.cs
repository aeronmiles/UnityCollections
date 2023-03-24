using System;
using System.Collections.Generic;
using System.Linq;

public static class ArrayExt
{
    // public static T TakeRandom<T>(this T[] array)
    // {
    //     int x = UnityEngine.Random.Range(0, array.Length);
    //     return array[x];
    // }

    public static T TakeRandom<T>(this IEnumerable<T> array)
    {
        int l = array.Count();
        int x = UnityEngine.Random.Range(0, l);
        return array.ElementAt(x);
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