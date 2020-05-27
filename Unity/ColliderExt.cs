using System.Collections.Generic;
using UnityEngine;

public static class ColliderExt
{
    public static Collider[] Enabled(this Collider[] colliders, bool enabled)
    {
        int l = colliders.Length;
        for (int i = 0; i < l; i++)
        {
            colliders[i].enabled = enabled;
        }

        return colliders;
    }

    public static List<Collider> Enabled(this List<Collider> colliders, bool enabled)
    {
        int l = colliders.Count;
        for (int i = 0; i < l; i++)
        {
            colliders[i].enabled = enabled;
        }

        return colliders;
    }
}