using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public static class CameraExt
{
  /// <summary>
  /// Blits a cropped area of the render camera's view to a target RenderTexture.
  /// </summary>
  /// <param name="renderCamera">The camera to render from.</param>
  /// <param name="rtOut">The output RenderTexture.</param>
  /// <param name="targetRenderer">The renderer used to determine the area to crop.</param>
  /// <param name="blitMat">Optional material used for blitting.</param>
  /// <param name="padding">Padding added around the cropped area.</param>
  public static bool BlitCroppedToTarget(this Camera camera, ref RenderTexture rtOut, Renderer targetRenderer, Material blitMat = null, int padding = 32)
  {
    if (camera == null || rtOut == null || targetRenderer == null)
    {
      if (camera == null)
        Debug.LogError("BlitCroppedToTarget: Camera is null.");
      if (rtOut == null)
        Debug.LogError("BlitCroppedToTarget: RenderTexture is null.");
      if (targetRenderer == null)
        Debug.LogError("BlitCroppedToTarget: Target renderer is null.");
      return false;
    }

    // Calculate screen bounds with padding
    var bounds = camera.ScreenSpaceBounds(targetRenderer, padding);

    if (!bounds.InScreenNonZero())
    {
      return false;
    }

    var cachedTargetTexture = camera.targetTexture;
    var projectionMatrix = camera.projectionMatrix;

    camera.projectionMatrix = camera.CroppedProjectionMatrix(bounds);

    camera.targetTexture = rtOut;
    camera.RenderDontRestore();
    rtOut.BlitMaterial(blitMat);

    camera.targetTexture = cachedTargetTexture;
    camera.projectionMatrix = projectionMatrix;

    return true;
  }

  /// <summary>
  /// Blits a cropped area of the camera's view to a new Texture2D, scaled to screen bounds with padding.
  /// </summary>
  /// <param name="camera">The camera to render from.</param>
  /// <param name="targetRenderer">The renderer used to determine the area to crop.</param>
  /// <param name="blitMat">Optional material used for blitting.</param>
  /// <param name="size">The size of the output texture.</param>
  /// <param name="padding">Padding added around the cropped area.</param>
  /// <param name="filterMode">The filter mode for the texture.</param>
  /// <returns>A Texture2D containing the cropped area.</returns>
  public static bool BlitCroppedToScreenBounds(this Camera camera, ref Texture2D texOut, Renderer targetRenderer, int width, int height, Material blitMat = null, int size = 128, int padding = 32, FilterMode filterMode = FilterMode.Bilinear, bool mipChains = false)
  {
    if (camera == null || targetRenderer == null || size <= 0)
    {
      if (camera == null)
        Debug.LogError("BlitCroppedToScreenBounds: Camera is null.");
      if (targetRenderer == null)
        Debug.LogError("BlitCroppedToScreenBounds: Target renderer is null.");
      if (size <= 0)
        Debug.LogError("BlitCroppedToScreenBounds: Invalid size. Ensure the size is greater than 0.");

      texOut = null;
      return false;
    }

    // Calculate screen bounds with padding
    var bounds = camera.ScreenSpaceBounds(targetRenderer, padding).ToRect();

    if (!bounds.InScreenNonZero())
    {
      texOut = null;
      return false;
    }
    Debug.Log(bounds);

    var cachedTargetTexture = camera.targetTexture;

    var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGBHalf);
    rt.filterMode = filterMode;

    // Render to texture
    camera.targetTexture = rt;
    camera.RenderDontRestore();

    // @TODO: optimize - remove memalloc
    Debug.LogWarning("BlitCroppedToScreenBounds: Memory allocation detected. Consider using a pooled texture.");
    var tex = new Texture2D((int)bounds.width, (int)bounds.height, TextureFormat.RGBAHalf, false)
    {
      filterMode = filterMode
    };

    RenderTexture.active = rt;
    tex.ReadPixels(bounds, 0, 0);
    tex.Apply(mipChains);

    var rt2 = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGBHalf);
    rt2.filterMode = filterMode;

    if (blitMat != null)
    {
      Graphics.Blit(tex, rt2, blitMat);
    }
    else
    {
      Graphics.Blit(tex, rt2);
    }

    rt2.BlitToTex(texOut);

    // Cleanup
    camera.targetTexture = cachedTargetTexture;
    RenderTexture.ReleaseTemporary(rt);
    RenderTexture.ReleaseTemporary(rt2);

    return true;
  }

  // /// <summary>
  // /// Blits a cropped area of the camera's view to a RenderTexture, scaled to screen bounds with padding.
  // /// </summary>
  // /// <param name="camera">The camera used for the calculation.</param>
  // /// <param name="targetRenderer">The renderer of the object to calculate bounds for.</param>
  // /// <param name="padding">Optional padding added to the bounds. Default is -1, which means no padding.</param>
  // /// <returns>The screen-space bounds of the object as seen by the specified camera.</returns>
  // public static bool BlitCroppedToScreenBounds(this Camera camera, ref RenderTexture rtOut, Renderer targetRenderer, Material[] blitMats = null, int padding = 12, bool linear = true)
  // {
  //   if (camera == null || targetRenderer == null)
  //   {
  //     if (camera == null)
  //       Debug.LogError("BlitCroppedToScreenBounds: Camera is null.");
  //     if (targetRenderer == null)
  //       Debug.LogError("BlitCroppedToScreenBounds: Target renderer is null.");

  //     return false;
  //   }

  //   // Calculate screen bounds with padding
  //   var bounds = camera.ScreenSpaceBounds(targetRenderer, padding).ToRect();
  //   if (!bounds.InScreenNonZero())
  //   {
  //     return false;
  //   }

  //   // store camera settings
  //   var cachedTargetTexture = camera.targetTexture;

  //   // Render to texture
  //   var rtScreen = RenderTexture.GetTemporary(width, height, 0, rtOut.format);

  //   camera.targetTexture = rtScreen;
  //   camera.RenderDontRestore();

  //   var tex = new Texture2D((int)bounds.width, (int)bounds.height, rtOut.ToTextureFormat(), rtOut.useMipMap, linear)
  //   {
  //     filterMode = rtOut.filterMode
  //   };

  //   RenderTexture.active = rtScreen;
  //   tex.ReadPixels(bounds, 0, 0);
  //   tex.Apply(rtOut.useMipMap);

  //   Graphics.Blit(tex, rtOut);
  //   rtOut.BlitMaterials(blitMats);

  //   // Cleanup
  //   camera.targetTexture = cachedTargetTexture;
  //   RenderTexture.ReleaseTemporary(rtScreen);

  //   return true;
  // }

  private static Material _BlitCroppedMaterial;
  public static bool BlitCroppedToScreenBounds(this Camera camera, ref RenderTexture rtOut, Renderer targetRenderer, int width, int height, Material[] blitMats = null, int padding = 12, bool linear = true)
  {
    if (camera == null || targetRenderer == null)
    {
      if (camera == null)
        Debug.LogError("BlitCroppedToScreenBounds: Camera is null.");
      if (targetRenderer == null)
        Debug.LogError("BlitCroppedToScreenBounds: Target renderer is null.");
      return false;
    }

    // Calculate screen bounds with padding
    var bounds = camera.ScreenSpaceBounds(targetRenderer, padding).ToRect();
    if (!bounds.InScreenNonZero())
    {
      return false;
    }

    // store camera settings
    var cachedTargetTexture = camera.targetTexture;

    // Render to texture
    var rtScreen = RenderTexture.GetTemporary(width, height, 0, rtOut.format);
    camera.targetTexture = rtScreen;
    camera.RenderDontRestore();

    // Calculate normalized crop rectangle
    Rect normalizedRect = new Rect(bounds.x / width, bounds.y / height, bounds.width / width, bounds.height / height);

    // Set up the custom blit material
    if (_BlitCroppedMaterial == null)
    {
      _BlitCroppedMaterial = new Material(Shader.Find("Hidden/BlitCropped"));
    }
    _BlitCroppedMaterial.SetTexture("_MainTex", rtScreen);
    _BlitCroppedMaterial.SetVector("_CropRect", new Vector4(normalizedRect.x, normalizedRect.y, normalizedRect.width, normalizedRect.height));

    // Perform the blit with cropping
    Graphics.Blit(rtScreen, rtOut, _BlitCroppedMaterial);
    rtOut.BlitMaterials(blitMats);

    // Cleanup
    camera.targetTexture = cachedTargetTexture;
    RenderTexture.ReleaseTemporary(rtScreen);

    return true;
  }


  public static bool Blit(this Camera camera, ref RenderTexture rtOut, Material blitMat = null, bool linear = true)
  {
    if (camera == null || rtOut == null)
    {
      if (camera == null)
        Debug.LogError("Blit: Camera is null.");
      if (rtOut == null)
        Debug.LogError("Blit: RenderTexture is null.");

      return false;
    }

    var cachedTargetTexture = camera.targetTexture;

    camera.targetTexture = rtOut;
    camera.RenderDontRestore();

    rtOut.BlitMaterial(blitMat);

    camera.targetTexture = cachedTargetTexture;

    return true;
  }

  /// <summary>
  /// Renders the camera's view into a Texture2D, optionally including mipmaps. This method captures
  /// the camera's current view, renders it into a temporary RenderTexture, and then copies the pixels
  /// to the specified Texture2D. It is useful for creating dynamic textures at runtime from a camera's perspective.
  /// </summary>
  /// <param name="camera">The camera whose view is to be rendered.</param>
  /// <param name="tex">The Texture2D to which the camera's view will be blitted. This texture will be modified.</param>
  public static bool BlitToTex(this Camera camera, ref Texture2D tex)
  {
    if (camera == null || tex == null)
    {
      if (camera == null)
        Debug.LogError("BlitToTex: Camera is null.");
      if (tex == null)
        Debug.LogError("BlitToTex: Texture is null.");

      return false;
    }

    var cachedTargetTexture = camera.targetTexture;

    var renderTexture = tex.GetTemporaryRT();
    camera.targetTexture = renderTexture;

    camera.RenderDontRestore();
    RenderTexture.active = camera.targetTexture;
    tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
    tex.Apply(tex.mipmapCount > 1);

    camera.targetTexture = cachedTargetTexture;
    RenderTexture.active = null;

    // Cleanup
    RenderTexture.ReleaseTemporary(renderTexture);

    return true;
  }

  /// <summary>
  /// Calculates a projection matrix for the camera that crops its view to a specified screen-space bounding box.
  /// This method is useful for rendering a portion of the camera's view, such as focusing on a specific object or area.
  /// The resulting matrix can be used to modify the camera's projectionMatrix for custom rendering effects.
  /// </summary>
  /// <param name="camera">The camera for which the projection matrix is calculated.</param>
  /// <param name="screenSpaceBounds">The bounds within
  public static Matrix4x4 CroppedProjectionMatrix(this Camera camera, Bounds screenSpaceBounds, int width = -1, int height = -1)
  {
    if (width < 0)
    {
      width = Screen.width;
    }
    if (height < 0)
    {
      height = Screen.height;
    }
    // Calculate texture coordinates relative to the screen-space bounding box
    float left = screenSpaceBounds.min.x / width;
    float right = screenSpaceBounds.max.x / width;
    float bottom = screenSpaceBounds.min.y / height;
    float top = screenSpaceBounds.max.y / height;

    // (Adapt this if you have an off-center or oblique projection)
    return Matrix4x4.Ortho(left, right, bottom, top, camera.nearClipPlane, camera.farClipPlane);
  }

  /// <summary>
  /// Calculates the screen-space bounds of a Renderer's object, optionally expanding the bounds by a specified padding.
  /// This method converts the object's world-space bounds to screen space, allowing for calculations related to the camera's
  /// current view, such as culling or texture rendering within these bounds. Padding can be used to ensure the object's entirety
  /// is captured, including effects like shadows or glow that may extend beyond its immediate geometry.
  /// </summary>
  /// <param name="camera">The camera relative to which the screen-space bounds are calculated.</param>
  /// <param name="targetRenderer">The renderer of the object whose bounds are to be calculated.</param>
  /// <param name="padding">Optional padding to expand the bounds. Positive values expand the bounds, while negative values contract them. Default is -1, which applies no padding.</param>
  /// <returns>The screen-space bounds of the object as seen by the specified camera, optionally padded.</returns>
  public static Bounds ScreenSpaceBounds(this Camera camera, Renderer targetRenderer, int padding = -1)
  {
    NativeArray<Vector3> corners = targetRenderer.bounds.CornerPositions();

    Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
    Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

    for (int i = 0; i < 8; i++)
    {
      Vector3 screenPos = camera.WorldToScreenPoint(corners[i]);
      min.x = Mathf.Min(min.x, screenPos.x);
      min.y = Mathf.Min(min.y, screenPos.y);
      max.x = Mathf.Max(max.x, screenPos.x);
      max.y = Mathf.Max(max.y, screenPos.y);
    }

    if (padding > 0)
    {
      padding /= 2;
      min.x -= padding;
      min.y -= padding;
      max.x += padding;
      max.y += padding;
    }

    Bounds result = new Bounds();
    result.SetMinMax(min, max);
    corners.Dispose();
    return result;
  }

  /// <summary>
  /// A dictionary to store the settings of various cameras. This static member is used to stash and pop camera settings,
  /// allowing temporary changes to a camera's parameters without losing the original settings. It is particularly useful for
  /// scenarios where a camera's state needs to be modified temporarily for a specific rendering effect or operation and then
  /// restored to its previous configuration.
  /// </summary>
  private static Dictionary<Camera, CameraSettings> _CameraSettings = new Dictionary<Camera, CameraSettings>();

  /// <summary>
  /// Stashes the current settings of the specified Camera object.
  /// This method is useful for saving the camera's state before making temporary changes that you plan to revert later.
  /// </summary>
  public static void StashSettings(this Camera camera)
  {
    if (!_CameraSettings.ContainsKey(camera))
    {
      _CameraSettings[camera] = new CameraSettings();
    }

    var settings = _CameraSettings[camera];
    // Transform-Related
    settings.position = camera.transform.position;
    settings.rotation = camera.transform.rotation;

    // Projection
    settings.projectionMatrix = camera.projectionMatrix;
    settings.fieldOfView = camera.fieldOfView;
    settings.nearClipPlane = camera.nearClipPlane;
    settings.farClipPlane = camera.farClipPlane;
    settings.orthographicSize = camera.orthographicSize;
    settings.orthographic = camera.orthographic;

    // Rendering
    settings.targetTexture = camera.targetTexture;
    settings.rect = camera.rect;
    settings.clearFlags = camera.clearFlags;
    settings.backgroundColor = camera.backgroundColor;
    settings.cullingMask = camera.cullingMask;
    settings.depth = camera.depth;

    // Post-Processing
    settings.layerMask = camera.cullingMask; // Assuming you use cullingMask to manage post-processing layers

    // Other
    settings.enabled = camera.enabled;
  }

  /// <summary>
  /// Restores the previously stashed settings of the specified Camera object.
  /// This method is useful for reverting the camera's state after making temporary changes.
  /// </summary>
  public static void PopSettings(this Camera camera)
  {
    if (!_CameraSettings.ContainsKey(camera))
    {
      return;
    }

    var settings = _CameraSettings[camera];
    // Transform-Related
    camera.transform.position = settings.position;
    camera.transform.rotation = settings.rotation;

    // Projection
    camera.projectionMatrix = settings.projectionMatrix;
    camera.fieldOfView = settings.fieldOfView;
    camera.nearClipPlane = settings.nearClipPlane;
    camera.farClipPlane = settings.farClipPlane;
    camera.orthographicSize = settings.orthographicSize;
    camera.orthographic = settings.orthographic;

    // Rendering
    camera.targetTexture = settings.targetTexture;
    camera.rect = settings.rect;
    camera.clearFlags = settings.clearFlags;
    camera.backgroundColor = settings.backgroundColor;
    camera.cullingMask = settings.cullingMask;
    camera.depth = settings.depth;

    // Post-Processing
    camera.cullingMask = settings.layerMask; // Assuming you use cullingMask to manage post-processing layers

    // Other
    camera.enabled = settings.enabled;

    // Optionally, remove the camera from the dictionary if you don't plan to stash settings again
    _CameraSettings.Remove(camera);
  }
}

public class CameraSettings
{
  // Transform-Related
  public Vector3 position;
  public Quaternion rotation;

  // Projection
  public Matrix4x4 projectionMatrix;
  public float fieldOfView;
  public float nearClipPlane;
  public float farClipPlane;
  public float orthographicSize;
  public bool orthographic;

  // Rendering
  public RenderTexture targetTexture;
  public Rect rect;
  public CameraClearFlags clearFlags;
  public Color backgroundColor;
  public int cullingMask;
  public float depth; // Rendering order

  // Post-Processing
  public LayerMask layerMask; // For use with post-processing effects

  // Other
  public bool enabled;
}
