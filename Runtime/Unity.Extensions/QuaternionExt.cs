using UnityEngine;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

public static class QuaternionExt
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Quaternion Inverse(this Quaternion q) => Quaternion.Inverse(q);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Quaternion[] Randomize(this Quaternion[] vs, int seed)
  {
    UnityEngine.Random.InitState(seed);
    int l = vs.Length;
    for (int i = 0; i < l; i++)
    {
      vs[i] = UnityEngine.Random.rotation;
    }
    return vs;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Vector3[] Euler(this Quaternion[] qs)
  {
    int l = qs.Length;
    Vector3[] es = new Vector3[l];
    for (int i = 0; i < l; i++)
    {
      es[i] = qs[i].eulerAngles;
    }
    return es;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Quaternion Average(this Quaternion[] qArray)
  {
    if (qArray.Length == 0)
    {
      Debug.LogWarning("Trying to average Quaternion array of Length 0");
      return Quaternion.identity;
    }

    Quaternion qAvg = qArray[0];
    for (int i = 1; i < qArray.Length; i++)
    {
      qAvg = Quaternion.Slerp(qAvg, qArray[i], 1.0f / (float)(i + 1));
    }
    return qAvg;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Quaternion[] MultiplyEuler(this Quaternion[] vs, Vector3 v)
  {
    int l = vs.Length;
    for (int i = 0; i < l; i++)
    {
      var vx = vs[i].eulerAngles;
      vs[i] = Quaternion.Euler(vx.x * v.x, vx.y * v.y, vx.z * v.z);
    }
    return vs;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Quaternion Multiply(this Quaternion vs, Vector3 v)
  {
    var vx = vs.eulerAngles;
    return Quaternion.Euler(vx.x * v.x, vx.y * v.y, vx.z * v.z);
  }

}