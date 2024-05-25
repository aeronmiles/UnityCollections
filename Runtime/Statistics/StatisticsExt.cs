using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;

namespace AM.Unity.Statistics
{
  public static class StatisticsExt
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static MeanMedianVarMinMaxFloat3 ToMeanMedianVarMinMax(this IEnumerable<Color> colors)
    {
      var floats = colors.ToNativeArrayFloat3();
      var mean = floats.Mean();
      MeanMedianVarMinMaxFloat3 mmv = new MeanMedianVarMinMaxFloat3
      {
        mean = mean,
        median = floats.MedianXYZ(),
        variance = floats.Variance(mean),
        min = floats.Min(),
        max = floats.Max()
      };

      floats.Dispose();

      return mmv;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static MeanMedianVarMinMaxFloat3 ToMeanMedianVarMinMax(this NativeArray<Color> colors)
    {
      var floats = colors.ToNativeFloat3();
      var mean = floats.Mean();
      MeanMedianVarMinMaxFloat3 mmv = new MeanMedianVarMinMaxFloat3
      {
        mean = mean,
        median = floats.MedianXYZ(),
        variance = floats.Variance(mean),
        min = floats.Min(),
        max = floats.Max()
      };

      floats.Dispose();
      return mmv;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static List<float> Errors(this IEnumerable<float> values, float referenceValue)
    {
      List<float> errors = values.ToList();
      foreach (var v in values)
      {
        errors.Add(math.distance(v, referenceValue));
      }

      return errors;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static float3 Mean(this IEnumerable<float3> float3s)
    {
      float3 m = new float3();
      foreach (var v in float3s)
      {
        m += v;
      }

      return m / float3s.Count();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static float Mean(this IEnumerable<float> floats)
    {
      double m = 0f;
      foreach (var v in floats)
      {
        m += v;
      }

      return (float)(m / floats.Count());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static float3 Min(this IEnumerable<float3> float3s)
    {
      float3 m = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
      foreach (var v in float3s)
      {
        m[0] = math.min(v.x, m[0]);
        m[1] = math.min(v.y, m[1]);
        m[2] = math.min(v.z, m[2]);
      }

      return m;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static float Min(this IEnumerable<float> floats)
    {
      float m = float.MaxValue;
      foreach (var v in floats)
      {
        m = math.min(v, m);
      }

      return m;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static float3 Max(this IEnumerable<float3> float3s)
    {
      float3 m = new float3(float.MinValue, float.MinValue, float.MinValue);
      foreach (var v in float3s)
      {
        m[0] = math.max(v.x, m[0]);
        m[1] = math.max(v.y, m[1]);
        m[2] = math.max(v.z, m[2]);
      }

      return m;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static float Max(this IEnumerable<float> floats)
    {
      float m = float.MinValue;
      foreach (var v in floats)
      {
        m = math.max(v, m);
      }

      return m;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static float Median(this NativeArray<float> floats, bool sortInPlace)
    {
      NativeArray<float> sortedValues;
      int l = floats.Length;
      if (sortInPlace)
      {
        sortedValues = floats;
        sortedValues.SortInPlace();
      }
      else
      {
        using (sortedValues = new NativeArray<float>(floats.Length, Allocator.Temp))
        {
          for (int i = 0; i < l; i++)
          {
            sortedValues[i] = floats[i];
          }
          sortedValues.SortInPlace();
        }
      }
      float median;
      int mid = l / 2;
      median = (l % 2 != 0) ? sortedValues[mid] : (sortedValues[mid] + sortedValues[mid - 1]) / 2;

      return median;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static float3 MedianXYZ(this NativeArray<float3> float3s)
    {
      int l = float3s.Count();
      float3s.XValues(out var xValues);
      float3s.XValues(out var yValues);
      float3s.XValues(out var zValues);
      xValues.SortInPlace();
      yValues.SortInPlace();
      zValues.SortInPlace();

      int mid = l / 2;
      float3 median = new float3
      {
        x = (l % 2 != 0) ? xValues[mid] : (xValues[mid] + xValues[mid - 1]) / 2,
        y = (l % 2 != 0) ? yValues[mid] : (yValues[mid] + yValues[mid - 1]) / 2,
        z = (l % 2 != 0) ? zValues[mid] : (zValues[mid] + zValues[mid - 1]) / 2
      };

      xValues.Dispose();
      yValues.Dispose();
      zValues.Dispose();

      return median;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static void XValues(this NativeArray<float3> float3s, out NativeArray<float> xValues)
    {
      int l = float3s.Length;
      xValues = new NativeArray<float>(l, Allocator.Temp);
      for (int i = 0; i < l; i++)
      {
        xValues[i] = float3s[i].x;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static void YValues(this NativeArray<float3> float3s, out NativeArray<float> yValues)
    {
      int l = float3s.Length;
      yValues = new NativeArray<float>(l, Allocator.Temp);
      for (int i = 0; i < l; i++)
      {
        yValues[i] = float3s[i].y;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static void ZValues(this NativeArray<float3> float3s, out NativeArray<float> zValues)
    {
      int l = float3s.Length;
      zValues = new NativeArray<float>(l, Allocator.Temp);
      for (int i = 0; i < l; i++)
      {
        zValues[i] = float3s[i].z;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static float Median(this IEnumerable<float> floats)
    {
      int l = floats.Count();
      if (floats == null || l == 0)
      {
        throw new Exception("Median of empty array not defined.");
      }
      float median;
      if (floats.AsArray(out var array))
      {
        int mid = l / 2;
        Array.Sort(array);
        median = (l % 2 != 0) ? array[mid] : (array[mid] + array[mid - 1]) / 2;
      }
      else if (floats.AsList(out var list))
      {
        int mid = l / 2;
        list.Sort();
        median = (l % 2 != 0) ? list[mid] : (list[mid] + list[mid - 1]) / 2;
      }
      else
      {
        throw new NotImplementedException("For type: " + Type.GetTypeCode(floats.GetType()) + " this method is not implemented.");
      }

      return median;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static float Squared(this float f) => math.sqrt(f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Variance(this IEnumerable<float3> float3s, float3 mean)
    {
      int l = 0;
      float3 sumOfSquares = new float3();
      foreach (var c in float3s)
      {
        l++;
        sumOfSquares += math.pow(c - mean, 2.0f);
      }

      if (l > 0)
      {
        return sumOfSquares / l;
      }
      else
      {
        return new float3();
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static float Variance(this IEnumerable<float> floats, float mean)
    {
      int l = 0;
      float sumOfSquares = 0.0f;
      foreach (var f in floats)
      {
        l++;
        sumOfSquares += math.pow(f - mean, 2.0f);
      }

      if (l > 0)
      {
        return sumOfSquares / l;
      }
      else
      {
        return 0f;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    private static float2 Quartiles(this IEnumerable<float> data)
    {
      float2 quartiles = new float2();
      using (var pooledObj = ListPool<float>.Get(out List<float> list))
      {
        list.AddRange(data);
        list.Sort();
        int n = data.Count();
        int q1 = Mathf.FloorToInt(0.25f * n);
        int q3 = Mathf.FloorToInt(0.75f * n);
        quartiles = new float2(list[q1], list[q3]);
      }
      return quartiles;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static MeanMedianVarMinMax ToMeanMedianVarMinMax(this IEnumerable<float> floats)
    {
      float mean = floats.Mean();
      MeanMedianVarMinMax mmv = new MeanMedianVarMinMax
      {
        mean = mean,
        median = floats.Median(),
        variance = floats.Variance(mean),
        min = floats.Min(),
        max = floats.Max()
      };

      return mmv;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static float2 Mean(this float2[] vs)
    {
      float2 average = new float2();
      int l = vs.Length;
      for (int i = 0; i < l; i++)
      {
        average += vs[i];
      }

      return average / l;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static Vector2 Mean(this Vector2[] vs)
    {
      Vector2 average = new Vector2();
      int l = vs.Length;
      for (int i = 0; i < l; i++)
      {
        average += vs[i];
      }

      return average / l;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static long Mean(this long[] vals)
    {
      long m = 0;
      int l = 0;
      for (int i = 0; i < l; i++)
      {
        m += vals[i];
      }

      return m / l;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static float MmToMeters(this float f) => f / 1000f;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BurstCompile]
    public static float MetersToMMs(this float f) => f * 1000f;
  }
}
