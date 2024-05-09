using System.Runtime.CompilerServices;
using UnityEngine;

public static class FloatExt
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float Round(this float val, int decimals = 0)
  {
    if (decimals > 0)
    {
      int multiplier = (decimals * 10);
      return Mathf.Round(val * multiplier) / multiplier;
    }
    else
    {
      return Mathf.Round(val);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float ToRadians(this float val) => val * Mathf.Deg2Rad;
}