using UnityEngine;

public static class FloatExt
{
    public static float Round(this float val, int decimals=0)
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
}