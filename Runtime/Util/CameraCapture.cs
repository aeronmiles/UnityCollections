using UnityEngine;
using System;
#if UNITY_IOS && !UNITY_EDITOR
using System.Globalization;
using System.Runtime.InteropServices;
#endif

public class CameraCapture : MonoBehaviour
{

#if UNITY_IOS && !UNITY_EDITOR
  [DllImport("__Internal")]
  private static extern void UnityBridge_setup();
  [DllImport("__Internal")]
  private static extern void _InitializeCamera(string gameObjectName);
  [DllImport("__Internal")]
  private static extern void _StartPreview();
  [DllImport("__Internal")]
  private static extern void _PausePreview();
  [DllImport("__Internal")]
  private static extern void _ResumePreview();
  [DllImport("__Internal")]
  private static extern void _TakePhoto();
  [DllImport("__Internal")]
  private static extern void _FreePhotoData(IntPtr pointer);
  [DllImport("__Internal")]
  private static extern void _SwitchCamera();
  [DllImport("__Internal")]
  private static extern void _SetColorTemperature(float temperature);
  [DllImport("__Internal")]
  private static extern void _StopCamera();
  [DllImport("__Internal")]
  private static extern IntPtr _GetCameraOrientation();
#endif

  public delegate void PhotoCaptureCallback(Texture2D photoData, float rotation, Vector3 scale, bool isMirrored);
  public event PhotoCaptureCallback OnPhotoCaptured;

  public delegate void PreviewFrameCallback(Texture2D previewTexture, float rotation, Vector3 scale, bool isMirrored);
  public event PreviewFrameCallback OnPreviewTextureUpdated;

  private Texture2D previewTexture;
  private object textureLock = new object();

  public bool IsCameraActive { get; private set; }
  public bool IsPreviewPaused { get; private set; }

  private void Start()
  {
#if UNITY_IOS && !UNITY_EDITOR
    UnityBridge_setup();
    _InitializeCamera(gameObject.name);
#else
    Debug.LogWarning("CameraCapture :: Camera capture is only supported on iOS devices.");
#endif
  }

  private void OnDisable()
  {
#if UNITY_IOS && !UNITY_EDITOR
    _StopCamera();
#endif
  }

  public void StartPreview()
  {
#if UNITY_IOS && !UNITY_EDITOR
    if (!IsCameraActive)
    {
      _InitializeCamera(gameObject.name);
    }
    _StartPreview();
#else
    Debug.LogWarning("CameraCapture :: Camera preview is only supported on iOS devices.");
#endif
  }

  public void PausePreview()
  {
#if UNITY_IOS && !UNITY_EDITOR
    if (IsCameraActive && !IsPreviewPaused)
    {
      _PausePreview();
    }
#else
    Debug.LogWarning("CameraCapture :: Camera preview is only supported on iOS devices.");
#endif
  }

  public void ResumePreview()
  {
#if UNITY_IOS && !UNITY_EDITOR
    if (!IsCameraActive)
    {
      _InitializeCamera(gameObject.name);
    }
    
    if (IsPreviewPaused)
    {
      _ResumePreview();
    }
#else
    Debug.LogWarning("CameraCapture :: Camera preview is only supported on iOS devices.");
#endif
  }

  public void StopCamera()
  {
#if UNITY_IOS && !UNITY_EDITOR
    if (IsCameraActive)
    {
      _StopCamera();
    }
#else
    Debug.LogWarning("CameraCapture :: Camera stopping is only supported on iOS devices.");
#endif
  }

  public void GetCameraOrientation()
  {
#if UNITY_IOS && !UNITY_EDITOR
    IntPtr ptr = _GetCameraOrientation();
    string result = Marshal.PtrToStringAnsi(ptr);
    Debug.Log("Camera Orientation and Mirrored: " + result);
    Marshal.FreeHGlobal(ptr);
#endif
  }

