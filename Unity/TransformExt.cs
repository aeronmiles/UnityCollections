using System;
using System.Collections.Generic;
using UnityEngine;

public static class TransformExt
{
    public static Transform[] Children(this Transform transform)
    {
        var childs = transform.GetComponentsInChildren<Transform>(true);
        Transform[] objs = new Transform[0];
        int l = childs.Length;
        int x = 0;
        for (int i = 0; i < l; i++)
        {
            if (childs[i].parent == transform)
            {
                Array.Resize(ref objs, x++);
                objs[x-1] = childs[i];
            }
        }
        return objs;
    }

    public static List<Transform> SetActive(this List<Transform> objs, bool active)
    {
        int l = objs.Count;
        for (int i = 0; i < l; i++)
        {
            objs[i].gameObject.SetActive(active);
        }
        return objs;
    }

    public static Transform[] SetActive(this Transform[] objs, bool active)
    {
        int l = objs.Length;
        for (int i = 0; i < l; i++)
        {
            objs[i].gameObject.SetActive(active);
        }
        return objs;
    }
    
    public static Vector3[] Positions(this Transform[] ts)
    {
        int l = ts.Length;
        Vector3[] positions = new Vector3[l];
        for (int i = 0; i < l; i++)
        {
            positions[i] = ts[i].position;
        }

        return positions;
    }

    public static Vector3[] EulerAngles(this Transform[] ts)
    {
        int l = ts.Length;
        Vector3[] angles = new Vector3[l];
        for (int i = 0; i < l; i++)
        {
            angles[i] = ts[i].eulerAngles;
        }

        return angles;
    }
}