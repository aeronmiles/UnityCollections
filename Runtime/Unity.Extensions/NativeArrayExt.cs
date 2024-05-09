using System;
using Unity.Collections;
using Unity.Mathematics;

public static class NativeArrayExt
{
  [BurstCompile]
  public static float CalculateMedian(this NativeArray<float> values, int count)
  {
    if (count == 0)
    {
      return 0f;
    }

    // Sort the values using a Burst-compatible sorting algorithm
    NativeArray<float> sortedValues = new NativeArray<float>(count, Allocator.Temp);
    for (int i = 0; i < count; i++)
    {
      sortedValues[i] = values[i];
    }
    sortedValues.Quicksort();

    // Calculate the median
    float median;
    if (count % 2 == 0)
    {
      int middleIndex = count / 2;
      median = (sortedValues[middleIndex - 1] + sortedValues[middleIndex]) * 0.5f;
    }
    else
    {
      int middleIndex = count / 2;
      median = sortedValues[middleIndex];
    }

    sortedValues.Dispose();

    return median;
  }

  [BurstCompile]
  public static void Quicksort(this NativeArray<float> arr)
  {
    int left = 0;
    int right = arr.Length - 1;
    if (left < right)
    {
      int pivotIndex = arr.Partition(left, right);
      arr.Quicksort(left, pivotIndex - 1);
      arr.Quicksort(pivotIndex + 1, right);
    }
  }
  [BurstCompile]
  public static void Quicksort(this NativeArray<float> arr, int left, int right)
  {
    if (left < right)
    {
      int pivotIndex = arr.Partition(left, right);
      arr.Quicksort(left, pivotIndex - 1);
      arr.Quicksort(pivotIndex + 1, right);
    }
  }

  [BurstCompile]
  public static int Partition(this NativeArray<float> arr, int left, int right)
  {
    float pivot = arr[right];
    int i = left - 1;

    for (int j = left; j < right; j++)
    {
      if (arr[j] < pivot)
      {
        i++;
        arr.Swap(i, j);
      }
    }

    arr.Swap(i + 1, right);
    return i + 1;
  }

  [BurstCompile]
  public static void Swap(this NativeArray<float> arr, int i, int j)
  {
    float temp = arr[i];
    arr[i] = arr[j];
    arr[j] = temp;
  }

  public static NativeArray<float> Sort(this NativeArray<float> array)
  {
    NativeArray<float> sorted = new NativeArray<float>(array.Length, Allocator.Temp);
    for (int i = 0; i < array.Length; i++)
    {
      sorted[i] = array[i];
    }
    for (int i = 0; i < sorted.Length; i++)
    {
      for (int j = i + 1; j < sorted.Length; j++)
      {
        if (sorted[i] > sorted[j])
        {
          float temp = sorted[i];
          sorted[i] = sorted[j];
          sorted[j] = temp;
        }
      }
    }
    return sorted;
  }

  public static NativeArray<float> XValues(this NativeArray<float3> array)
  {
    int l = array.Length;
    NativeArray<float> xValues = new NativeArray<float>(l, Allocator.Temp);
    for (int i = 0; i < l; i++)
    {
      xValues[i] = array[i].x;
    }
    return xValues;
  }

  public static NativeArray<float> YValues(this NativeArray<float3> array)
  {
    int l = array.Length;
    NativeArray<float> yValues = new NativeArray<float>(l, Allocator.Temp);
    for (int i = 0; i < l; i++)
    {
      yValues[i] = array[i].y;
    }
    return yValues;
  }

  public static NativeArray<float> ZValues(this NativeArray<float3> array)
  {
    int l = array.Length;
    NativeArray<float> zValues = new NativeArray<float>(l, Allocator.Temp);
    for (int i = 0; i < l; i++)
    {
      zValues[i] = array[i].z;
    }
    return zValues;
  }
}

internal class BurstCompileAttribute : Attribute
{
}