  public void TakePhoto()
  {
#if UNITY_IOS && !UNITY_EDITOR
    _TakePhoto();
#else
    Debug.LogWarning("CameraCapture :: Photo capture is only supported on iOS devices.");
#endif
  }

  public void SwitchCamera()
  {
#if UNITY_IOS && !UNITY_EDITOR
    _SwitchCamera();
#else
    Debug.LogWarning("CameraCapture :: Camera switching is only supported on iOS devices.");
#endif
  }

  public void SetColorTemperature(float temperature)
  {
#if UNITY_IOS && !UNITY_EDITOR
    _SetColorTemperature(temperature);
#else
    Debug.LogWarning("CameraCapture :: Color temperature adjustment is only supported on iOS devices.");
#endif
  }

  [AOT.MonoPInvokeCallback(typeof(Action<string>))]
  private void OnCameraInitialized(string _) => IsCameraActive = true;

  [AOT.MonoPInvokeCallback(typeof(Action<string>))]
  private void OnCameraStopped(string _) => IsCameraActive = false;

  [AOT.MonoPInvokeCallback(typeof(Action<string>))]
  private void OnPreviewPaused(string _) => IsPreviewPaused = true;

  [AOT.MonoPInvokeCallback(typeof(Action<string>))]
  private void OnPreviewResumed(string _) => IsPreviewPaused = false;

