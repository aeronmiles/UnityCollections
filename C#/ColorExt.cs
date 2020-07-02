using UnityEngine;

public static class ColorExt
{
    public static Vector3 rgb(this Color c)
    {
        return new Vector3(c.r, c.g, c.b);
    }
}