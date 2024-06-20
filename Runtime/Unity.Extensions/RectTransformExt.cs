
using Unity.Mathematics;
using UnityEngine;

public static class RectTransformExt
{
  public static Rect InvertY(this Rect rect, int screenHeight = -1)
  {
    if (screenHeight == -1)
    {
      screenHeight = Screen.height;
    }
    return new Rect(rect.x, screenHeight - rect.y - rect.height, rect.width, rect.height);
  }

  public static RectTransform Closest(this RectTransform[] ts, Vector2 pos)
  {
    int l = ts.Length;

    RectTransform closest = null;
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

  public static RectTransform ClosestLocal(this RectTransform[] ts, Vector2 pos)
  {
    int l = ts.Length;

    RectTransform closest = null;
    float d = float.MaxValue;
    for (int i = 0; i < l; i++)
    {
      float di = Vector3.Distance(pos, ts[i].localPosition);
      if (di < d)
      {
        closest = ts[i];
        d = di;
      }
    }

    return closest;
  }

  public static Vector3 PositionDelta(this RectTransform rect, float2 pos)
  {
    pos = pos - new float2(0.5f, 0.5f);
    return new Vector3(rect.sizeDelta.x * pos.x, rect.sizeDelta.y * pos.y);
  }
}
