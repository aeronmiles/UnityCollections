using System.Collections.Generic;

public static class LinqExt
{
    public static int IndexOf<T>(this T[] items, T item)
    {
        int retVal = 0;
        int l = items.Length;
        for (int i = 0; i < l; i++)
        {
            if (EqualityComparer<T>.Default.Equals(items[i], item)) return retVal;
            retVal++;
        }
        return -1;
    }
}