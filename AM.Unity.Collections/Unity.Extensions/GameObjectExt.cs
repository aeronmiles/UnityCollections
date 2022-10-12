using System.Collections.Generic;
using UnityEngine;

public static class GameObjectExt
{
    public static GameObject ByName(this GameObject[] gos, string name)
    {
        int l = gos.Length;
        for (int i = 0; i < l; i++)
        {
            if (gos[i].name == name) return gos[i];
        }
        return null;
    }

    public static GameObject[] EnableRenderers(this GameObject[] objs, bool show)
    {
        int l = objs.Length;
        for (int i = 0; i < l; i++)
        {
            GameObject o = objs[i];
            o.GetComponentsInChildren<Renderer>(true).Enabled(show);
        }

        return objs;
    }

    public static GameObject[] EnableColliders(this GameObject[] objs, bool show)
    {
        int l = objs.Length;
        for (int i = 0; i < l; i++)
        {
            GameObject o = objs[i];
            o.GetComponentsInChildren<Collider>(true).Enabled(show);
        }

        return objs;
    }

    public static GameObject[] EnableCanvases(this GameObject[] objs, bool show)
    {
        int l = objs.Length;
        for (int i = 0; i < l; i++)
        {
            GameObject o = objs[i];
            o.GetComponentsInChildren<Canvas>(true).Enabled(show);
        }

        return objs;
    }

    public static GameObject[] TakeRandom(this GameObject[] objs, int number)
    {
        if (number > objs.Length)
        {
            Debug.LogError("Not enough objects in array");
            return null;
        }

        int c = objs.Length;
        int n = 0;
        GameObject[] objOut = new GameObject[number];
        while (n < number)
        {
            objOut[n] = objs[UnityEngine.Random.Range(0, c)];
            n++;
        }

        return objOut;
    }

    public static List<GameObject> Children(this GameObject go)
    {
        var childs = go.GetComponentsInChildren<Transform>(true);
        List<GameObject> objs = new List<GameObject>();
        int l = childs.Length;
        for (int i = 0; i < l; i++)
        {
            if (childs[i].parent == go.transform) objs.Add(childs[i].gameObject);
        }
        return objs;
    }

    public static List<GameObject> AllChildren(this GameObject go)
    {
        var childs = go.GetComponentsInChildren<Transform>(true);
        List<GameObject> objs = new List<GameObject>();

        int l = childs.Length;
        for (int i = 0; i < l; i++)
        {
            if (childs[i].gameObject != go) objs.Add(childs[i].gameObject);
        }

        return objs;
    }

    public static bool AllActive(this GameObject[] gos)
    {
        if (gos.Length == 0) return true;

        foreach (var go in gos)
        {
            if (go != null && !go.activeSelf) return false;
        }

        return true;
    }

    public static bool NoneActive(this GameObject[] gos)
    {
        if (gos.Length == 0) return true;

        foreach (var go in gos)
        {
            if (go != null && go.activeSelf) return false;
        }

        return true;
    }

    public static List<GameObject> SetActive(this List<GameObject> gos, bool active)
    {
        int l = gos.Count;
        for (int i = 0; i < l; i++)
        {
            gos[i].SetActive(active);
        }
        return gos;
    }

    public static GameObject[] SetActive(this GameObject[] gos, bool active)
    {
        int l = gos.Length;
        for (int i = 0; i < l; i++)
        {
            if (gos[i] != null) gos[i].SetActive(active);
        }
        return gos;
    }

    public static GameObject FindChild(this GameObject parent, string name)
    {
        Transform[] trs = parent.GetComponentsInChildren<Transform>(true);
        int l = trs.Length;
        for (int i = 0; i < l; i++)
        {
            if (trs[i].name == name)
            {
                return trs[i].gameObject;
            }
        }
        return null;
    }

    public static GameObject FindObjectWithTag(this GameObject parent, string tag)
    {
        Transform[] trs = parent.GetComponentsInChildren<Transform>(true);
        int l = trs.Length;
        for (int i = 0; i < l; i++)
        {
            if (trs[i].tag == tag)
            {
                return trs[i].gameObject;
            }
        }
        return null;
    }

    public static List<Transform> Transforms(this List<GameObject> objs)
    {
        List<Transform> Transforms = new List<Transform>();

        int l = objs.Count;
        for (int i = 0; i < l; i++)
        {
            Transforms.Add(objs[i].transform);
        }

        return Transforms;

    }

    public static Bounds Bounds(this Bounds[] bounds)
    {
        Vector3 min = float.MaxValue.ToVector3();
        Vector3 max = float.MinValue.ToVector3();
        int l = bounds.Length;
        for (int i = 0; i < l; i++)
        {
            min = new Vector3(Mathf.Min(min.x, bounds[i].min.x), 
            Mathf.Min(min.y, bounds[i].min.y), Mathf.Min(min.z, bounds[i].min.z));
            max = new Vector3(Mathf.Max(max.x, bounds[i].max.x), 
            Mathf.Max(max.y, bounds[i].max.y), Mathf.Max(max.z, bounds[i].max.z));
        }

        return new Bounds((max + min) * 0.5f, max - min);
    }
}