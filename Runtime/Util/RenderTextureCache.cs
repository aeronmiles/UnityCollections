using System;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// cache to store and retreave re-usable render texture references
/// </summary>
public class RenderTextureCache : IEquatable<RenderTextureCache>
{
    public int width;
    public int height;
    public RenderTextureFormat format;
    public int depth;
    public int mipCount;

    static Dictionary<RenderTextureCache, RenderTexture> m_rts = new Dictionary<RenderTextureCache, RenderTexture>();

    public RenderTextureCache(int width, int height, int mipCount, int depth, RenderTextureFormat format)
    {
        this.width = width;
        this.height = height;
        this.format = format;
        this.depth = depth;
        this.mipCount = mipCount;
    }

    public static RenderTexture Get(int width, int height, int depth, RenderTextureFormat format, int mipCount = 0)
    {
        var rtc = new RenderTextureCache(width, height, mipCount, depth, format);
        if (m_rts.ContainsKey(rtc))
        {
            return m_rts[rtc];
        }
        else
        {
            var rt = new RenderTexture(width, height, depth, format, mipCount);
            m_rts.Add(rtc, rt);
            return rt;
        }
    }

    public bool Equals(RenderTextureCache rtt)
    {
        return this.GetHashCode() == rtt.GetHashCode();
    }

    // @TODO: fix hashcode
    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            return 17 * 23 + this.width.GetHashCode() + this.height.GetHashCode() + this.format.GetHashCode() + this.depth.GetHashCode() + this.mipCount.GetHashCode();
        }
    }
}