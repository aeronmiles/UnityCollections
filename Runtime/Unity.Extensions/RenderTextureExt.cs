using UnityEngine;

public static class RenderTextureExt
{
  public static void BlitMaterial(this RenderTexture rtOut, Material blitMat)
  {
    if (blitMat != null)
    {
      var rtTemp = rtOut.GetTemporaryCopy();
      Graphics.Blit(rtOut, rtTemp, blitMat);
      Graphics.Blit(rtTemp, rtOut);
      RenderTexture.ReleaseTemporary(rtTemp);
    }
  }

  public static void BlitMaterials(this RenderTexture rtOut, Material[] blitMats)
  {
    if (blitMats != null && blitMats.Length > 0)
    {
      RenderTexture rtTemp = rtOut.GetTemporaryCopy();
      RenderTexture rtSource = rtOut;
      RenderTexture rtDestination = rtTemp;

      for (int i = 0; i < blitMats.Length; i++)
      {
        Graphics.Blit(rtSource, rtDestination, blitMats[i]);

        // Swap the source and destination for the next blit operation
        (rtSource, rtDestination) = (rtDestination, rtSource);
      }

      // If the last blit destination was rtTemp (i.e., blitMats.Length is odd), we need to blit back to rtOut
      if (blitMats.Length % 2 != 0)
      {
        Graphics.Blit(rtTemp, rtOut);
      }

      RenderTexture.ReleaseTemporary(rtTemp);
    }
  }

  public static TextureFormat ToTextureFormat(this Texture tex)
  {
    switch (tex)
    {
      case Texture2D texture2D:
        return texture2D.format;
      case RenderTexture renderTexture:
        return renderTexture.ToTextureFormat();
      default:
        Debug.LogWarning("Unhandled Texture type - Using RGBA32 as fallback");
        return TextureFormat.RGBA32; // Fallback
    }
  }

  public static TextureFormat ToTextureFormat(this RenderTexture renderTexture)
  {
    switch (renderTexture.format)
    {
      case RenderTextureFormat.ARGB32:
        return TextureFormat.ARGB32;
      case RenderTextureFormat.Depth:
        // Depth textures can't be directly converted to Texture2D.
        // You might need a specialized shader/technique for visualization.
        Debug.LogWarning("Cannot directly convert Depth format to Texture2D. Consider using a visualization shader.");
        return TextureFormat.RGBA32; // Fallback
      case RenderTextureFormat.ARGBHalf:
        return TextureFormat.RGBAHalf;
      case RenderTextureFormat.Shadowmap:
        Debug.LogWarning("Cannot directly convert Shadowmap format to Texture2D. Consider custom handling.");
        return TextureFormat.RGBA32; // Fallback (consider custom handling)
      case RenderTextureFormat.RGB565:
        return TextureFormat.RGB565;
      case RenderTextureFormat.ARGB4444:
        return TextureFormat.ARGB4444;
      // case RenderTextureFormat.ARGB1555:
      //     return TextureFormat.ARGB1555;
      case RenderTextureFormat.Default:
        return TextureFormat.RGBA32; // Reasonable fallback
                                     // case RenderTextureFormat.ARGB2101010:
                                     //     return TextureFormat.ARGB2101010;
      case RenderTextureFormat.DefaultHDR:
        return TextureFormat.RGBAHalf; // Common assumption for HDR
      case RenderTextureFormat.ARGB64:
        return TextureFormat.RGBA64;
      case RenderTextureFormat.ARGBFloat:
        return TextureFormat.RGBAFloat;
      case RenderTextureFormat.RGFloat:
        return TextureFormat.RGFloat;
      case RenderTextureFormat.RGHalf:
        return TextureFormat.RGHalf;
      case RenderTextureFormat.RFloat:
        return TextureFormat.RFloat;
      case RenderTextureFormat.RHalf:
        return TextureFormat.RHalf;
      case RenderTextureFormat.R8:
        return TextureFormat.R8;
      case RenderTextureFormat.ARGBInt:
        return TextureFormat.ARGB32; // No exact equivalent, ARGB32 is close
      case RenderTextureFormat.RGInt:
        return TextureFormat.RG32; // No exact equivalent, RG32 is close
      case RenderTextureFormat.RInt:
        return TextureFormat.R8; // No exact equivalent, R8 is close
      case RenderTextureFormat.BGRA32:
        return TextureFormat.BGRA32;
      // case RenderTextureFormat.RGB111110Float:
      //     return TextureFormat.RGB111110Float;
      case RenderTextureFormat.RG32:
        return TextureFormat.RG32;
      // case RenderTextureFormat.RGBAUShort:
      //     return TextureFormat.RGBAUShort;
      case RenderTextureFormat.RG16:
        return TextureFormat.RG16;
      // case RenderTextureFormat.BGRA10101010_XR:
      //     return TextureFormat.BGRA10101010_XR;
      // case RenderTextureFormat.BGR101010_XR:
      //     return TextureFormat.BGR101010_XR;
      case RenderTextureFormat.R16:
        return TextureFormat.R16;
      default:
        Debug.LogWarning("Unhandled RenderTextureFormat - Using RGBA32 as fallback");
        return TextureFormat.RGBA32; // Fallback
    }
  }

  public static Texture2D AsTexture2D(this RenderTexture rt, bool linear)
  {
    Texture2D texture2D = new Texture2D(rt.width, rt.height, rt.ToTextureFormat(), rt.useMipMap, linear)
    {
      filterMode = rt.filterMode
    };
    return texture2D;
  }

  public static RenderTexture GetTemporaryCopy(this RenderTexture rt) => RenderTexture.GetTemporary(rt.width, rt.height, rt.depth, rt.format);

  public static Texture2D ToTexture2D(this RenderTexture rt, bool linear)
  {
    Texture2D texture2D = new Texture2D(rt.width, rt.height, rt.ToTextureFormat(), rt.useMipMap, linear)
    {
      filterMode = rt.filterMode
    };
    return texture2D;
  }

  public static Texture2D RotateAndScale(Texture2D sourceTexture, float rotation, Vector2 scale)
  {
    // Create a temporary RenderTexture for the rotation
    int width = sourceTexture.width;
    int height = sourceTexture.height;

    // Swap width and height if rotating 90 or 270 degrees
    if (Mathf.Approximately(Mathf.Abs(rotation % 180), 90))
    {
      int temp = width;
      width = height;
      height = temp;
    }

    RenderTexture rt = RenderTexture.GetTemporary(width, height, 0);
    rt.useMipMap = false;

    // Create a material for the rotation and scaling
    Material material = new Material(Shader.Find("Hidden/Internal-Colored"))
    {
      hideFlags = HideFlags.HideAndDontSave
    };

    // Set up the rotation matrix
    float rad = rotation * Mathf.Deg2Rad;
    Vector2 pivot = new Vector2(0.5f, 0.5f);
    Matrix4x4 matrix = Matrix4x4.TRS(
        new Vector3(0.5f, 0.5f, 0),
        Quaternion.Euler(0, 0, rotation),
        new Vector3(scale.x, scale.y, 1)
    ) * Matrix4x4.TRS(new Vector3(-0.5f, -0.5f, 0), Quaternion.identity, Vector3.one);

    // Apply the transformation
    Graphics.Blit(sourceTexture, rt, material);

    // Convert back to Texture2D
    Texture2D result = rt.ToTexture2D(false);

    // Clean up
    RenderTexture.ReleaseTemporary(rt);
    Object.DestroyImmediate(material);

    return result;
  }
}