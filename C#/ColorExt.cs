using UnityEngine;

public static class ColorExt
{
    public static Vector3 rgb(this Color c)
    {
        return new Vector3(c.r, c.g, c.b);
    }

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

    public static float[] ToGrayscale(this Color[] cs)
    {
        int l = cs.Length;
        float[] gs = new float[l];
        for (int i = 0; i < l; i++) gs[i] = cs[i].grayscale;
        return gs;
    }
}