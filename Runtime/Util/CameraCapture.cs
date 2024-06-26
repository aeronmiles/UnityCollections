using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class CameraCapture : MonoBehaviour
{
#if UNITY_IOS
  [DllImport("__Internal")]
  private static extern void _InitializeCamera(string gameObjectName);

  [DllImport("__Internal")]
  private static extern void _StartPreview();

  [DllImport("__Internal")]
  private static extern void _TakePhoto();

  [DllImport("__Internal")]
  private static extern void _SwitchCamera();

  [DllImport("__Internal")]
  private static extern void _SetColorTemperature(float temperature);

  [DllImport("__Internal")]
  private static extern void _StopCamera();

  public delegate void PhotoCaptureCallback(byte[] photoData);
  public event PhotoCaptureCallback OnPhotoCaptured;

  public delegate void PreviewFrameCallback(Texture2D previewTexture);
  public event PreviewFrameCallback OnPreviewTextureFrame;

  private Texture2D previewTexture;
  private Texture2D _temporaryTexture;

  void Start()
  {
    if (Application.platform == RuntimePlatform.IPhonePlayer)
    {
      _InitializeCamera(gameObject.name);
    }
    else
    {
      Debug.LogWarning("Camera capture is only supported on iOS devices.");
    }
  }

  void OnDisable()
  {
    if (Application.platform == RuntimePlatform.IPhonePlayer)
    {
      _StopCamera();
    }
  }

  public void StartPreview()
  {
    if (Application.platform == RuntimePlatform.IPhonePlayer)
    {
      _StartPreview();
    }
    else
    {
      Debug.LogWarning("Camera preview is only supported on iOS devices.");
    }
  }

  public void TakePhoto()
  {
    if (Application.platform == RuntimePlatform.IPhonePlayer)
    {
      _TakePhoto();
    }
    else
    {
      Debug.LogWarning("Photo capture is only supported on iOS devices.");
    }
  }

  public void SwitchCamera()
  {
    if (Application.platform == RuntimePlatform.IPhonePlayer)
    {
      _SwitchCamera();
    }
    else
    {
      Debug.LogWarning("Camera switching is only supported on iOS devices.");
    }
  }

  public void SetColorTemperature(float temperature)
  {
    if (Application.platform == RuntimePlatform.IPhonePlayer)
    {
      _SetColorTemperature(temperature);
    }
    else
    {
      Debug.LogWarning("Color temperature adjustment is only supported on iOS devices.");
    }
  }

  [AOT.MonoPInvokeCallback(typeof(Action<string>))]
  private void OnPhotoTaken(string encodedPhoto)
  {
    try
    {
      byte[] photoBytes = Convert.FromBase64String(encodedPhoto);
      Debug.Log($"Photo taken, size: {photoBytes.Length} bytes");

      OnPhotoCaptured?.Invoke(photoBytes);
    }
    catch (Exception e)
    {
      Debug.LogError($"Error processing photo: {e.Message}");
    }
  }

  [AOT.MonoPInvokeCallback(typeof(Action<string>))]
  private void OnPreviewFrameReceived(string message)
  {
    string[] parts = message.Split('|');
    if (parts.Length != 2) return;

    string[] dimensions = parts[0].Split(',');
    if (dimensions.Length != 3) return;

    int width = int.Parse(dimensions[0]);
    int height = int.Parse(dimensions[1]);
    int bytesPerRow = int.Parse(dimensions[2]);

    byte[] frameData = Convert.FromBase64String(parts[1]);

    if (previewTexture == null || previewTexture.width != width || previewTexture.height != height)
    {
      if (previewTexture != null) Destroy(previewTexture);
      previewTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
    }

    if (_temporaryTexture == null || _temporaryTexture.width != width || _temporaryTexture.height != height)
    {
      if (_temporaryTexture != null) Destroy(_temporaryTexture);
      _temporaryTexture = new Texture2D(width, height, TextureFormat.BGRA32, false);
    }

    _temporaryTexture.LoadRawTextureData(frameData);
    _temporaryTexture.Apply();

    Graphics.ConvertTexture(_temporaryTexture, previewTexture);

    OnPreviewTextureFrame?.Invoke(previewTexture);
  }
#endif
}