using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class TextureExt
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static RenderTexture GetTemporaryRT(this Texture texture, int depth = 0)
  {
    var rt = RenderTexture.GetTemporary(texture.width, texture.height, depth, texture.graphicsFormat);
    rt.filterMode = texture.filterMode;
    return rt;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Texture2D ToTexture2D(this Texture texture, TextureFormat format, bool linear = false, bool mipChains = false)
  {
    // return Texture2D.CreateExternalTexture(
    //     texture.width,
    //     texture.height,
    //     format,
    //     mipChains, linear,
    //     texture.GetNativeTexturePtr());
    Debug.Log("TextureExt :: ToTexture2D");

    var tex = new Texture2D(texture.width, texture.height, format, mipChains, linear)
    {
      filterMode = texture.filterMode,
      wrapMode = texture.wrapMode
    };
    tex.name = "TextureExt::ToTexture2D::tex";
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
    tex.name = "TextureExt::ConvertToHSV::tex";
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
    while (sourceTex.width >> m != mipTexOut.width && m < maxLevels)
    {
      m++;
    }

    if (m == maxLevels)
    {
      Debug.LogError("No mip level matches output texture width / height");
      return;
    }

    mipTexOut.SetPixels(sourceTex.GetPixels(m));
    mipTexOut.Apply(mipChains);
  }

  private static Material _TileRotateMaterial;
  private static Material TileRotateMaterial
  {
    get
    {
      if (_TileRotateMaterial == null)
      {
        var shader = Shader.Find("AM/Unlit/TileRotate");
        if (shader == null)
        {
          Debug.LogError("Failed to find TileRotate shader. Make sure it's included in the project and the name is correct.");
        }
        else
        {
          _TileRotateMaterial = new Material(shader);
        }
      }
      return _TileRotateMaterial;
    }
  }

  [BurstCompile]
  public static void Rotate90(this Texture2D sourceTex, Texture2D outTex, bool counterClockwise = false)
  {
    if (sourceTex == null)
    {
      throw new ArgumentNullException(nameof(sourceTex), "Source texture is null.");
    }

    if (outTex == null)
    {
      throw new ArgumentNullException(nameof(outTex), "Output texture is null.");
    }

    if (sourceTex.width != outTex.height || sourceTex.height != outTex.width)
    {
      throw new ArgumentException("Output texture dimensions must be swapped source texture dimensions.");
    }

    if (sourceTex.format != outTex.format)
    {
      throw new ArgumentException("Output texture format must match source texture format.");
    }

    var sourceBytes = sourceTex.GetRawTextureData<byte>();
    var outputBytes = outTex.GetRawTextureData<byte>();

    int bytesPerPixel = GetBytesPerPixel(sourceTex.format);

    var job = new Rotate90Job
    {
      sourceBytes = sourceBytes,
      outputBytes = outputBytes,
      dimensions = new int2(sourceTex.width, sourceTex.height),
      bytesPerPixel = bytesPerPixel,
      counterClockwise = counterClockwise ? 1 : 0
    };

    // Calculate the optimal batch size (now in pixels)
    int totalPixels = sourceTex.width * sourceTex.height;
    int batchSize = math.max(32, math.min(1024, totalPixels / SystemInfo.processorCount));

    job.ScheduleParallel(totalPixels, batchSize, default).Complete();
    outTex.Apply();

    Debug.Log($"Rotated {sourceTex.width}x{sourceTex.height} texture by 90 degrees {(counterClockwise ? "counter-clockwise" : "clockwise")}");
  }

  private static int GetBytesPerPixel(TextureFormat format) => format switch
  {
    TextureFormat.Alpha8 => 1,
    TextureFormat.RGB24 => 3,
    TextureFormat.RGBA32 => 4,
    _ => throw new ArgumentException($"Unsupported texture format: {format}")
  };

  [BurstCompile]
  private struct Rotate90Job : IJobFor
  {
    [ReadOnly] public NativeArray<byte> sourceBytes;
    [WriteOnly] public NativeArray<byte> outputBytes;
    public int2 dimensions;
    public int bytesPerPixel;
    public int counterClockwise;

    public void Execute(int index)
    {
      int2 output = math.int2(index % dimensions.y, index / dimensions.y);
      int2 source = counterClockwise == 1
          ? math.int2(dimensions.x - 1 - output.y, output.x)
          : math.int2(output.y, dimensions.y - 1 - output.x);

      int sourceIndex = (source.y * dimensions.x + source.x) * bytesPerPixel;
      int outputIndex = index * bytesPerPixel;

      // Copy all bytes for the pixel at once
      for (int i = 0; i < bytesPerPixel; i++)
      {
        outputBytes[outputIndex + i] = sourceBytes[sourceIndex + i];
      }
    }
  }

  public static void BlitTileRotate(this Texture2D sourceTex, Texture2D texOut, float tileX, float tileY, float angle)
  {
    if (sourceTex == null)
    {
      throw new ArgumentNullException(nameof(sourceTex), "Source texture is null.");
    }
    else if (texOut == null)
    {
      throw new ArgumentNullException(nameof(texOut), "Output texture is null.");
    }

    float sourceAspect = (float)sourceTex.width / sourceTex.height;
    float outputAspect = (float)texOut.width / texOut.height;

    // Calculate scaling factors to fit the source texture into the output texture
    float scaleX = 1f, scaleY = 1f;
    if (sourceAspect > outputAspect)
    {
      // Source is wider, scale to fit height
      scaleY = outputAspect / sourceAspect;
    }
    else
    {
      // Source is taller, scale to fit width
      scaleX = sourceAspect / outputAspect;
    }

    // Apply tiling to the calculated scale
    scaleX *= tileX;
    scaleY *= tileY;

    var rt = RenderTexture.GetTemporary(texOut.width, texOut.height, 0, RenderTextureFormat.ARGB32);
    rt.name = "TextureExt::BlitTileRotate::rt";

    TileRotateMaterial.SetTexture("_MainTex", sourceTex);
    TileRotateMaterial.SetVector("_TileXY", new Vector4(scaleX, scaleY, 0.0f, 0.0f));
    TileRotateMaterial.SetFloat("_RotationRadians", Mathf.Deg2Rad * angle);
    TileRotateMaterial.SetFloat("_AspectRatio", sourceAspect / outputAspect);

    Graphics.Blit(sourceTex, rt, TileRotateMaterial);
    var cachedRT = RenderTexture.active;
    RenderTexture.active = rt;
    texOut.ReadPixels(new Rect(0, 0, texOut.width, texOut.height), 0, 0, false);
    texOut.Apply();

    // Cleanup
    RenderTexture.active = cachedRT;
    RenderTexture.ReleaseTemporary(rt);
  }

  [BurstCompile]
  private struct TileRotateJob : IJobParallelFor
  {
    [ReadOnly] public NativeArray<Color32> inputPixels;
    [WriteOnly] public NativeArray<Color32> outputPixels;
    public int2 inputDimensions;
    public int2 outputDimensions;
    public float2 scale;
    public float rotationRadians;
    public float aspectRatio;

    public void Execute(int index)
    {
      int2 outputCoord = new int2(index % outputDimensions.x, index / outputDimensions.x);
      float2 uv = new float2(outputCoord.x / (float)(outputDimensions.x - 1), outputCoord.y / (float)(outputDimensions.y - 1));

      // Center UV
      uv -= 0.5f;

      // Apply rotation
      float s = math.sin(rotationRadians);
      float c = math.cos(rotationRadians);
      float2x2 rotationMatrix = new float2x2(c, -s, s, c);
      uv = math.mul(rotationMatrix, uv);

      // Apply scaling and aspect ratio adjustment
      uv.y *= aspectRatio;
      uv *= scale;
      // Move UV back to [0, 1] range
      uv += 0.5f;

      // Clamp UV coordinates
      uv = math.clamp(uv, 0, 1);

      // Sample the input texture
      int2 inputCoord = new int2((int)(uv.x * (inputDimensions.x - 1)), (int)(uv.y * (inputDimensions.y - 1)));
      int inputIndex = inputCoord.y * inputDimensions.x + inputCoord.x;

      outputPixels[index] = inputPixels[inputIndex];
    }
  }

  public static void JobifiedTileRotate(this Texture2D sourceTex, Texture2D texOut, float tileX, float tileY, float angle)
  {
    if (sourceTex == null || texOut == null)
    {
      throw new ArgumentNullException(sourceTex == null ? nameof(sourceTex) : nameof(texOut), "Source or output texture is null.");
    }

    // Ensure the textures are readable
    if (!sourceTex.isReadable || !texOut.isReadable)
    {
      throw new InvalidOperationException("Textures must be marked as readable in import settings.");
    }

    float sourceAspect = (float)sourceTex.width / sourceTex.height;
    float outputAspect = (float)texOut.width / texOut.height;

    // Calculate scaling factors to fit the source texture into the output texture
    float scaleX = 1f, scaleY = 1f;
    if (sourceAspect > outputAspect)
    {
      // Source is wider, scale to fit height
      scaleY = outputAspect / sourceAspect;
    }
    else
    {
      // Source is taller, scale to fit width
      scaleX = sourceAspect / outputAspect;
    }

    // Apply tiling to the calculated scale
    scaleX *= tileX;
    scaleY *= tileY;

    // Get native arrays from texture data
    NativeArray<Color32> sourcePixels = sourceTex.GetRawTextureData<Color32>();
    NativeArray<Color32> outputPixels = texOut.GetRawTextureData<Color32>();

    var job = new TileRotateJob
    {
      inputPixels = sourcePixels,
      outputPixels = outputPixels,
      inputDimensions = new int2(sourceTex.width, sourceTex.height),
      outputDimensions = new int2(texOut.width, texOut.height),
      scale = new float2(scaleX, scaleY),
      rotationRadians = angle * Mathf.Deg2Rad,
      aspectRatio = sourceAspect / outputAspect
    };

    JobHandle handle = job.Schedule(outputPixels.Length, 64);
    handle.Complete();

    // Apply changes to the output texture
    texOut.Apply();
  }

  public static async void JobifiedTileRotateAsync(this Texture2D sourceTex, Texture2D texOut, float tileX, float tileY, float angle, Action<Texture2D> onSuccess, Action<string> onError)
  {
    try
    {
      bool result = await Task.Run(() =>
      {
        if (sourceTex == null || texOut == null)
        {
          throw new ArgumentNullException(sourceTex == null ? nameof(sourceTex) : nameof(texOut), "Source or output texture is null.");
        }

        if (!sourceTex.isReadable || !texOut.isReadable)
        {
          throw new InvalidOperationException("Textures must be marked as readable in import settings.");
        }

        float sourceAspect = (float)sourceTex.width / sourceTex.height;
        float outputAspect = (float)texOut.width / texOut.height;

        float scaleX = 1f, scaleY = 1f;
        if (sourceAspect > outputAspect)
        {
          scaleY = outputAspect / sourceAspect;
        }
        else
        {
          scaleX = sourceAspect / outputAspect;
        }

        scaleX *= tileX;
        scaleY *= tileY;

        NativeArray<Color32> sourcePixels = sourceTex.GetRawTextureData<Color32>();
        NativeArray<Color32> outputPixels = texOut.GetRawTextureData<Color32>();

        try
        {
          var job = new TileRotateJob
          {
            inputPixels = sourcePixels,
            outputPixels = outputPixels,
            inputDimensions = new int2(sourceTex.width, sourceTex.height),
            outputDimensions = new int2(texOut.width, texOut.height),
            scale = new float2(1f / scaleX, 1f / scaleY),
            rotationRadians = angle * Mathf.Deg2Rad,
            aspectRatio = sourceAspect / outputAspect
          };

          JobHandle handle = job.Schedule(outputPixels.Length, 64);
          handle.Complete();

          return true;
        }
        catch (Exception e)
        {
          throw new Exception("Error during job execution: " + e.Message, e);
        }
      });

      if (result)
      {
        UnityMainThreadDispatcher.I.Enqueue(() =>
        {
          try
          {
            texOut.Apply();
            onSuccess?.Invoke(texOut);
          }
          catch (Exception e)
          {
            onError?.Invoke("Error applying texture changes: " + e.Message);
          }
        });
      }
    }
    catch (Exception e)
    {
      UnityMainThreadDispatcher.I.Enqueue(() =>
      {
        onError?.Invoke("TileRotate operation failed: " + e.Message);
      });
    }
  }

  public static bool TileRotate(this Texture2D tex, float tileX, float tileY, float angle)
  {
    if (tex == null)
    {
      throw new ArgumentNullException(nameof(tex), "Texture is null.");
    }

    // Render to texture
    var rt = tex.GetTemporaryRT();
    rt.name = "TextureExt::TileRotate::rt";

    TileRotateMaterial.SetTexture("_MainTex", tex);
    TileRotateMaterial.SetVector("_TileXY", new Vector4(tileX, tileY, 0.0f, 0.0f));
    TileRotateMaterial.SetFloat("_RotationRadians", Mathf.Deg2Rad * angle);
    TileRotateMaterial.SetFloat("_AspectRatio", (float)tex.width / tex.height);

    // Perform the blit with cropping
    Graphics.Blit(tex, rt, TileRotateMaterial);

    var cachedRT = RenderTexture.active;
    RenderTexture.active = rt;
    tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
    tex.Apply();

    // Cleanup
    RenderTexture.active = cachedRT;
    RenderTexture.ReleaseTemporary(rt);

    return true;
  }

  /// <summary>
  /// blit sourceTex cropped to texOut size
  /// </summary>
  /// <typeparam name="Texture"></typeparam>
  /// <typeparam name="RenderTexture"></typeparam>
  /// <returns></returns>
  public static void BlitToTexCropped(this Texture sourceTex, Texture2D texOut, Material mat = null, bool mipChains = false)
  {
    var rt = sourceTex.GetTemporaryRT();
    rt.name = "TextureExt::BlitToTexCropped::rt";

    var cachedRT = RenderTexture.active;
    RenderTexture.active = rt;
    if (mat != null)
    {
      Graphics.Blit(sourceTex, rt, mat);
    }
    else
    {
      Graphics.Blit(sourceTex, rt);
    }

    int w = texOut.width;
    int h = texOut.height;
    int x = (sourceTex.width >> 1) - (w >> 1);
    int y = (sourceTex.height >> 1) - (h >> 1);
    texOut.ReadPixels(new Rect(x, y, w, h), 0, 0, false);
    texOut.Apply(mipChains);

    // Cleanup
    RenderTexture.active = cachedRT;
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
  public static void BlitToTexCentre(this Texture sourceTex, Texture2D texOut, Material mat = null, bool mipChains = false)
  {
    // int sourceWidth = sourceTex.width;
    // int sourceHeight = sourceTex.height;

    var rt = sourceTex.GetTemporaryRT();
    rt.name = "TextureExt::BlitToTexCentre::rt";
    var cachedRT = RenderTexture.active;

    RenderTexture.active = rt;
    GL.Clear(true, true, Color.black);

    // Blit the entire source texture to the render texture
    if (mat != null)
    {
      Graphics.Blit(sourceTex, rt, mat);
    }
    else
    {
      Graphics.Blit(sourceTex, rt);
    }

    float scaleX = (float)texOut.width / sourceTex.width;
    float scaleY = (float)texOut.height / sourceTex.height;
    float minScale = Mathf.Min(scaleX, scaleY); // Ensures the  source texture fits entirely within texOut

    // // Calculate offsets to center the scaled source texture within texOut
    // float offsetX = (texOut.width - (sourceTex.width * minScale)) * 0.5f;
    // float offsetY = (texOut.height - (sourceTex.height * minScale)) * 0.5f;

    var texRT = texOut.GetTemporaryRT();
    texRT.name = "TextureExt::BlitToTexCentre::texRT";
    Graphics.Blit(rt, texRT);

    RenderTexture.active = texRT;
    texOut.ReadPixels(new Rect(0, 0, sourceTex.width * minScale, sourceTex.height * minScale), 0, 0, false);
    texOut.Apply(mipChains);

    // Clean up
    RenderTexture.active = cachedRT;
    RenderTexture.ReleaseTemporary(rt);
    RenderTexture.ReleaseTemporary(texRT);
  }

  private static Material _BlitCroppedMaterial;
  private static Material BlitCroppedMaterial
  {
    get
    {
      if (_BlitCroppedMaterial == null)
      {
        var shader = Shader.Find("Hidden/BlitCropped");
        if (shader == null)
        {
          Debug.LogError("Failed to find TileRotate shader. Make sure it's included in the project and the name is correct.");
        }
        else
        {
          _BlitCroppedMaterial = new Material(shader);
        }
      }
      return _BlitCroppedMaterial;
    }
  }

  public static void BlitToTexCentreFitted(this Texture2D source, Texture2D destination, Material mat = null, bool fitInside = true)
  {
    var currentRT = RenderTexture.active;

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
    RenderTexture tempRT = destination.GetTemporaryRT();
    tempRT.name = "TextureExt::BlitToTexCentreFitted::tempRT";
    RenderTexture tempRTBlitMat = destination.GetTemporaryRT();
    tempRTBlitMat.name = "TextureExt::BlitToTexCentreFitted::tempRTBlitMat";

    // Set the active RenderTexture
    RenderTexture.active = tempRT;
    // Clear the RenderTexture
    GL.Clear(true, true, Color.clear);

    // Set up the material and pass the scale and offset
    var mainTex = BlitCroppedMaterial.GetTexture("_MainTex");
    var cropRect = BlitCroppedMaterial.GetVector("_CropRect");
    BlitCroppedMaterial.SetTexture("_MainTex", source);
    BlitCroppedMaterial.SetVector("_CropRect", new Vector4(offsetX, offsetY, scaleWidth, scaleHeight));

    // Blit the texture
    Graphics.Blit(source, tempRT, BlitCroppedMaterial);
    if (mat != null)
    {
      Graphics.Blit(tempRT, tempRTBlitMat, mat);
      RenderTexture.active = tempRTBlitMat;
    }

    // Copy the result to the destination texture
    destination.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
    destination.Apply();

    // Release the temporary RenderTexture
    RenderTexture.active = currentRT;
    RenderTexture.ReleaseTemporary(tempRT);
    RenderTexture.ReleaseTemporary(tempRTBlitMat);

    // Cleanup
    BlitCroppedMaterial.SetTexture("_MainTex", mainTex);
    BlitCroppedMaterial.SetVector("_CropRect", cropRect);
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
    var rt = texOut.GetTemporaryRT();
    rt.name = "TextureExt::BlitToTex::rt";
    var cachedRT = RenderTexture.active;

    RenderTexture.active = rt;
    if (mat != null)
    {
      Graphics.Blit(sourceTex, rt, mat);
    }
    else
    {
      Graphics.Blit(sourceTex, rt);
    }

    // @TODO: Optimize
    // Graphics.CopyTexture(rt, 0, 0, texOut, 0, 0);
    texOut.ReadPixels(new Rect(0, 0, texOut.width, texOut.height), 0, 0, false);
    texOut.Apply(mipChains);

    // Cleanup
    RenderTexture.active = cachedRT;
    RenderTexture.ReleaseTemporary(rt);
  }

  /// <summary>
  /// blit sourceTex to texOut
  /// </summary>
  /// <param name="sourceTex"></param>
  /// <param name="texOut"></param>
  public static void BlitToRT(this Texture sourceTex, RenderTexture rtOut, Material mat = null, bool mipChains = false)
  {
    rtOut.name = "TextureExt::BlitToRT::rtOut";
    var cachedRT = RenderTexture.active;

    RenderTexture.active = rtOut;
    if (mat != null)
    {
      Graphics.Blit(sourceTex, rtOut, mat);
    }
    else
    {
      Graphics.Blit(sourceTex, rtOut);
    }

    // Cleanup
    RenderTexture.active = cachedRT;
  }


  public static void BlitToTex(this RenderTexture sourceTex, Texture2D texOut, Material[] mats, bool mipChains = false)
  {
    sourceTex.BlitMaterials(mats);
    // RenderTexture.active = sourceTex;
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