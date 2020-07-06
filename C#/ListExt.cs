using System;
using System.Collections.Generic;

public static class ListExt
{
    public static List<T> ForEach<T>(this List<T> items, ref Action<T> action)
    {
        int l = items.Count;
        for (int i = 0; i < l; i++)
        {
            action(items[i]);
        }
        return items;
    }
}