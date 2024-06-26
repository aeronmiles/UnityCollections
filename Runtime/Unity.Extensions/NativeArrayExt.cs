using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;

public static class NativeArrayExt
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  [BurstCompile]
  public static float CalculateMedian(this NativeArray<float> values, int count, bool sortInPlace = false)
  {
    if (count == 0)
    {
      return 0f;
    }

    NativeArray<float> sortedValues = default;
    bool isNewArrayAllocated = false;

    try
    {
      if (sortInPlace)
      {
        sortedValues = values;
      }
      else
      {
        // Sort the values using a Burst-compatible sorting algorithm
        sortedValues = new NativeArray<float>(count, Allocator.Temp);
        isNewArrayAllocated = true;
        for (int i = 0; i < count; i++)
        {
          sortedValues[i] = values[i];
        }
      }
      sortedValues.Quicksort(count);

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

      return median;
    }
    finally
    {
      if (isNewArrayAllocated && sortedValues.IsCreated)
      {
        sortedValues.Dispose();
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  [BurstCompile]
  public static void Quicksort(this NativeArray<float> arr, int n) => arr.Quicksort(0, n - 1);

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

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  [BurstCompile]
  public static void Swap(this NativeArray<float> arr, int i, int j)
  {
    float temp = arr[i];
    arr[i] = arr[j];
    arr[j] = temp;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  [BurstCompile]
  public static void SortInPlace(this NativeArray<float> array)
  {
    for (int i = 0; i < array.Length; i++)
    {
      for (int j = i + 1; j < array.Length; j++)
      {
        if (array[i] > array[j])
        {
          float temp = array[i];
          array[i] = array[j];
          array[j] = temp;
        }
      }
    }
  }
}
