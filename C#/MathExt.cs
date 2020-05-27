using System.Collections.Generic;
using UnityEngine;

public static class MathExt
{
    public static float Average(this float[] floats)
    {
        float amount = 0f;
        int l = floats.Length;
        for (int i = 0; i < l; i++)
        {
            amount += floats[i];
        }

        return amount / floats.Length;
    }

    public static float WeightedAverage(this float[] floats)
    {
        float amount = 0f;
        int count = floats.Length;
        float last = floats[count-1];
        int x = 0;
        int l = floats.Length;
        for (int i = 0; i < l; i++)
        {
            amount += Mathf.Lerp(last, floats[i], (x++ / count));
        }

        return amount / count;
    }

    public static float Average(this List<float> floats)
    {
        float amount = 0f;
        int l = floats.Count;
        for (int i = 0; i < l; i++)
        {
            amount += floats[i];
        }

        return amount / floats.Count;
    }

    public static float WeightedAverage(this List<float> floats)
    {
        float amount = 0f;
        int count = floats.Count;
        float last = floats[count-1];
        int x = 0;
        int l = floats.Count;
        for (int i = 0; i < l; i++)
        {
            amount += Mathf.Lerp(last, floats[i], (x++ / count));
        }

        return amount / count;
    }
}