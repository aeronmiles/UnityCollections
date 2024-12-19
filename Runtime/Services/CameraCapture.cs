using UnityEngine;
using System;
using System.Threading;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Buffers;

namespace NativeCameraCapture
{
  public class CameraCapture : MonoBehaviour
  {

    [Header("Debug")]
    [SerializeField] private Texture2D _debugEditorPhoto;
    [SerializeField] private UIImage.Orientation _debugImageOrientation = UIImage.Orientation.Up;

    private ICameraService _cameraService;
    private ICameraService cameraService
    {
      get
      {
        if (_cameraService == null)
        {
#if UNITY_EDITOR
          // @TODO: Implement
          // _cameraService = new UnityEditorCameraService();
#elif UNITY_IOS
          _cameraService = new IosCameraService();
#elif UNITY_ANDROID
          // @TODO: Implement
          throw new NotImplementedException("Android camera service not implemented");
          // _cameraService = new AndroidCameraService();
#endif
        }
        return _cameraService;
      }
    }

    public event Action<Texture2D, float, Vector3> OnPhotoCaptured;
    public event Action<string> OnPhotoCapturedError;
    public event Action<Texture2D, float, Vector3> OnPreviewTextureUpdated;

    private Texture2D _photoTexture;
    private Texture2D _previewTexture;
    private readonly object _photoTexLock = new object();
    private readonly object _previewTexLock = new object();

    public bool isCameraActive { get; private set; }
    public bool isPreviewPaused { get; private set; }

    private int _isCapturing;
    public bool isCapturing => Interlocked.CompareExchange(ref _isCapturing, 1, 1) == 1;
    private bool _isApplicationQuitting;

    #region Lifecycle Methods

    private void Start()
    {
      if (!_isApplicationQuitting)
      {
        // LogMemoryUsage("Before camera initialization");
        cameraService?.InitializeCamera(gameObject.name);
        // LogMemoryUsage("After camera initialization");
      }
    }

    private void OnEnable()
    {
      Application.lowMemory += HandleLowMemory;
    }

    private void OnDisable()
    {
      Application.lowMemory -= HandleLowMemory;
      if (!_isApplicationQuitting)
      {
        StopCamera();
      }
    }

#if UNITY_EDITOR
    private void Update()
    {
      if (isCameraActive && !isPreviewPaused)
      {
        // Simulate preview frame update
        var (rotation, scale) = CalculateRotationAndScale(_debugImageOrientation);
        OnPreviewTextureUpdated?.Invoke(_debugEditorPhoto, rotation, scale);
      }
    }
#endif

    private void OnApplicationPause(bool pauseStatus)
    {
      if (pauseStatus)
      {
        PausePreview();
        CleanupResources();
      }
      else if (isPreviewPaused && isCameraActive)
      {
        ResumePreview();
      }
    }

    private void OnApplicationQuit()
    {
      _isApplicationQuitting = true;
    }

    #endregion

    #region Resource Management

    private void LogMemoryUsage(string context)
    {
#if DEBUG
      // Debug tracked / untracked memory usage
      double totalMemory = GC.GetTotalMemory(forceFullCollection: false) / 1024.0;
      double totalMemoryTracked = GC.GetTotalMemory(forceFullCollection: true) / 1024.0;
      Debug.Log($"CameraCapture :: {context} :: Memory Untracked: {totalMemory - totalMemoryTracked} kbytes, Tracked Memory: {totalMemoryTracked} bytes");
#endif
    }

    private void HandleLowMemory()
    {
      CleanupResources(force: true);
    }

    private bool IsTextureValid(Texture2D texture)
    {
      try
      {
        return texture != null &&
               texture.width > 0 &&
               texture.height > 0;
      }
      catch
      {
        return false;
      }
    }

    public void CleanupResources(bool force = false)
    {
#if DEBUG
      LogMemoryUsage("Before cleanup");
#endif
      _ = Resources.UnloadUnusedAssets();
      GC.Collect();
#if DEBUG
      LogMemoryUsage("After cleanup");
#endif
    }

    #endregion

    #region Camera Operations

