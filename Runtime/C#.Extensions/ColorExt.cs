using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class ColorExt
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static IEnumerable<float3> ToFloat3(this IEnumerable<Color> colors)
  {
    foreach (var c in colors)
    {
      yield return new float3(c.r, c.g, c.b);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static NativeArray<float3> ToFloat3(this NativeArray<Color> colors)
  {
    var floats = new NativeArray<float3>(colors.Length, Allocator.Temp);
    for (int i = 0; i < colors.Length; i++)
    {
      floats[i] = new float3(colors[i].r, colors[i].g, colors[i].b);
    }
    return floats;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Vector3 rgb(this Color c)
  {
    return new Vector3(c.r, c.g, c.b);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Vector3[] ToRGB(this Color[] cs)
  {
    int l = cs.Length;
    Vector3[] rgbs = new Vector3[l];
    for (int i = 0; i < l; i++)
    {
      rgbs[i] = cs[i].rgb();
    }
    return rgbs;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Color32 Divide(this Color32 v, Color32 vx)
  {
    return new Color32((byte)(v.r / vx.r), (byte)(v.g / vx.g), (byte)(v.b / vx.b), (byte)(v.a / vx.a));
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Color Divide(this Color v, Color vx)
  {
    return new Color(v.r / vx.r, v.g / vx.g, v.b / vx.b, v.a / vx.a);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float[] ToGrayscale(this Color[] cs)
  {
    int l = cs.Length;
    float[] gs = new float[l];
    for (int i = 0; i < l; i++) gs[i] = cs[i].grayscale;
    return gs;
  }
}