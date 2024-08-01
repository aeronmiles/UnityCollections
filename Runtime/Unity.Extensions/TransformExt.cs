using System;
using System.Collections.Generic;
using UnityEngine;

public static class TransformExt
{
  public static Transform[] SetPositionAndRotation(this Transform[] ts, Vector3[] ps, Quaternion[] rs)
  {
    int l = ts.Length;
    for (int i = 0; i < l; i++)
    {
      ts[i].position = ps[i];
      ts[i].rotation = rs[i];
    }
    return ts;
  }

  public static List<Transform> Children(this Transform transform)
  {
    var childs = transform.GetComponentsInChildren<Transform>(true);
    List<Transform> objs = new();
    foreach (var child in childs)
    {
      if (child.parent == transform)
      {
        objs.Add(child);
      }
    }
    return objs;
  }

  public static Transform Closest(this Transform[] ts, Vector3 pos)
  {
    int l = ts.Length;

    Transform closest = null;
    float d = float.MaxValue;
    for (int i = 0; i < l; i++)
    {
      float di = Vector3.Distance(pos, ts[i].position);
      if (di < d)
      {
        closest = ts[i];
        d = di;
      }
    }

    return closest;
  }

  public static Transform Closest(this List<Transform> ts, Vector3 pos)
  {
    int l = ts.Count;

    Transform closest = null;
    float d = float.MaxValue;
    for (int i = 0; i < l; i++)
    {
      float di = Vector3.Distance(pos, ts[i].position);
      if (di < d)
      {
        closest = ts[i];
        d = di;
      }
    }

    return closest;
  }


  public static Vector3[] Positions(this Transform[] ts)
  {
    int l = ts.Length;
    Vector3[] positions = new Vector3[l];
    for (int i = 0; i < l; i++)
    {
      positions[i] = ts[i].position;
    }

    return positions;
  }

  public static Vector3[] EulerAngles(this Transform[] ts)
  {
    int l = ts.Length;
    Vector3[] angles = new Vector3[l];
    for (int i = 0; i < l; i++)
    {
      angles[i] = ts[i].eulerAngles;
    }

    return angles;
  }
}