    public void StartPreview()
    {
      if (_isApplicationQuitting)
      {
        return;
      }

      try
      {
        // LogMemoryUsage("Before starting preview");
        if (!isCameraActive)
        {
          cameraService?.InitializeCamera(gameObject.name);
        }
        cameraService?.StartPreview();
        // LogMemoryUsage("After starting preview");
      }
      catch (Exception e)
      {
        Debug.LogError($"CameraCapture :: Error starting preview: {e.Message}\nStack Trace: {e.StackTrace}");
        // CleanupResources(force: true);
      }
    }

    public void PausePreview()
    {
      if (_isApplicationQuitting)
      {
        return;
      }

#if UNITY_EDITOR
      isCameraActive = false;
      return;
#else
      try
      {
        if (isCameraActive && !isPreviewPaused)
        {
          cameraService?.PausePreview();
          Debug.Log("CameraCapture :: Preview paused");
        }
      }
      catch (Exception e)
      {
        Debug.LogError($"CameraCapture :: Error pausing preview: {e.Message}");
      }
#endif
    }

    public void ResumePreview()
    {
      if (_isApplicationQuitting)
      {
        return;
      }

#if UNITY_EDITOR
      isCameraActive = true;
      return;
#else
      try
      {
        // LogMemoryUsage("Before resuming preview");
        if (!isCameraActive)
        {
          cameraService?.InitializeCamera(gameObject.name);
        }

        if (isPreviewPaused)
        {
          cameraService?.ResumePreview();
        }
        // LogMemoryUsage("After resuming preview");
      }
      catch (Exception e)
      {
        Debug.LogError($"CameraCapture :: Error resuming preview: {e.Message}");
        // CleanupResources(force: true);
      }
#endif
    }

    public void StopCamera()
    {
      if (_isApplicationQuitting)
      {
        return;
      }

      try
      {
        if (isCameraActive)
        {
          cameraService?.StopCamera();
          CleanupResources();
        }
      }
      catch (Exception e)
      {
        Debug.LogError($"CameraCapture :: Error stopping camera: {e.Message}");
        CleanupResources(force: true);
      }
    }

    public void TakePhoto()
    {
      if (_isApplicationQuitting)
      {
        return;
      }

#if UNITY_EDITOR //|| UNITY_IOS
      var (rotation, scale) = CalculateRotationAndScale(UIImage.Orientation.Up);
      OnPhotoCaptured?.Invoke(_debugEditorPhoto, rotation, scale);
      // @TODO: Fix the following, which would be the actual implementation
      // UpdatePhotoTexture(_debugEditorPhoto.GetRawTextureData(), UIImage.Orientation.Up, false);
      return;
#else
      if (Interlocked.CompareExchange(ref _isCapturing, 1, 0) == 0)
      {
        try
        {
          // LogMemoryUsage("Before taking photo");
          cameraService?.TakePhoto();
        }
        catch (Exception e)
        {
          Debug.LogError($"CameraCapture :: TakePhoto :: Error taking photo: {e.Message}");
          _ = Interlocked.Exchange(ref _isCapturing, 0);
          CleanupResources(force: true);
          // LogMemoryUsage("After photo error");
        }
      }
      else
      {
        Debug.LogWarning("CameraCapture :: TakePhoto :: Already capturing");
      }
#endif
    }

    public void SwitchCamera()
    {
      if (_isApplicationQuitting)
      {
        return;
      }

      try
      {
        cameraService?.SwitchCamera();
      }
      catch (Exception e)
      {
        Debug.LogError($"CameraCapture :: Error switching camera: {e.Message}");
        CleanupResources(force: true);
      }
    }

    public enum FlashMode
    {
      Off = 0,
      On = 1,
      Auto = 2
    }

    public void SetFlashMode(FlashMode mode)
    {
      if (_isApplicationQuitting)
      {
        return;
      }

      try
      {
        cameraService?.SetFlashMode((int)mode);
      }
      catch (Exception e)
      {
        Debug.LogError($"CameraCapture :: Error setting flash mode: {e.Message}");
      }
    }

    public enum WhiteBalanceMode
    {
      Locked = 0,
      AutoWhiteBalance = 1,
      ContinuousAutoWhiteBalance = 2
    }

