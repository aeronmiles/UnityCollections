using UnityEngine;
using System;
#if UNITY_IOS
using System.Runtime.InteropServices;
using System.Globalization;
#endif

public class CameraCapture : MonoBehaviour
{
  public enum UIInterfaceOrientation
  {
    Unknown = 0,
    Portrait = 1,
    PortraitUpsideDown = 2,
    LandscapeLeft = 3,
    LandscapeRight = 4
  }
#if UNITY_IOS
  [DllImport("__Internal")]
  private static extern void UnityBridge_setup();
  [DllImport("__Internal")]
  private static extern void _InitializeCamera(string gameObjectName);
  [DllImport("__Internal")]
  private static extern void _StartPreview();
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
  private static extern void _UpdateOrientation(int orientation);
  [DllImport("__Internal")]
  private static extern IntPtr _GetCameraOrientationAndMirrored();
#endif

  public delegate void PhotoCaptureCallback(byte[] photoData);
  public event PhotoCaptureCallback OnPhotoCaptured;

  public delegate void PreviewFrameCallback(Texture2D previewTexture);
  public event PreviewFrameCallback OnPreviewTextureUpdated;

  private Texture2D previewTexture;
  private object textureLock = new object();

  private void Start()
  {
#if UNITY_IOS
    UnityBridge_setup();
    _InitializeCamera(gameObject.name);
#else
    Debug.LogWarning("CameraCapture :: Camera capture is only supported on iOS devices.");
#endif
  }

#if UNITY_IOS
  private void Update()
  {
    UpdateOrientation();
    GetCameraOrientationAndMirrored();
  }

  private void UpdateOrientation()
  {
    if (Input.deviceOrientation == DeviceOrientation.LandscapeLeft)
    {
      _UpdateOrientation((int)UIInterfaceOrientation.LandscapeLeft);
    }
    else if (Input.deviceOrientation == DeviceOrientation.LandscapeRight)
    {
      _UpdateOrientation((int)UIInterfaceOrientation.LandscapeRight);
    }
    else if (Input.deviceOrientation == DeviceOrientation.Portrait)
    {
      _UpdateOrientation((int)UIInterfaceOrientation.Portrait);
    }
    else if (Input.deviceOrientation == DeviceOrientation.PortraitUpsideDown)
    {
      _UpdateOrientation((int)UIInterfaceOrientation.PortraitUpsideDown);
    }
  }
#endif

  private void OnDisable()
  {
#if UNITY_IOS
    _StopCamera();
#endif
  }

  public void StartPreview()
  {
#if UNITY_IOS
    _StartPreview();
#else
    Debug.LogWarning("CameraCapture :: Camera preview is only supported on iOS devices.");
#endif
  }

  public void GetCameraOrientationAndMirrored()
  {
#if UNITY_IOS
    IntPtr ptr = _GetCameraOrientationAndMirrored();
    string result = Marshal.PtrToStringAnsi(ptr);
    Debug.Log("Camera Orientation and Mirrored: " + result);
    Marshal.FreeHGlobal(ptr);
#endif
  }

  public void TakePhoto()
  {
#if UNITY_IOS
    _TakePhoto();
#else
    Debug.LogWarning("CameraCapture :: Photo capture is only supported on iOS devices.");
#endif
  }

  public void SwitchCamera()
  {
#if UNITY_IOS
    _SwitchCamera();
#else
    Debug.LogWarning("CameraCapture :: Camera switching is only supported on iOS devices.");
#endif
  }

  public void SetColorTemperature(float temperature)
  {
#if UNITY_IOS
    _SetColorTemperature(temperature);
#else
    Debug.LogWarning("CameraCapture :: Color temperature adjustment is only supported on iOS devices.");
#endif
  }

  [AOT.MonoPInvokeCallback(typeof(Action<string>))]
  private void OnPhotoTaken(string pointerData)
  {
#if UNITY_IOS
    try
    {
      Debug.Log($"CameraCapture :: Received photo data: {pointerData}");

      string[] parts = pointerData.Split(',');
      if (parts.Length != 2)
      {
        Debug.LogError($"CameraCapture :: Invalid photo data received. Expected 2 parts, got {parts.Length}");
        return;
      }

      ulong pointerValue;
      if (!ulong.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out pointerValue))
      {
        Debug.LogError($"CameraCapture :: Failed to parse photo pointer value: {parts[0]}");
        return;
      }
      IntPtr baseAddress = new IntPtr((long)pointerValue);

      int dataLength;
      if (!int.TryParse(parts[1], out dataLength))
      {
        Debug.LogError($"CameraCapture :: Failed to parse photo data length: {parts[1]}");
        return;
      }

      Debug.Log($"CameraCapture :: Parsed values - Pointer: {baseAddress.ToInt64():X}, DataLength: {dataLength}");

      byte[] photoBytes = new byte[dataLength];
      Marshal.Copy(baseAddress, photoBytes, 0, dataLength);

      Debug.Log($"CameraCapture :: Photo taken, size: {photoBytes.Length} bytes");

      UnityMainThreadDispatcher.I.Enqueue(() =>
      {
        OnPhotoCaptured?.Invoke(photoBytes);
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
#if UNITY_IOS
    try
    {
      // Debug.Log($"CameraCapture :: Received pointer data: {pointerData}");

      string[] parts = pointerData.Split(',');
      if (parts.Length != 5)
      {
        Debug.LogError($"CameraCapture :: Invalid pointer data received. Expected 5 parts, got {parts.Length}");
        return;
      }

      // Debug.Log($"CameraCapture :: Parsing pointer: {parts[0]}");
      ulong pointerValue;
      if (!ulong.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out pointerValue))
      {
        Debug.LogError($"CameraCapture :: Failed to parse pointer value: {parts[0]}");
        return;
      }
      IntPtr baseAddress = new IntPtr((long)pointerValue);

      // Debug.Log($"CameraCapture :: Parsing dimensions: {parts[1]}, {parts[2]}");
      if (!int.TryParse(parts[1], out int width) || !int.TryParse(parts[2], out int height))
      {
        Debug.LogError($"CameraCapture :: Failed to parse width or height: {parts[1]}, {parts[2]}");
        return;
      }

      // Debug.Log($"CameraCapture :: Parsing bytesPerRow and dataLength: {parts[3]}, {parts[4]}");
      if (!int.TryParse(parts[3], out int bytesPerRow) || !int.TryParse(parts[4], out int dataLength))
      {
        Debug.LogError($"CameraCapture :: Failed to parse bytesPerRow or dataLength: {parts[3]}, {parts[4]}");
        return;
      }

      // Debug.Log($"CameraCapture :: Parsed values - Pointer: {baseAddress.ToInt64():X}, Width: {width}, Height: {height}, BytesPerRow: {bytesPerRow}, DataLength: {dataLength}");

      byte[] frameData = new byte[dataLength];
      Marshal.Copy(baseAddress, frameData, 0, dataLength);

      UpdateTexture(frameData, width, height);
    }
    catch (Exception e)
    {
      Debug.LogError($"CameraCapture :: Error processing frame data: {e.Message}\nStack Trace: {e.StackTrace}");
    }
#endif
  }

  private void UpdateTexture(byte[] frameData, int width, int height)
  {
#if UNITY_IOS
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

      UnityMainThreadDispatcher.I.Enqueue(() => OnPreviewTextureUpdated?.Invoke(previewTexture));
    }
#endif
  }

}