  [AOT.MonoPInvokeCallback(typeof(Action<string>))]
  private void OnPhotoTaken(string pointerData)
  {
#if UNITY_IOS && !UNITY_EDITOR
    try
    {
      Debug.Log($"CameraCapture :: Received photo data: {pointerData}");

      string[] parts = pointerData.Split(',');
      if (parts.Length != 5)
      {
        Debug.LogError($"CameraCapture :: Invalid photo data received. Expected 5 parts, got {parts.Length}");
        return;
      }

      if (!ulong.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var pointerValue))
      {
        Debug.LogError($"CameraCapture :: Failed to parse photo pointer value: {parts[0]}");
        return;
      }
      IntPtr baseAddress = new IntPtr((long)pointerValue);

      if (!int.TryParse(parts[1], out var dataLength))
      {
        Debug.LogError($"CameraCapture :: Failed to parse photo data length: {parts[1]}");
        return;
      }

      AVFoundation.AVCaptureVideoOrientation videoOrientation = default;
      if (!int.TryParse(parts[2], out var videoOrientationInt))
      {
        Debug.LogError($"CameraCapture :: Failed to parse video orientation: {parts[2]}");
        return;
      }
      videoOrientation = (AVFoundation.AVCaptureVideoOrientation)videoOrientationInt;

      UIImage.Orientation imageOrientation = default;
      if (!int.TryParse(parts[3], out var imageOrientationInt))
      {
        Debug.LogError($"CameraCapture :: Failed to parse image orientation: {parts[3]}");
        return;
      }
      imageOrientation = (UIImage.Orientation)imageOrientationInt;

      if (!bool.TryParse(parts[4], out var isMirrored))
      {
        Debug.LogError($"CameraCapture :: Failed to parse isMirrored: {parts[4]}");
      }

      Debug.Log($"CameraCapture :: Parsed values - Pointer: {baseAddress.ToInt64():X}, DataLength: {dataLength}, ImageOrientation: {imageOrientation} IsMirrored: {isMirrored}");

      byte[] photoBytes = new byte[dataLength];
      Marshal.Copy(baseAddress, photoBytes, 0, dataLength);

      UnityMainThreadDispatcher.I.Enqueue(() =>
      {
        Texture2D photoTexture = new Texture2D(2, 2);
        photoTexture.LoadImage(photoBytes);

        var rotScale = RotationAngleScale(videoOrientation, imageOrientation, isMirrored);
        OnPhotoCaptured?.Invoke(photoTexture, rotScale.Item1, rotScale.Item2, isMirrored);
        _FreePhotoData(baseAddress);
      });
    }
    catch (Exception e)
    {
      Debug.LogError($"CameraCapture :: Error processing photo data: {e.Message}\nStack Trace: {e.StackTrace}");
    }
#endif
  }

  [AOT.MonoPInvokeCallback(typeof(Action<string>))]
  private void OnPreviewFrameReceived(string pointerData)
  {
#if UNITY_IOS && !UNITY_EDITOR
    try
    {
      // Debug.Log($"CameraCapture :: Received preview frame data: {pointerData}");

      string[] parts = pointerData.Split(',');
      if (parts.Length != 8)
      {
        Debug.LogError($"CameraCapture :: Invalid preview frame data received. Expected 8 parts, got {parts.Length}");
        return;
      }

      if (!ulong.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var pointerValue))
      {
        Debug.LogError($"CameraCapture :: Failed to parse preview frame pointer value: {parts[0]}");
        return;
      }
      IntPtr baseAddress = new IntPtr((long)pointerValue);

      if (!int.TryParse(parts[1], out var width) ||
          !int.TryParse(parts[2], out var height) ||
          !int.TryParse(parts[3], out var bytesPerRow) ||
          !int.TryParse(parts[4], out var dataLength))
      {
        Debug.LogError($"CameraCapture :: Failed to parse preview frame dimensions or data length: {parts[1]}, {parts[2]}, {parts[3]}, {parts[4]}");
        return;
      }

      if (!int.TryParse(parts[5], out var videoOrientationValue))
      {
        Debug.LogError($"CameraCapture :: Failed to parse video orientation: {parts[5]}");
        return;
      }
      AVFoundation.AVCaptureVideoOrientation videoOrientation = (AVFoundation.AVCaptureVideoOrientation)videoOrientationValue;

      if (!int.TryParse(parts[6], out var imageOrientationValue))
      {
        Debug.LogError($"CameraCapture :: Failed to parse image orientation: {parts[6]}");
        return;
      }
      UIImage.Orientation imageOrientation = (UIImage.Orientation)imageOrientationValue;

      if (!bool.TryParse(parts[7], out var isMirrored))
      {
        Debug.LogError($"CameraCapture :: Failed to parse isMirrored: {parts[4]}");
      }

      // Debug.Log($"CameraCapture :: Parsed preview frame values - Pointer: {baseAddress.ToInt64():X}, Width: {width}, Height: {height}, BytesPerRow: {bytesPerRow}, DataLength: {dataLength}, VideoOrientation: {videoOrientation}, ImageOrientation: {imageOrientation}, IsMirrored: {isMirrored}");

      byte[] frameData = new byte[dataLength];
      Marshal.Copy(baseAddress, frameData, 0, dataLength);

      UnityMainThreadDispatcher.I.Enqueue(() =>
      {
        UpdatePreviewTexture(frameData, width, height, videoOrientation, imageOrientation, isMirrored);
      });
    }
    catch (Exception e)
    {
      Debug.LogError($"CameraCapture :: Error processing preview frame data: {e.Message}\nStack Trace: {e.StackTrace}");
    }