    public void SetWhiteBalanceMode(WhiteBalanceMode mode)
    {
      if (_isApplicationQuitting)
      {
        return;
      }

      try
      {
        cameraService?.SetWhiteBalanceMode((int)mode);
      }
      catch (Exception e)
      {
        Debug.LogError($"CameraCapture :: Error setting white balance mode: {e.Message}");
      }
    }

    public void SetColorTemperature(float temperature)
    {
      if (_isApplicationQuitting)
      {
        return;
      }

      try
      {
        cameraService?.SetColorTemperature(temperature);
      }
      catch (Exception e)
      {
        Debug.LogError($"CameraCapture :: Error setting color temperature: {e.Message}");
      }
    }

    #endregion

    #region Native Callbacks

    [AOT.MonoPInvokeCallback(typeof(Action<string>))]
    private void OnMessage(string message) => Debug.Log($"CameraCapture :: OnMessage :: {message}");

    [AOT.MonoPInvokeCallback(typeof(Action<string>))]
    private void OnCameraInitialized(string _)
    {
      if (!_isApplicationQuitting)
      {
        isCameraActive = true;
        // LogMemoryUsage("Camera initialized");
      }
    }

    [AOT.MonoPInvokeCallback(typeof(Action<string>))]
    private void OnCameraStopped(string _)
    {
      isCameraActive = false;
      // LogMemoryUsage("Before camera stopped cleanup");
      // CleanupResources();
      // LogMemoryUsage("After camera stopped cleanup");
    }

    [AOT.MonoPInvokeCallback(typeof(Action<string>))]
    private void OnPreviewPaused(string _)
    {
      isPreviewPaused = true;
      Debug.Log("CameraCapture :: Preview paused");
    }

    [AOT.MonoPInvokeCallback(typeof(Action<string>))]
    private void OnPreviewResumed(string _)
    {
      isPreviewPaused = false;
      Debug.Log("CameraCapture :: Preview resumed");
    }

    [AOT.MonoPInvokeCallback(typeof(Action<string>))]
    private void OnPhotoTakenError(string errorMessage)
    {
      if (_isApplicationQuitting)
      {
        return;
      }

      Debug.LogError($"CameraCapture :: OnPhotoTakenError :: {errorMessage}");
      _ = Interlocked.Exchange(ref _isCapturing, 0);
      OnPhotoCapturedError?.Invoke(errorMessage);
    }

    [AOT.MonoPInvokeCallback(typeof(Action<string>))]
    private void OnPhotoTaken(string pointerData)
    {
      /// ========================================== ///
      /// Ensure all paths mark photo buffer as read ///
      /// ========================================== ///
      if (_isApplicationQuitting)
      {
        // Memory management is responsibility of native code
        return;
      }

      // LogMemoryUsage("Before processing photo");
      IntPtr baseAddress = IntPtr.Zero;
      byte[] photoBytes = null;

      try
      {
        // Step 1: Parse the incoming data
        if (string.IsNullOrEmpty(pointerData))
        {
          throw new ArgumentException("Received null or empty pointer data");
        }

        var (ptr, width, height, dataLength, imageOrientation) = ParsePhotoData(pointerData);
        baseAddress = ptr;

        // Step 2: Validate photo parameters
        if (!ValidatePhotoParameters(baseAddress, width, height, dataLength))
        {
          MarkBufferAsRead(baseAddress, BufferType.Photo);
          throw new ArgumentException("Invalid photo parameters");
        }

        // Step 3: Create managed array and copy data
        try
        {
          // photoBytes = new byte[dataLength];
          photoBytes = ArrayPool<byte>.Shared.Rent(dataLength);
          Marshal.Copy(baseAddress, photoBytes, 0, dataLength);
        }
        catch (Exception e)
        {
          MarkBufferAsRead(baseAddress, BufferType.Photo);
          throw new InvalidOperationException($"Failed to copy photo data: {e.Message}", e);
        }

        // Step 4: Immediately mark buffer as read after successful copy
        try
        {
          MarkBufferAsRead(baseAddress, BufferType.Photo);
        }
        catch (Exception e)
        {
          Debug.LogError($"CameraCapture :: Error freeing photo data: {e.Message}");
          // Continue since we have our copy
        }

        // Step 5: Validate copied data
        if (photoBytes == null || photoBytes.Length == 0)
        {
          throw new InvalidOperationException("Photo data copy failed - buffer is null or empty");
        }

        // Step 6: Process the copied data on the main thread
        // var capturedPhotoBytes = photoBytes; // Capture in local variable for closure
        var capturedParameters = (width, height, imageOrientation);

        UnityMainThreadDispatcher.I.Enqueue(() =>
        {
          try
          {
            UpdatePhotoTexture(
                photoBytes,
                capturedParameters.width,
                capturedParameters.height,
                capturedParameters.imageOrientation
            );
            // LogMemoryUsage("After updating photo texture");
          }
          catch (Exception e)
          {
            Debug.LogError($"CameraCapture :: Error updating photo texture: {e.Message}\nStack Trace: {e.StackTrace}");
            // CleanupResources(force: true);
            ArrayPool<byte>.Shared.Return(photoBytes);
            OnPhotoCapturedError?.Invoke($"Failed to process photo: {e.Message}");
          }
          finally
          {
            _ = Interlocked.Exchange(ref _isCapturing, 0);
          }
        });
      }
      catch (Exception e)
      {
        Debug.LogError($"CameraCapture :: Error processing photo data: {e.Message}\nStack Trace: {e.StackTrace}");

        // Cleanup native memory if not already done
        MarkBufferAsRead(baseAddress, BufferType.Photo);

        // Reset capturing flag
        _ = Interlocked.Exchange(ref _isCapturing, 0);

        // Notify error
        OnPhotoCapturedError?.Invoke($"Failed to process photo: {e.Message}");

        // LogMemoryUsage("After photo error cleanup");
      }
      finally
      {
        MarkBufferAsRead(baseAddress, BufferType.Photo);
      }
    }

