using Unity.Mathematics;

public static class float2Ext
{
    public static float ClosestDistance(this float2[] ps, float2 p)
    {
        int l = ps.Length;
        float closest = float.MaxValue;
        for (int i = 0; i < l; i++)
        {
            float d = math.distance(p, ps[i]);
            if (d < closest)
            {
                closest = d;
            }
        }

        return closest;
    }

    public static float2 ClosestValue(this float2[] ps, float2 p)
    {
        int l = ps.Length;
        float closest = float.MaxValue;
        int ind = 0;
        for (int i = 0; i < l; i++)
        {
            float d = math.distance(p, ps[i]);
            if (d < closest)
            {
                closest = d;
                ind = i;
            }
        }

        return ps[ind];
    }
}
