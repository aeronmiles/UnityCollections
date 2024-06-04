using System.Collections;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public static class TextureExt
{
  public static RenderTexture GetTemporaryRT(this Texture texture, int depth = 0)
  {
    var rt = RenderTexture.GetTemporary(texture.width, texture.height, depth, texture.graphicsFormat);
    rt.filterMode = texture.filterMode;
    return rt;
  }

  public static Texture3D ToTexture3D(this RenderTexture rt, TextureFormat format, bool mipChains = false)
  {
    if (rt.volumeDepth == 0)
    {
      Debug.LogError("RenderTexture is not a 3D texture");
      return null;
    }

    var tex = new Texture3D(rt.width, rt.height, rt.volumeDepth, format, mipChains)
    {
      filterMode = rt.filterMode,
      wrapMode = rt.wrapMode
    };
    Graphics.CopyTexture(rt, 0, 0, tex, 0, 0);
    return tex;
  }

  public static Texture2D ToTexture2D(this Texture texture, TextureFormat format, bool linear = false, bool mipChains = false)
  {
    // return Texture2D.CreateExternalTexture(
    //     texture.width,
    //     texture.height,
    //     format,
    //     mipChains, linear,
    //     texture.GetNativeTexturePtr());

    var tex = new Texture2D(texture.width, texture.height, format, mipChains, linear)
    {
      filterMode = texture.filterMode,
      wrapMode = texture.wrapMode
    };
    Graphics.CopyTexture(texture, 0, 0, tex, 0, 0);
    return tex;
  }

  public static Texture2D ConvertToHSV(this Texture2D texture)
  {
    var tex = new Texture2D(texture.width, texture.height, texture.format, false, true)
    {
      filterMode = texture.filterMode,
      wrapMode = texture.wrapMode
    };
    var pixels = texture.GetPixels();
    int l = pixels.Length;
    for (int i = 0; i < l; i++)
    {
      Color c = pixels[i];
      Color.RGBToHSV(c, out float h, out float s, out float v);
      pixels[i] = new Color(h, s, v, pixels[i].a);
    }
    tex.SetPixels(pixels);
    tex.Apply();

    return tex;
  }

  public static void SaveToPNG(this Texture2D texture, string path)
  {
    if (!texture.isReadable)
    {
      texture = texture.ToTexture2D(texture.format);
    }
    File.WriteAllBytes(path, texture.ToTexture2D(texture.format).EncodeToPNG());
  }

  public static void SaveToJPG(this Texture2D texture, string path, int quality = 100)
  {
    if (!texture.isReadable)
    {
      texture = texture.ToTexture2D(texture.format);
    }
    File.WriteAllBytes(path, texture.EncodeToJPG(quality));
  }

  public static void SaveToEXR(this Texture2D texture, string path)
  {
    if (!texture.isReadable)
    {
      texture = texture.ToTexture2D(texture.format);
    }
    File.WriteAllBytes(path, texture.EncodeToEXR());
  }

  /// <summary>
  /// copy sourceTex mip level to mipTexOut, where mip level height/width == mipTexOut height/width
  /// </summary>
  /// <param name="tex"></param>
  /// <param name="mipTex"></param>
  /// <param name="maxLevels"></param>
  public static void CopyTexMipToTex(this Texture2D sourceTex, Texture2D mipTexOut, int maxLevels = 5, bool mipChains = false)
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
    mipTexOut.Apply(mipChains);
  }

  /// <summary>
  /// blit sourceTex cropped to texOut size
  /// </summary>
  /// <typeparam name="Texture"></typeparam>
  /// <typeparam name="RenderTexture"></typeparam>
  /// <returns></returns>
  public static void BlitToTexCropped(this Texture sourceTex, Texture2D texOut, Material mat = null, RenderTextureFormat format = RenderTextureFormat.ARGBHalf, bool mipChains = false)
  {
    int sourceWidth = sourceTex.width;
    int sourceHeight = sourceTex.height;
    var rt = RenderTexture.GetTemporary(sourceWidth, sourceHeight, 32, format);

    RenderTexture.active = rt;
    if (mat != null)
      Graphics.Blit(sourceTex, rt, mat);
    else
      Graphics.Blit(sourceTex, rt);

    int w = texOut.width;
    int h = texOut.height;
    int x = (sourceWidth >> 1) - (w >> 1);
    int y = (sourceHeight >> 1) - (h >> 1);
    texOut.ReadPixels(new Rect(x, y, w, h), 0, 0, false);
    texOut.Apply(mipChains);
    RenderTexture.active = null;

    // Cleanup
    RenderTexture.ReleaseTemporary(rt);
  }

  /// <summary>
  /// blit sourceTex to texOut centre region
  /// </summary>
  /// <param name="sourceTex"></param>
  /// <param name="texOut"></param>
  /// <param name="mat"></param>
  /// <param name="format"></param> <summary>
  /// </summary>
  public static void BlitToTexCentre(this Texture sourceTex, Texture2D texOut, Material mat = null, RenderTextureFormat format = RenderTextureFormat.ARGBHalf, bool mipChains = false)
  {
    int sourceWidth = sourceTex.width;
    int sourceHeight = sourceTex.height;

    var rt = RenderTexture.GetTemporary(sourceWidth, sourceHeight, 0, format);
    RenderTexture.active = rt;
    GL.Clear(true, true, Color.black);

    // Blit the entire source texture to the render texture
    if (mat != null)
      Graphics.Blit(sourceTex, rt, mat);
    else
      Graphics.Blit(sourceTex, rt);

    float scaleX = (float)texOut.width / sourceTex.width;
    float scaleY = (float)texOut.height / sourceTex.height;
    float minScale = Mathf.Min(scaleX, scaleY); // Ensures the  source texture fits entirely within texOut

    // Calculate offsets to center the scaled source texture within texOut
    float offsetX = (texOut.width - (sourceTex.width * minScale)) * 0.5f;
    float offsetY = (texOut.height - (sourceTex.height * minScale)) * 0.5f;

    var texRT = RenderTexture.GetTemporary(texOut.width, texOut.height, 0, format);
    Graphics.Blit(rt, texRT);

    RenderTexture.active = texRT;
    texOut.ReadPixels(new Rect(0, 0, sourceTex.width * minScale, sourceTex.height * minScale), 0, 0, false);
    texOut.Apply(mipChains);

    // Clean up
    RenderTexture.active = null;
    RenderTexture.ReleaseTemporary(rt);
    RenderTexture.ReleaseTemporary(texRT);
  }

  public static void BlitToTexCentreFitted(this Texture2D source, Texture2D destination, bool fitInside = true)
  {
    // Calculate the scale factor and offsets
    float srcAspect = (float)source.width / source.height;
    float dstAspect = (float)destination.width / destination.height;

    float scaleWidth, scaleHeight;
    if ((fitInside && srcAspect > dstAspect) || (!fitInside && srcAspect < dstAspect))
    {
      scaleWidth = 1.0f;
      scaleHeight = srcAspect / dstAspect;
    }
    else
    {
      scaleWidth = dstAspect / srcAspect;
      scaleHeight = 1.0f;
    }

    float offsetX = (1.0f - scaleWidth) / 2.0f;
    float offsetY = (1.0f - scaleHeight) / 2.0f;

    // Create a temporary RenderTexture
    RenderTexture tempRT = RenderTexture.GetTemporary(destination.width, destination.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

    // Set the active RenderTexture
    RenderTexture.active = tempRT;

    // Clear the RenderTexture
    GL.Clear(true, true, Color.clear);

    // Set up the material and pass the scale and offset
    Material material = new Material(Shader.Find("Hidden/BlitCropped"));
    material.SetTexture("_MainTex", source);
    material.SetVector("_CropRect", new Vector4(offsetX, offsetY, scaleWidth, scaleHeight));

    // Blit the texture
    Graphics.Blit(source, tempRT, material);

    // Copy the result to the destination texture
    RenderTexture.active = tempRT;
    destination.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
    destination.Apply();

    // Release the temporary RenderTexture
    RenderTexture.ReleaseTemporary(tempRT);

    // Reset the active RenderTexture
    RenderTexture.active = null;
  }

  // public static void BlitToTexCentreFitted(this Texture sourceTex, Texture2D texOut, Material mat = null, RenderTextureFormat format = RenderTextureFormat.ARGBHalf, bool mipChains = false)
  // {
  //   int sourceWidth = sourceTex.width;
  //   int sourceHeight = sourceTex.height;
  //   int destWidth = texOut.width;
  //   int destHeight = texOut.height;

  //   // Calculate aspect ratios
  //   float scaleWidth = destWidth / (float)sourceWidth;
  //   float scaleHeight = destHeight / (float)sourceHeight;
  //   float scale = Mathf.Min(scaleWidth, scaleHeight);

  //   // Create a new temporary RenderTexture where we will draw the source texture centered
  //   var texRT = RenderTexture.GetTemporary(destWidth, destHeight, 0, format);
  //   RenderTexture.active = texRT;
  //   GL.Clear(true, true, Color.black); // Optional: Clear with black color

  //   // Calculate the rect where to draw the source texture
  //   int scaledWidth = Mathf.RoundToInt(sourceWidth * scale);
  //   int scaledHeight = Mathf.RoundToInt(sourceHeight * scale);
  //   Rect targetRect = new Rect((destWidth - scaledWidth) * 0.5f, (destHeight - scaledHeight) * 0.5f, scaledWidth, scaledHeight);

  //   // Use Graphics.DrawTexture to draw the source texture into the target rect
  //   if (mat != null)
  //   {
  //     Graphics.DrawTexture(targetRect, sourceTex, mat);
  //   }
  //   else
  //   {
  //     Graphics.DrawTexture(targetRect, sourceTex);
  //   }

  //   // Copy from the RenderTexture to the target Texture2D
  //   texOut.ReadPixels(new Rect(0, 0, destWidth, destHeight), 0, 0);
  //   texOut.Apply(mipChains);

  //   // Clean up
  //   RenderTexture.active = null;
  //   RenderTexture.ReleaseTemporary(texRT);
  // }

  /// <summary>
  /// blit sourceTex to texOut
  /// </summary>
  /// <param name="sourceTex"></param>
  /// <param name="texOut"></param>
  public static void BlitToTex(this Texture sourceTex, Texture2D texOut, Material mat = null, bool mipChains = false)
  {
    int w = texOut.width;
    int h = texOut.height;
    var rt = RenderTexture.GetTemporary(w, h, 0, sourceTex.graphicsFormat);
    var cachedRT = RenderTexture.active;

    RenderTexture.active = rt;
    if (mat != null)
      Graphics.Blit(sourceTex, rt, mat);
    else
      Graphics.Blit(sourceTex, rt);

    // @TODO: Optimize
    // Graphics.CopyTexture(rt, 0, 0, texOut, 0, 0);
    texOut.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
    texOut.Apply(mipChains);

    // Cleanup
    RenderTexture.active = cachedRT;
    RenderTexture.ReleaseTemporary(rt);
  }

  public static void BlitToTex(this RenderTexture sourceTex, Texture2D texOut, Material[] mats, bool mipChains = false)
  {
    sourceTex.BlitMaterials(mats);
    RenderTexture.active = sourceTex;
    Graphics.CopyTexture(sourceTex, 0, 0, texOut, 0, 0);
    texOut.Apply(mipChains);
  }

  public static IEnumerator BlitToTexAsync(this RenderTexture sourceTex, Texture2D texOut, Material[] mats = null, bool mipChains = false)
  {
    throw new System.NotImplementedException();
  }

  public static bool IsFloatFormat(this Texture2D texture)
  {
    return texture.format == TextureFormat.RGBAHalf || texture.format == TextureFormat.RGBAFloat || texture.format == TextureFormat.RHalf || texture.format == TextureFormat.RFloat || texture.format == TextureFormat.RGHalf || texture.format == TextureFormat.RGFloat;
  }
}