    private enum BufferType
    {
      Preview = 0,
      Photo = 1
    }

    private void MarkBufferAsRead(IntPtr baseAddress, BufferType type)
    {
      if (baseAddress == IntPtr.Zero)
      {
        return;
      }
      try
      {
        if (type == BufferType.Preview)
        {
          cameraService.MarkPreviewBufferAsRead(baseAddress);
        }
        else if (type == BufferType.Photo)
        {
          cameraService.MarkPhotoBufferAsRead(baseAddress);
        }
        baseAddress = IntPtr.Zero;
      }
      catch (Exception freeError)
      {
        baseAddress = IntPtr.Zero;
        Debug.LogError($"CameraCapture :: Error freeing {type} buffer addr : 0x{baseAddress.ToString("X")}, photo data after error: {freeError.Message}");
      }
    }

    [AOT.MonoPInvokeCallback(typeof(Action<string>))]
    private void OnPreviewFrameReceived(string pointerData)
    {
      /// ============================================ ///
      /// Ensure all paths mark preview buffer as read ///
      /// ============================================ ///
      if (_isApplicationQuitting)
      {
        return;
      }

      // Skip during photo capture to prevent race conditions
      if (Interlocked.CompareExchange(ref _isCapturing, 1, 1) == 1)
      {
        var (ptr, width, height, dataLength, imageOrientation) = ParsePhotoData(pointerData);
        MarkBufferAsRead(ptr, BufferType.Preview);
        return;
      }

      // LogMemoryUsage("Before processing preview frame");
      IntPtr baseAddress = IntPtr.Zero;
      try
      {
        var (ptr, width, height, dataLength, imageOrientation) = ParsePhotoData(pointerData);
        baseAddress = ptr;

        // Validate frame parameters
        if (!ValidatePhotoParameters(baseAddress, width, height, dataLength))
        {
          MarkBufferAsRead(baseAddress, BufferType.Preview);
          Debug.LogError("CameraCapture :: OnPreviewFrameReceived :: Invalid frame parameters");
          return;
        }

        // Log baseAddress
        // Debug.Log($"CameraCapture :: OnPreviewFrameReceived :: Base addr : 0x{baseAddress.ToString("X")}");

        // Create array and copy data immediately
        var frameData = ArrayPool<byte>.Shared.Rent(dataLength);
        // byte[] frameData = new byte[dataLength];
        Marshal.Copy(baseAddress, frameData, 0, dataLength);
        // Immediately mark buffer as read
        MarkBufferAsRead(baseAddress, BufferType.Preview);

        if (!_isApplicationQuitting)
        {
          UnityMainThreadDispatcher.I.Enqueue(() =>
          {
            try
            {
              UpdatePreviewTexture(frameData, width, height, imageOrientation);
              // LogMemoryUsage("After updating preview texture");
            }
            catch (Exception e)
            {
              Debug.LogError($"CameraCapture :: Error updating preview texture: {e.Message}");
              ArrayPool<byte>.Shared.Return(frameData);
              // CleanupResources(force: true);
            }
          });
        }
        else
        {
          ArrayPool<byte>.Shared.Return(frameData);
        }
      }
      catch (Exception e)
      {
        Debug.LogError($"CameraCapture :: Error processing preview frame: {e.Message}\nStack Trace: {e.StackTrace}");
        MarkBufferAsRead(baseAddress, BufferType.Preview);
      }
    }

