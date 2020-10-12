using System;
using System.Collections.Generic;
using System.Linq;
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

    public static List<Renderer> SetActive(this List<Renderer> renderers, bool active)
    {
        int l = renderers.Count;
        for (int i = 0; i < l; i++)
        {
            renderers[i].gameObject.SetActive(active);
        }
        return renderers;
    }

    public static Renderer[] SetActive(this Renderer[] renderers, bool active)
    {
        int l = renderers.Length;
        for (int i = 0; i < l; i++)
        {
            renderers[i].gameObject.SetActive(active);
        }
        return renderers;
    }

    public static List<Material> SharedMaterials(this Renderer[] renderers)
    {
        List<Material> materials = new List<Material>();
        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials;
            foreach (var m in mats)
            {
                if (m != null)
                {
                    materials.Add(m);
                }
                else
                {
                    Debug.Log(r.gameObject.name + " material is null");
                }
            }
        }

        return materials;
    }

    public static List<Material> Materials(this Renderer[] renderers)
    {
        List<Material> materials = new List<Material>();
        foreach (var r in renderers)
        {
            var mats = r.materials;
            foreach (var m in mats)
            {
                if (m != null)
                {
                    materials.Add(m);
                }
                else
                {
                    Debug.Log(r.gameObject.name + " material is null");
                }
            }
        }

        return materials;
    }

    public static Material[] UniqueMaterials(this Renderer[] renderers)
    {
        return renderers.Materials().GroupBy(m => m.name).Select(g => g.First()).ToArray();
    }

    public static Material[] UniqueSharedMaterials(this Renderer[] renderers)
    {
        foreach (var r in renderers.SharedMaterials().GroupBy(m => m.name))
            Debug.Log(r);

        return renderers.SharedMaterials().GroupBy(m => m.name).Select(g => g.FirstOrDefault()).ToArray();
    }
}