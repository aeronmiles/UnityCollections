using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;

public static class TextureBlitExt
{
  public static RenderTexture GetTemporaryRT(this Texture texture, int depth = 0)
  {
    var rt = RenderTexture.GetTemporary(texture.width, texture.height, depth, texture.graphicsFormat);
    rt.filterMode = texture.filterMode;
    return rt;
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
    texture.BlitToTex(tex);

    return tex;
  }

  public static Texture2D ConvertToHSV_CPU(this Texture2D texture)
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
    var rt = RenderTexture.GetTemporary(sourceWidth, sourceHeight, 32, format);

    RenderTexture.active = rt;
    GL.Clear(true, true, Color.black);

    // Blit the entire source texture to the render texture
    if (mat != null)
      Graphics.Blit(sourceTex, rt, mat);
    else
      Graphics.Blit(sourceTex, rt);

    // Calculate offsets for positioning the blit in the center
    var scale = new Vector2(texOut.width / (float)texOut.height, texOut.height / (float)sourceHeight);
    var offset = new Vector2(-(1f - (sourceWidth / (float)texOut.width)), -(1f - (sourceHeight / (float)texOut.height)));

    var texRT = RenderTexture.GetTemporary(texOut.width, texOut.height, 32, format);
    // Copy from the render texture to the target texture
    Graphics.Blit(rt, texRT, scale, offset);
    RenderTexture.active = texRT;
    texOut.ReadPixels(new Rect(0, 0, texOut.width, texOut.height), 0, 0, false);
    texOut.Apply(mipChains);

    // Clean up
    RenderTexture.active = null;
    RenderTexture.ReleaseTemporary(rt);
    RenderTexture.ReleaseTemporary(texRT);
  }

  public static void BlitToTexCentreFitted(this Texture sourceTex, Texture2D texOut, Material mat = null, RenderTextureFormat format = RenderTextureFormat.ARGBHalf, bool mipChains = false)
  {
    int sourceWidth = sourceTex.width;
    int sourceHeight = sourceTex.height;
    int destWidth = texOut.width;
    int destHeight = texOut.height;

    // Calculate aspect ratios
    float sourceAspect = sourceWidth / (float)sourceHeight;
    float destAspect = destWidth / (float)destHeight;

    // Calculate scale to fit the source texture into the destination texture
    float scale;
    if (sourceAspect > destAspect)
    {
      // Source is wider
      scale = destWidth / (float)sourceWidth;
    }
    else
    {
      // Source is taller or equal
      scale = destHeight / (float)sourceHeight;
    }

    var rt = RenderTexture.GetTemporary(sourceWidth, sourceHeight, 32, format);

    RenderTexture.active = rt;
    GL.Clear(true, true, Color.black);

    // Blit the entire source texture to the render texture
    if (mat != null)
      Graphics.Blit(sourceTex, rt, mat);
    else
      Graphics.Blit(sourceTex, rt);

    // Create a new temporary RenderTexture where we will draw the source texture centered
    var texRT = RenderTexture.GetTemporary(destWidth, destHeight, 32, format);
    RenderTexture.active = texRT;
    GL.Clear(true, true, Color.black); // Optional: Clear with black color

    // Calculate the rect where to draw the source texture
    int scaledWidth = (int)(sourceWidth * scale);
    int scaledHeight = (int)(sourceHeight * scale);
    Rect targetRect = new Rect((destWidth - scaledWidth) / 2, (destHeight - scaledHeight) / 2, scaledWidth, scaledHeight);

    // Use Graphics.DrawTexture instead of Blit for more control over the positioning
    if (mat != null)
      Graphics.DrawTexture(targetRect, sourceTex, mat);
    else
      Graphics.DrawTexture(targetRect, sourceTex);

    // Copy from the RenderTexture to the target Texture2D
    texOut.ReadPixels(new Rect(0, 0, destWidth, destHeight), 0, 0);
    texOut.Apply(mipChains);

    // Clean up
    RenderTexture.active = null;
    RenderTexture.ReleaseTemporary(rt);
    RenderTexture.ReleaseTemporary(texRT);
  }


  /// <summary>
  /// blit sourceTex to texOut
  /// </summary>
  /// <param name="sourceTex"></param>
  /// <param name="texOut"></param>
  public static void BlitToTex(this Texture sourceTex, Texture2D texOut, Material mat = null, bool mipChains = false)
  {
    int w = texOut.width;
    int h = texOut.height;
    var rt = RenderTexture.GetTemporary(w, h, 32, sourceTex.graphicsFormat);
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
    // int w = texOut.width;
    // int h = texOut.height;
    // var cachedRT = RenderTexture.active;
    sourceTex.BlitMaterials(mats);
    RenderTexture.active = sourceTex;
    Graphics.CopyTexture(sourceTex, 0, 0, texOut, 0, 0);
    // texOut.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
    texOut.Apply(mipChains);

    // Cleanup
    // RenderTexture.active = cachedRT;
  }

  public static IEnumerator BlitToTexAsync(this RenderTexture sourceTex, Texture2D texOut, Material[] mats = null, bool mipChains = false)
  {
    sourceTex.BlitMaterials(mats);

    AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(texOut);
    yield return request;

    texOut.LoadRawTextureData(request.GetData<byte>());
    texOut.Apply(mipChains);
  }

  public static bool IsFloatFormat(this Texture2D texture)
  {
    return texture.format == TextureFormat.RGBAHalf || texture.format == TextureFormat.RGBAFloat || texture.format == TextureFormat.RHalf || texture.format == TextureFormat.RFloat || texture.format == TextureFormat.RGHalf || texture.format == TextureFormat.RGFloat;
  }

  public static IEnumerator LoadImage(string path, Action<Texture2D> callback = null)
  {
    using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(path))
    {
      yield return uwr.SendWebRequest();

      if (uwr.result != UnityWebRequest.Result.Success)
      {
        Debug.Log(uwr.error);
      }
      else
      {
        // Get downloaded asset bundle
        Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
        callback?.Invoke(texture);
      }
    }
  }
}