#endif
  }

  private void UpdatePreviewTexture(byte[] frameData, int width, int height, AVFoundation.AVCaptureVideoOrientation videoOrientation, UIImage.Orientation imageOrientation, bool isMirrored)
  {
    lock (textureLock)
    {
      if (previewTexture == null || previewTexture.width != width || previewTexture.height != height)
      {
        if (previewTexture != null)
        {
          Destroy(previewTexture);
        }
        previewTexture = new Texture2D(width, height, TextureFormat.BGRA32, false);
      }

      previewTexture.LoadRawTextureData(frameData);
      previewTexture.Apply();

      var rotScale = RotationAngleScale(videoOrientation, imageOrientation, isMirrored);
      OnPreviewTextureUpdated?.Invoke(previewTexture, rotScale.Item1, rotScale.Item2, isMirrored);
    }
  }

  private (float, Vector3) RotationAngleScale(AVFoundation.AVCaptureVideoOrientation videoOrientation, UIImage.Orientation imageOrientation, bool isMirrored)
  {
    int rotationAngle = 0;
    Vector3 scale = Vector3.one;

    DeviceOrientation orientation = Input.deviceOrientation;
    if (Screen.orientation != ScreenOrientation.AutoRotation)
    {
      if (Screen.orientation == ScreenOrientation.Portrait)
      {
        orientation = DeviceOrientation.Portrait;
      }
      else if (Screen.orientation == ScreenOrientation.PortraitUpsideDown)
      {
        orientation = DeviceOrientation.PortraitUpsideDown;
      }
      else if (Screen.orientation == ScreenOrientation.LandscapeLeft)
      {
        orientation = DeviceOrientation.LandscapeLeft;
      }
      else if (Screen.orientation == ScreenOrientation.LandscapeRight)
      {
        orientation = DeviceOrientation.LandscapeRight;
      }
    }

    // Determine rotation based on device orientation and video orientation
    switch (orientation)
    {
      case DeviceOrientation.Portrait:
        if (videoOrientation == AVFoundation.AVCaptureVideoOrientation.LandscapeRight)
          rotationAngle = -90;
        else if (videoOrientation == AVFoundation.AVCaptureVideoOrientation.LandscapeLeft)
          rotationAngle = 90;
        else if (videoOrientation == AVFoundation.AVCaptureVideoOrientation.PortraitUpsideDown)
          rotationAngle = 180;
        break;
      case DeviceOrientation.PortraitUpsideDown:
        if (videoOrientation == AVFoundation.AVCaptureVideoOrientation.LandscapeRight)
          rotationAngle = 90;
        else if (videoOrientation == AVFoundation.AVCaptureVideoOrientation.LandscapeLeft)
          rotationAngle = -90;
        else if (videoOrientation == AVFoundation.AVCaptureVideoOrientation.Portrait)
          rotationAngle = 180;
        break;
      case DeviceOrientation.LandscapeLeft:
        if (videoOrientation == AVFoundation.AVCaptureVideoOrientation.Portrait)
          rotationAngle = -90;
        else if (videoOrientation == AVFoundation.AVCaptureVideoOrientation.PortraitUpsideDown)
          rotationAngle = 90;
        else if (videoOrientation == AVFoundation.AVCaptureVideoOrientation.LandscapeRight)
          rotationAngle = 180;
        break;
      case DeviceOrientation.LandscapeRight:
        if (videoOrientation == AVFoundation.AVCaptureVideoOrientation.Portrait)
          rotationAngle = 90;
        else if (videoOrientation == AVFoundation.AVCaptureVideoOrientation.PortraitUpsideDown)
          rotationAngle = -90;
        else if (videoOrientation == AVFoundation.AVCaptureVideoOrientation.LandscapeLeft)
          rotationAngle = 180;
        break;
    }

    bool isVerticalMirrored = imageOrientation == UIImage.Orientation.UpMirrored || imageOrientation == UIImage.Orientation.DownMirrored;

    // @TODO: test on different iOS devices, add mirrored variants
    if (rotationAngle == 0 || rotationAngle == 180)
    {
      // Mirroring is inverted ??
      scale.x = isMirrored ? 1f : -1f;
      scale.y = isVerticalMirrored ? -1f : 1f;
    }
    else
    {
      // Mirroring is inverted ??
      scale.y = isMirrored ? 1f : -1f;
      scale.x = isVerticalMirrored ? -1f : 1f;
    }

#if DEBUG
    // Debug.Log($"Image deviceOr: {Input.deviceOrientation} videoOr: {videoOrientation} imgOr: {imageOrientation}rotationAngle: {rotationAngle} scale: {scale}");
#endif
    return (rotationAngle, scale);
  }
}