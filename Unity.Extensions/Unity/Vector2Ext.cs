using UnityEngine;

public static class Vector2Ext
{
    public static Vector2 ToVector2(this float x) => new Vector2(x, x);
}
