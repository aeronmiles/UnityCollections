using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

public static class Vector3Ext
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3[] RandomRange(this Vector3[] vs, int seed, float min, float max)
    {
        var r = new Unity.Mathematics.Random((uint)seed);
        int l = vs.Length;
        for (int i = 0; i < l; i++)
        {
            vs[i] = new Vector3(
                math.lerp(min, max, r.NextFloat()),
                math.lerp(min, max, r.NextFloat()),
                math.lerp(min, max, r.NextFloat())
            );
        }
        return vs;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3[] RandomRange(this Vector3[] vs, int seed, Vector3 min, Vector3 max)
    {
        var r = new Unity.Mathematics.Random((uint)seed);
        int l = vs.Length;
        for (int i = 0; i < l; i++)
        {
            vs[i] = new Vector3(
                math.lerp(min.x, max.x, r.NextFloat()),
                math.lerp(min.y, max.y, r.NextFloat()),
                math.lerp(min.z, max.z, r.NextFloat())
            );
        }
        return vs;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 XYZ(this Vector3 v, float value)
    {
        v.x = v.y = v.z = value;
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Random(this Vector3 v, float min = -1f, float max = 1f)
    {
        return new Vector3(UnityEngine.Random.Range(min, max), UnityEngine.Random.Range(min, max), UnityEngine.Random.Range(min, max));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ToVector3(this float value)
    {
        return new Vector3(value, value, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public static Vector3 Average(this IEnumerable<Vector3> vArray)
    {
        int l = vArray.Count();
        if (l == 0)
        {
            Debug.LogWarning("Trying to average Vector3 array of Length 0");
            return Vector3.zero;
        }

        Vector3 averagePos = Vector3.zero;

        foreach (Vector3 p in vArray)
        {
            averagePos += p;
        }

        return averagePos / l;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Multiply(this Vector3 v, Vector3 vx)
    {
        return new Vector3(v.x * vx.x, v.y * vx.y, v.z * vx.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Round(this Vector3 v, int decimals = 0)
    {
        return new Vector3(v.x.Round(decimals), v.y.Round(decimals), v.z.Round(decimals));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3[] ToNearest(this Vector3[] vs, Vector3 step)
    {
        int l = vs.Length;
        for (int i = 0; i < l; i++)
        {
            Vector3 v = vs[i];
            vs[i] = new Vector3(
                math.round(v.x / step.x) * step.x,
                math.round(v.y / step.y) * step.y,
                math.round(v.z / step.z) * step.z
                );
        }
        return vs;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion[] ToQuaternion(this Vector3[] vs)
    {
        int l = vs.Length;
        Quaternion[] qs = new Quaternion[l];
        for (int i = 0; i < l; i++)
        {
            qs[i] = Quaternion.Euler(vs[i]);
        }
        return qs;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion ToQuaternion(this Vector3 v)
    {
        return Quaternion.Euler(v.x, v.y, v.z);
    } 
}