using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Rendering;
using Unity.Mathematics;
using UnityEditor;
using rand = UnityEngine.Random;

public static class TextureBlitExt
{
    public static Texture2D ToTexture2D(this Texture texture)
    {
        return Texture2D.CreateExternalTexture(
            texture.width,
            texture.height,
            TextureFormat.RGB24,
            false, false,
            texture.GetNativeTexturePtr());
    }

    public static void SaveToPNG(this Texture2D texture, string path)
    {
        var bytes = texture.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
    }

    public static void SaveToEXR(this Texture2D texture, string path)
    {
        var bytes = texture.EncodeToEXR();
        File.WriteAllBytes(path, bytes);
    }


    /// <summary>
    /// copy sourceTex mip level to mipTexOut, where mip level height/width == mipTexOut height/width
    /// </summary>
    /// <param name="tex"></param>
    /// <param name="mipTex"></param>
    /// <param name="maxLevels"></param>
    public static void CopyTexMipToTex(this Texture2D sourceTex, Texture2D mipTexOut, int maxLevels = 5)
    {
        if (sourceTex.width != sourceTex.height || mipTexOut.width != mipTexOut.height)
        {
            Debug.LogError("Texture aspect ratios must be 1.0 where width == height");
            return;
        }

        int m = 0;
        while (sourceTex.width >> m != mipTexOut.width && m < maxLevels) m++;

        if (m == maxLevels)
        {
            Debug.LogError("No mip level matches output texture width / height");
            return;
        }

        mipTexOut.SetPixels(sourceTex.GetPixels(m));
        mipTexOut.Apply(false);
    }


    /// <summary>
    /// blit sourceTex cropped to texOut size
    /// </summary>
    /// <typeparam name="Texture"></typeparam>
    /// <typeparam name="RenderTexture"></typeparam>
    /// <returns></returns>
    public static void BlitToTexCropped(this Texture sourceTex, Texture2D texOut, Material mat = null, bool calculateMips = false, RenderTextureFormat format = RenderTextureFormat.ARGBHalf)
    {
        int sourceWidth = sourceTex.width;
        int sourceHeight = sourceTex.height;
        var rt = RenderTextureCache.Get(sourceWidth, sourceHeight, 32, format);
        RenderTexture.active = rt;
        if (mat != null)
            Graphics.Blit(sourceTex, rt, mat);
        else
            Graphics.Blit(sourceTex, rt);

        int w = texOut.width;
        int h = texOut.height;
        int x = (sourceWidth >> 1) - (w >> 1);
        int y = (sourceHeight >> 1) - (h >> 1);
        // Graphics.CopyTexture(rt, 0, 0, x, y, w, h, texOut, 0, 0, 0, 0);
        texOut.ReadPixels(new Rect(x, y, w, h), 0, 0, false);
        texOut.Apply(calculateMips);
        RenderTexture.active = null;
    }


    /// <summary>
    /// blit sourceTex to texOut
    /// </summary>
    /// <param name="sourceTex"></param>
    /// <param name="texOut"></param>
    /// <param name="mipLevels"></param>
    public static void BlitToTex(this Texture sourceTex, Texture2D texOut, Material mat = null, bool calculateMips = false, RenderTextureFormat format = RenderTextureFormat.ARGBHalf)
    {
        int w = texOut.width;
        int h = texOut.height;
        var rt = RenderTextureCache.Get(w, h, 32, format);
        RenderTexture.active = rt;
        if (mat != null)
            Graphics.Blit(sourceTex, rt, mat);
        else
            Graphics.Blit(sourceTex, rt);

        int x = (rt.width >> 1) - (w >> 1);
        int y = (rt.height >> 1) - (h >> 1);
        texOut.ReadPixels(new Rect(x, y, w, h), 0, 0, false);
        texOut.Apply(calculateMips);
        RenderTexture.active = null;
    }

}