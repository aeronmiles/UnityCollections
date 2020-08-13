
using UnityEngine;

public static class RectTransformExt
{
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
}
