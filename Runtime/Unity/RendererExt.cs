using System.Collections.Generic;
using UnityEngine;

public static class RendererExt
{
    public static Renderer[] Enabled(this Renderer[] renderers, bool enabled)
    {
        int l = renderers.Length;
        for (int i = 0; i < l; i++)
        {
            renderers[i].enabled = enabled;
        }

        return renderers;
    }

    public static List<Renderer> SetActive(this List<Renderer> objs, bool active)
    {
        int l = objs.Count;
        for (int i = 0; i < l; i++)
        {
            objs[i].gameObject.SetActive(active);
        }
        return objs;
    }

    public static Renderer[] SetActive(this Renderer[] objs, bool active)
    {
        int l = objs.Length;
        for (int i = 0; i < l; i++)
        {
            objs[i].gameObject.SetActive(active);
        }
        return objs;
    }
}