    #endregion
    private bool ValidatePhotoParameters(IntPtr baseAddress, int width, int height, int dataLength)
    {
      if (baseAddress == IntPtr.Zero)
      {
        Debug.LogError("CameraCapture :: ValidatePhotoParameters :: Received null pointer");
        return false;
      }

      if (width <= 0 || height <= 0)
      {
        Debug.LogError($"CameraCapture :: ValidatePhotoParameters :: Invalid dimensions: width={width}, height={height}");
        return false;
      }

      int expectedDataLength = height * width * 4;
      if (dataLength != expectedDataLength)
      {
        Debug.LogError($"CameraCapture :: ValidatePhotoParameters :: Data length mismatch: received={dataLength}, expected={expectedDataLength}");
        return false;
      }

      return true;
    }

    private void UpdatePhotoTexture(byte[] photoBytes, int width, int height, UIImage.Orientation imageOrientation)
    {
      if (photoBytes == null || photoBytes.Length == 0)
      {
        throw new ArgumentException("Invalid photo data received");
      }

      lock (_photoTexLock)
      {
        Texture2D newTexture = null;
        try
        {
          // Create the new texture before destroying the old one
          newTexture = new Texture2D(width, height, TextureFormat.BGRA32, false)
          {
            name = "CameraCapture::UpdatePhotoTexture::newTexture"
          };

          // Load the raw BGRA data directly
          newTexture.LoadRawTextureData(photoBytes);
          newTexture.Apply();

          // If we successfully created and loaded the new texture, destroy the old one
          if (_photoTexture != null)
          {
            try
            {
              Destroy(_photoTexture);
            }
            catch (Exception e)
            {
              Debug.LogError($"CameraCapture :: Error destroying old photo texture: {e.Message}");
            }
          }
          _photoTexture = newTexture;

          Debug.Log($"CameraCapture :: Loaded photo texture - Width: {_photoTexture.width}, Height: {_photoTexture.height}");

          var (rotation, scale) = CalculateRotationAndScale(imageOrientation);
          OnPhotoCaptured?.Invoke(_photoTexture, rotation, scale);
        }
        catch (Exception e)
        {
          Debug.LogError($"CameraCapture :: Error in UpdatePhotoTexture: {e.Message}");
          OnPhotoCapturedError?.Invoke("CameraCapture :: UpdatePhotoTexture :: Error : " + e.Message);

          // Clean up the new texture if it failed to load
          if (newTexture != null)
          {
            var textureToDestroy = newTexture;
            UnityMainThreadDispatcher.I.Enqueue(() =>
            {
              try
              {
                Destroy(textureToDestroy);
              }
              catch (Exception destroyError)
              {
                Debug.LogError($"CameraCapture :: Error destroying failed texture: {destroyError.Message}");
              }
            });
          }
          throw;
        }
        finally
        {
          ArrayPool<byte>.Shared.Return(photoBytes);
        }
      }
    }

