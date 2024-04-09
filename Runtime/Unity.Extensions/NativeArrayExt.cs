using Unity.Collections;
using Unity.Mathematics;

public static class NativeArrayExt
{
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