using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CachedTransform
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 localScale;

    public Vector3 localPosition;
    public Quaternion localRotation;

    public Transform Transform;

    public CachedTransform(Transform xform)
    {
        position = xform.position;
        rotation = xform.rotation;
        localScale = xform.localScale;
        localPosition = xform.localPosition;
        localRotation = xform.localRotation;
        Transform = xform;
    }

    public static explicit operator CachedTransform(Transform x)
    {
        return new CachedTransform(x);
    }

    public static bool operator ==(CachedTransform x, CachedTransform y)
    {
        if (x.position != y.position) return false;
        if (x.rotation != y.rotation) return false;
        if (x.localScale != y.localScale) return false;
        return true;
    }

    public static bool operator !=(CachedTransform x, CachedTransform y)
    {
        if (x.position != y.position) return true;
        if (x.rotation != y.rotation) return true;
        if (x.localScale != y.localScale) return true;
        return false;
    }

    public static bool operator ==(CachedTransform x, Transform y)
    {
        if (x.position != y.position) return false;
        if (x.rotation != y.rotation) return false;
        if (x.localScale != y.localScale) return false;
        return true;
    }

    public static bool operator !=(CachedTransform x, Transform y)
    {
        if (x.position != y.position) return true;
        if (x.rotation != y.rotation) return true;
        if (x.localScale != y.localScale) return true;
        return false;
    }
    public static bool operator ==(Transform x, CachedTransform y)
    {
        if (x.position != y.position) return false;
        if (x.rotation != y.rotation) return false;
        if (x.localScale != y.localScale) return false;
        return true;
    }

    public static bool operator !=(Transform x, CachedTransform y)
    {
        if (x.position != y.position) return true;
        if (x.rotation != y.rotation) return true;
        if (x.localScale != y.localScale) return true;
        return false;
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

public static class CachedTransformExtensions
{
    public static Transform SetFromCached(this Transform t, CachedTransform c)
    {
        t.position = c.position;
        t.rotation = c.rotation;
        t.localScale = c.localScale;
        return t;
    }

    public static List<CachedTransform> ToCachedTransforms(this IEnumerable<Transform> transforms)
    {
        List<CachedTransform> cachedTransforms = new List<CachedTransform>();
        foreach (Transform t in transforms)
        {
            cachedTransforms.Add(new CachedTransform(t));
        }
        return cachedTransforms;
    }

    public static void ResetTransforms(this IEnumerable<CachedTransform> cachedTransforms)
    {
        foreach (CachedTransform c in cachedTransforms)
        {
            c.Transform.SetFromCached(c);
        }
    }
}