    private void UpdatePreviewTexture(byte[] frameData, int width, int height, UIImage.Orientation imageOrientation)
    {
      if (_isApplicationQuitting)
      {
        return;
      }

      Texture2D newTexture = null;
      try
      {
        lock (_previewTexLock)
        {
          // LogMemoryUsage("Before updating preview texture");

          if (_previewTexture == null || _previewTexture.width != width || _previewTexture.height != height)
          {
            newTexture = new Texture2D(width, height, TextureFormat.BGRA32, false)
            {
              name = "CameraCapture::UpdatePreviewTexture::newTexture"
            };
            var oldTexture = _previewTexture;
            _previewTexture = newTexture;

            if (oldTexture != null)
            {
              var textureToDestroy = oldTexture;
              UnityMainThreadDispatcher.I.Enqueue(() =>
              {
                try
                {
                  if (textureToDestroy != null)
                  {
                    Destroy(textureToDestroy);
                  }
                }
                catch (Exception e)
                {
                  Debug.LogError($"CameraCapture :: Error destroying old preview texture: {e.Message}");
                }
              });
            }
          }

          try
          {
            _previewTexture.LoadRawTextureData(frameData);
            _previewTexture.Apply();
          }
          catch (Exception e)
          {
            Debug.LogError($"CameraCapture :: Error loading preview texture data: {e.Message}");
            throw;
          }
        }

        var (rotation, scale) = CalculateRotationAndScale(imageOrientation);
        OnPreviewTextureUpdated?.Invoke(_previewTexture, rotation, scale);
        // LogMemoryUsage("After updating preview texture");
      }
      catch (Exception)
      {
        if (newTexture != null)
        {
          var textureToDestroy = newTexture;
          UnityMainThreadDispatcher.I.Enqueue(() =>
          {
            try
            {
              if (textureToDestroy != null)
              {
                Destroy(textureToDestroy);
              }
            }
            catch (Exception e)
            {
              Debug.LogError($"CameraCapture :: Error destroying failed preview texture: {e.Message}");
            }
          });
        }
        throw;
      }
      finally
      {
        ArrayPool<byte>.Shared.Return(frameData);
      }
    }

    private (IntPtr baseAddress, int width, int height, int dataLength, UIImage.Orientation imageOrientation) ParsePhotoData(string pointerData)
    {
      string[] parts = pointerData.Split(',');
      if (parts.Length != 5)
      {
        throw new ArgumentException($"Invalid photo data received. Expected 6 parts, got {parts.Length}");
      }

      try
      {
        IntPtr baseAddress = new IntPtr(Convert.ToInt64(parts[0], CultureInfo.InvariantCulture));
        int width = int.Parse(parts[1], CultureInfo.InvariantCulture);
        int height = int.Parse(parts[2], CultureInfo.InvariantCulture);
        int dataLength = int.Parse(parts[3], CultureInfo.InvariantCulture);
        var imageOrientation = (UIImage.Orientation)int.Parse(parts[4], CultureInfo.InvariantCulture);
#if DEBUG
        // Debug.Log($"CameraCapture :: ParsePhotoData :: " +
        //           $"Base addr : 0x{baseAddress.ToString("X")}, " +
        //           $"Width: {width}, Height: {height}, " +
        //           $"DataLength: {dataLength}, " +
        //           $"ImageOrientation: {imageOrientation}, ");
#endif

        return (baseAddress, width, height, dataLength, imageOrientation);
      }
      catch (Exception e)
      {
        throw new ArgumentException($"Error parsing photo data: {e.Message}", e);
      }
    }

    // Only supports portrait device orientation, @TODO: add support for other orientations
    private (float rotation, Vector3 scale) CalculateRotationAndScale(
    UIImage.Orientation imageOrientation)
    {
      if (imageOrientation == UIImage.Orientation.Right || imageOrientation == UIImage.Orientation.Left || imageOrientation == UIImage.Orientation.Up || imageOrientation == UIImage.Orientation.Down)
      // Right
      {
        return (90f, new Vector3(-1f, 1f, 1f));
      }
      // LeftMirrored, etc ...
      else
      {
        return (90f, new Vector3(-1f, -1f, 1f));
      }
    }

    private DeviceOrientation GetDeviceOrientation()
    {
      if (Screen.orientation != ScreenOrientation.AutoRotation)
      {
        return Screen.orientation switch
        {
          ScreenOrientation.Portrait => DeviceOrientation.Portrait,
          ScreenOrientation.PortraitUpsideDown => DeviceOrientation.PortraitUpsideDown,
          ScreenOrientation.LandscapeLeft => DeviceOrientation.LandscapeLeft,
          ScreenOrientation.LandscapeRight => DeviceOrientation.LandscapeRight,
          _ => Input.deviceOrientation
        };
      }
      return Input.deviceOrientation;
    }

