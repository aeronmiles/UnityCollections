using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public static class Vector3Ext
{
    public static Vector3[] Randomize(this Vector3[] vs, int seed)
    {
        var r = new Unity.Mathematics.Random((uint)seed);
        int l = vs.Length;
        for (int i = 0; i < l; i++)
        {
            vs[i] = new Vector3(r.NextFloat() * 2f - 1f, r.NextFloat() * 2f - 1f, r.NextFloat() * 2f - 1f);
        }
        return vs;
    }

    public static Vector3 XYZ(this Vector3 v, float value)
    {
        v.x = v.y = v.z = value;
        return v;
    }

    public static Vector3 Random(this Vector3 v, float min = -1f, float max = 1f)
    {
        return new Vector3(UnityEngine.Random.Range(min, max), UnityEngine.Random.Range(min, max), UnityEngine.Random.Range(min, max));
    }

    public static Vector3 ToVector3(this float value)
    {
        return new Vector3(value, value, value);
    }

    public static int ClosestIndex(this Vector3 v, Vector3[] vs)
    {
        float d = float.MaxValue;
        int closestIndex = -1;
        int length = vs.Length;
        for (int i = 0; i < length; i++)
        {
            float di = (vs[i] - v).magnitude;
            if (di < d)
            {
                closestIndex = i;
                d = di;
            }
        }

        return closestIndex;
    }

    public static int ClosestIndex(this Vector3 v, Transform[] vs)
    {
        float d = float.MaxValue;
        int closestIndex = -1;
        for (int i = 0; i < vs.Length; i++)
        {
            float di = (vs[i].position - v).magnitude;
            if (di < d)
            {
                closestIndex = i;
                d = di;
            }
        }

        return closestIndex;
    }

    public static void ClosestIndex(this Vector3 v, ref List<Vector3> vs, ref int vsLength, ref float maxVal, ref int indexOut)
    {
        for (int i = 0; i < vsLength; i++)
        {
            float di = (vs[i] - v).magnitude;
            if (di < maxVal)
            {
                indexOut = i;
                maxVal = di;
            }
        }
    }

    public static Vector3 ClosestPosition(this Vector3 v, Vector3[] vs)
    {
        float d = float.MaxValue;
        Vector3 closestPos = Vector3.zero;
        int length = vs.Length;
        Vector3 posI;
        for (int i = 0; i < length; i++)
        {
            posI = vs[i];
            float di = (v - posI).magnitude;
            if (di < d)
            {
                closestPos = posI;
                d = di;
            }
        }

        return closestPos;
    }

    public static void ClosestPosition(this Vector3 v, ref List<Vector3> vs, ref int vsLength, ref float maxVal, ref Vector3 vOut)
    {
        for (int i = 0; i < vsLength; i++)
        {
            float di = (vs[i] - v).magnitude;
            if (di < maxVal)
            {
                vOut = vs[i];
                maxVal = di;
            }
        }
    }

    public static void FurthestPosition(this Vector3 v, ref Vector3[] vs, ref int vsLength, ref float minVal, ref Vector3 vOut)
    {
        for (int i = 0; i < vsLength; i++)
        {
            float di = (vs[i] - v).magnitude;
            if (di > minVal)
            {
                vOut = vs[i];
                minVal = di;
            }
        }
    }

    public static float MinDistance(this Vector3 v, Vector3[] vs)
    {
        float minDistance = float.MaxValue;
        int l = vs.Length;
        for (int i = 0; i < l; i++)
        {
            if ((v - vs[i]).magnitude < minDistance) minDistance = (v - vs[i]).magnitude;
        }

        return minDistance;
    }

    public static Vector3 Average(this Vector3[] vArray)
    {
        Vector3 averagePos = Vector3.zero;
        int l = vArray.Length;
        for (int i = 0; i < l; i++)
        {
            averagePos += vArray[i];
        }

        return averagePos / vArray.Length;
    }

    public static Vector3 Multiply(this Vector3 v, Vector3 vx)
    {
        return new Vector3(v.x * vx.x, v.y * vx.y, v.z * vx.z);
    }

    public static Vector3[] Multiply(this Vector3[] vs, Vector3 v)
    {
        int l = vs.Length;
        for (int i = 0; i < l; i++)
        {
            var vx = vs[i];
            vs[i] = new Vector3(vx.x * v.x, vx.y * v.y, vx.z * v.z);
        }
        return vs;
    }

    public static Vector3 Round(this Vector3 v, int decimals = 0)
    {
        return new Vector3(v.x.Round(decimals), v.y.Round(decimals), v.z.Round(decimals));
    }
}