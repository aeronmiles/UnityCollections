using System;
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

    public static Material[] SharedMaterials(this Renderer[] renderers)
    {
        int n = 0;
        foreach (var r in renderers) 
            n += r.sharedMaterials.Length;
        
        Material[] materials = new Material[n];
        int i = 0;
        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials;
            foreach (var m in mats) materials[i++] = m;
        }

        return materials;
    }
    
    public static Material[] Materials(this Renderer[] renderers)
    {
        int n = 0;
        foreach (var r in renderers) 
            n += r.materials.Length;
        
        Material[] materials = new Material[n];
        int i = 0;
        foreach (var r in renderers)
        {
            var mats = r.materials;
            foreach (var m in mats) materials[i++] = m;
        }

        return materials;
    }
}