    #region Support Classes and Interfaces

    internal struct PhotoData
    {
      public IntPtr baseAddress;
      public int width;
      public int height;
      public int dataLength;
      public UIImage.Orientation imageOrientation;
    }

    public interface ICameraService
    {
      void InitializeCamera(string gameObjectName);
      void StartPreview();
      void PausePreview();
      void ResumePreview();
      void TakePhoto();
      void SwitchCamera();
      void SetFlashMode(int mode);
      void SetColorTemperature(float temperature);
      void SetWhiteBalanceMode(int mode);
      void StopCamera();
      // void FreePhotoData(IntPtr pointer);
      void MarkPreviewBufferAsRead(IntPtr pointer);
      void MarkPhotoBufferAsRead(IntPtr pointer);
    }

    public class UnityEditorCameraService : ICameraService
    {
      public void InitializeCamera(string gameObjectName) => throw new NotImplementedException();
      public void PausePreview() => throw new NotImplementedException();
      public void ResumePreview() => throw new NotImplementedException();
      public void SetFlashMode(int mode) => throw new NotImplementedException();
      public void SetColorTemperature(float temperature) => throw new NotImplementedException();
      public void SetWhiteBalanceMode(int mode) => throw new NotImplementedException();
      public void StartPreview() => throw new NotImplementedException();
      public void StopCamera() => throw new NotImplementedException();
      public void SwitchCamera() => throw new NotImplementedException();
      public void TakePhoto() => throw new NotImplementedException();
      // public void FreePhotoData(IntPtr pointer) => throw new NotImplementedException();
      public void MarkPreviewBufferAsRead(IntPtr pointer) => throw new NotImplementedException();
      public void MarkPhotoBufferAsRead(IntPtr pointer) => throw new NotImplementedException();
    }

#if UNITY_IOS
    public class IosCameraService : ICameraService
    {
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
      // [DllImport("__Internal")]
      // private static extern void _FreePhotoData(IntPtr pointer);
      [DllImport("__Internal")]
      private static extern void _SwitchCamera();
      [DllImport("__Internal")]
      private static extern void _SetFlashMode(int mode);
      [DllImport("__Internal")]
      private static extern void _SetWhiteBalanceMode(int mode);
      [DllImport("__Internal")]
      private static extern void _SetColorTemperature(float temperature);
      [DllImport("__Internal")]
      private static extern void _StopCamera();

      public IosCameraService()
      {
#if !UNITY_EDITOR
      UnityBridge_setup();
#endif
      }

      public void InitializeCamera(string gameObjectName)
      {
#if !UNITY_EDITOR
      _InitializeCamera(gameObjectName);
#endif
      }
      public void StartPreview()
      {
#if !UNITY_EDITOR
      _StartPreview();
#endif
      }
      public void PausePreview() => _PausePreview();
      public void ResumePreview() => _ResumePreview();
      public void TakePhoto() => _TakePhoto();
      public void SwitchCamera() => _SwitchCamera();
      public void SetFlashMode(int mode) => _SetFlashMode(mode);
      public void SetWhiteBalanceMode(int mode) => _SetWhiteBalanceMode(mode);
      public void SetColorTemperature(float temperature) => _SetColorTemperature(temperature);
      public void StopCamera() => _StopCamera();
      // public void FreePhotoData(IntPtr pointer) => _FreePhotoData(pointer); [DllImport("__Internal")]
      [DllImport("__Internal")]
      private static extern void _MarkPreviewBufferAsRead(IntPtr pointer);

      [DllImport("__Internal")]
      private static extern void _MarkPhotoBufferAsRead(IntPtr pointer);

      public void MarkPreviewBufferAsRead(IntPtr pointer) => _MarkPreviewBufferAsRead(pointer);

      public void MarkPhotoBufferAsRead(IntPtr pointer) => _MarkPhotoBufferAsRead(pointer);
    }
#endif

    #endregion
  }
}