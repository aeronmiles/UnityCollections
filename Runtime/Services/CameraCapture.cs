using UnityEngine;
using System;
using System.Threading;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Linq;
using AVFoundation;


namespace NativeCameraCapture
{
  public class CameraCapture : MonoBehaviour
  {
    private ICameraService _cameraService;
    private ICameraService cameraService
    {
      get
      {
        if (_cameraService == null)
        {
#if UNITY_IOS
          _cameraService = new IosCameraService();
#elif UNITY_ANDROID
          _cameraService = new AndroidCameraService();
#elif UNITY_EDITOR
          // @TODO: Implement editor camera service
          // _cameraService = new UnityEditorCameraService();
#endif
        }
        return _cameraService;
      }
    }

    public event Action<Texture2D, float, Vector3, bool> OnPhotoCaptured;
    public event Action<string> OnPhotoCapturedError;
    public event Action<Texture2D, float, Vector3, bool> OnPreviewTextureUpdated;

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
      // InvokeRepeating(nameof(PeriodicResourceValidation), 30f, 30f);
    }

    private void OnDisable()
    {
      Application.lowMemory -= HandleLowMemory;
      // CancelInvoke(nameof(PeriodicResourceValidation));
      if (!_isApplicationQuitting)
      {
        StopCamera();
      }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
      if (pauseStatus)
      {
        PausePreview();
        // CleanupResources();
      }
      else if (isPreviewPaused && isCameraActive)
      {
        ResumePreview();
      }
    }

    private void OnApplicationQuit()
    {
      _isApplicationQuitting = true;
      // CleanupResources(force: true);
    }

    #endregion

    #region Resource Management

    private void LogMemoryUsage(string context)
    {
      // Debug tracked / untracked memory usage
      double totalMemory = GC.GetTotalMemory(forceFullCollection: false) / 1024.0;
      double totalMemoryTracked = GC.GetTotalMemory(forceFullCollection: true) / 1024.0;
      Debug.Log($"CameraCapture :: {context} :: Memory Untracked: {totalMemory - totalMemoryTracked} kbytes, Tracked Memory: {totalMemoryTracked} bytes");
    }

    private void HandleLowMemory()
    {
      Debug.Log("CameraCapture :: Low memory warning received");
      // LogMemoryUsage("Before emergency cleanup");
      // CleanupResources(force: true);
      // LogMemoryUsage("After emergency cleanup");
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

    private void CleanupResources(bool force = false)
    {
      LogMemoryUsage("Before cleanup");
      _ = Resources.UnloadUnusedAssets();
      GC.Collect();
      LogMemoryUsage("After cleanup");
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
    }

    public void ResumePreview()
    {
      if (_isApplicationQuitting)
      {
        return;
      }

      try
      {
        LogMemoryUsage("Before resuming preview");
        if (!isCameraActive)
        {
          cameraService?.InitializeCamera(gameObject.name);
        }

        if (isPreviewPaused)
        {
          cameraService?.ResumePreview();
        }
        LogMemoryUsage("After resuming preview");
      }
      catch (Exception e)
      {
        Debug.LogError($"CameraCapture :: Error resuming preview: {e.Message}");
        // CleanupResources(force: true);
      }
    }

    public void StopCamera()
    {
      if (_isApplicationQuitting)
      {
        return;
      }

      try
      {
        LogMemoryUsage("Before stopping camera");
        if (isCameraActive)
        {
          cameraService?.StopCamera();
          // CleanupResources();
        }
        LogMemoryUsage("After stopping camera");
      }
      catch (Exception e)
      {
        Debug.LogError($"CameraCapture :: Error stopping camera: {e.Message}");
        // CleanupResources(force: true);
      }
    }

    public void TakePhoto()
    {
      if (_isApplicationQuitting)
      {
        return;
      }

      if (Interlocked.CompareExchange(ref _isCapturing, 1, 0) == 0)
      {
        try
        {
          LogMemoryUsage("Before taking photo");
          cameraService?.TakePhoto();
        }
        catch (Exception e)
        {
          Debug.LogError($"CameraCapture :: TakePhoto :: Error taking photo: {e.Message}");
          _ = Interlocked.Exchange(ref _isCapturing, 0);
          LogMemoryUsage("After photo error");
        }
      }
      else
      {
        Debug.LogWarning("CameraCapture :: TakePhoto :: Already capturing");
      }
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
        // CleanupResources(force: true);
      }
    }

    public void SetWhiteBalanceMode(int mode)
    {
      if (_isApplicationQuitting)
      {
        return;
      }

      try
      {
        cameraService?.SetWhiteBalanceMode(mode);
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
    private void OnCameraInitialized(string _)
    {
      if (!_isApplicationQuitting)
      {
        isCameraActive = true;
        LogMemoryUsage("Camera initialized");
      }
    }

    [AOT.MonoPInvokeCallback(typeof(Action<string>))]
    private void OnCameraStopped(string _)
    {
      isCameraActive = false;
      LogMemoryUsage("Before camera stopped cleanup");
      // CleanupResources();
      LogMemoryUsage("After camera stopped cleanup");
    }

    [AOT.MonoPInvokeCallback(typeof(Action<string>))]
    private void OnPreviewPaused(string _) => isPreviewPaused = true;

    [AOT.MonoPInvokeCallback(typeof(Action<string>))]
    private void OnPreviewResumed(string _) => isPreviewPaused = false;

    [AOT.MonoPInvokeCallback(typeof(Action<string>))]
    private void OnPhotoTakenError(string errorMessage)
    {
      if (_isApplicationQuitting)
      {
        return;
      }

      Debug.LogError($"CameraCapture :: OnPhotoTakenError :: {errorMessage}");
      LogMemoryUsage("Photo error");
      _ = Interlocked.Exchange(ref _isCapturing, 0);
      OnPhotoCapturedError?.Invoke(errorMessage);
    }

    [AOT.MonoPInvokeCallback(typeof(Action<string>))]
    private void OnPhotoTaken(string pointerData)
    {
      /// ===================================== ///
      /// UNSAFE - Ensure all paths free MEMORY ///
      /// ===================================== ///
      if (_isApplicationQuitting)
      {
        return;
      }

      LogMemoryUsage("Before processing photo");
      IntPtr baseAddress = IntPtr.Zero;
      byte[] photoBytes = null;

      try
      {
        // Step 1: Parse the incoming data
        if (string.IsNullOrEmpty(pointerData))
        {
          throw new ArgumentException("Received null or empty pointer data");
        }

        var (ptr, width, height, dataLength, videoOrientation, imageOrientation, isMirrored) = ParsePhotoData(pointerData);
        baseAddress = ptr;

        // Step 2: Validate photo parameters
        if (!ValidatePhotoParameters(baseAddress, width, height, dataLength))
        {
          throw new ArgumentException("Invalid photo parameters");
        }

        // Log baseAddress
        // Debug.Log($"CameraCapture :: OnPhotoTaken :: Base addr : 0x{baseAddress.ToString("X")}");

        // Step 3: Create managed array and copy data
        try
        {
          photoBytes = new byte[dataLength];
          Marshal.Copy(baseAddress, photoBytes, 0, dataLength);
        }
        catch (Exception e)
        {
          throw new InvalidOperationException($"Failed to copy photo data: {e.Message}", e);
        }

        // Step 4: Immediately free native memory after successful copy
        try
        {
          (cameraService as IosCameraService).FreePhotoData(baseAddress);
          baseAddress = IntPtr.Zero;
          LogMemoryUsage("After freeing native photo memory");
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

        if (photoBytes.All(b => b == 0))
        {
          throw new InvalidOperationException("Photo data appears to be empty");
        }

        // Step 6: Process the copied data on the main thread
        var capturedPhotoBytes = photoBytes; // Capture in local variable for closure
        var capturedParameters = (width, height, videoOrientation, imageOrientation, isMirrored);

        UnityMainThreadDispatcher.I.Enqueue(() =>
        {
          try
          {
            UpdatePhotoTexture(
                capturedPhotoBytes,
                capturedParameters.width,
                capturedParameters.height,
                capturedParameters.videoOrientation,
                capturedParameters.imageOrientation,
                capturedParameters.isMirrored
            );
            LogMemoryUsage("After updating photo texture");
          }
          catch (Exception e)
          {
            Debug.LogError($"CameraCapture :: Error updating photo texture: {e.Message}\nStack Trace: {e.StackTrace}");
            // CleanupResources(force: true);
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
        if (baseAddress != IntPtr.Zero)
        {
          try
          {
            var cameraService = this.cameraService as IosCameraService;
            if (cameraService != null)
            {
              cameraService?.FreePhotoData(baseAddress);
            }
            else
            {
              Debug.LogError("CameraCapture :: OnPhotoTaken :: Camera service is not an instance of IosCameraService");
            }
          }
          catch (Exception freeError)
          {
            Debug.LogError($"CameraCapture :: Error freeing photo data after error: {freeError.Message}");
          }
        }

        // Reset capturing flag
        _ = Interlocked.Exchange(ref _isCapturing, 0);

        // Notify error
        OnPhotoCapturedError?.Invoke($"Failed to process photo: {e.Message}");

        LogMemoryUsage("After photo error cleanup");
      }
      finally
      {
        // Additional cleanup if needed
        photoBytes = null;
      }
    }

    [AOT.MonoPInvokeCallback(typeof(Action<string>))]
    private void OnPreviewFrameReceived(string pointerData)
    {
      /// ===================================== ///
      /// UNSAFE - Ensure all paths free MEMORY ///
      /// ===================================== ///
      if (_isApplicationQuitting)
      {
        return;
      }

      // Skip during photo capture to prevent race conditions
      if (Interlocked.CompareExchange(ref _isCapturing, 1, 1) == 1)
      {
        try
        {
          var (ptr, width, height, bytesPerRow, dataLength, videoOrientation, imageOrientation, isMirrored) =
              ParsePreviewFrameData(pointerData);
          (cameraService as IosCameraService).FreePhotoData(ptr);
          ptr = IntPtr.Zero;
        }
        catch (Exception e)
        {
          Debug.LogError($"CameraCapture :: Error freeing preview frame after photo capture: {e.Message}");
        }

        return;
      }

      // LogMemoryUsage("Before processing preview frame");
      IntPtr baseAddress = IntPtr.Zero;
      try
      {
        var (ptr, width, height, bytesPerRow, dataLength, videoOrientation, imageOrientation, isMirrored) =
            ParsePreviewFrameData(pointerData);
        baseAddress = ptr;

        // Validate frame parameters
        if (!ValidatePreviewFrameParameters(baseAddress, width, height, bytesPerRow, dataLength))
        {
          return;
        }

        // Log baseAddress
        // Debug.Log($"CameraCapture :: OnPreviewFrameReceived :: Base addr : 0x{baseAddress.ToString("X")}");

        // Create array and copy data immediately
        byte[] frameData = new byte[dataLength];
        Marshal.Copy(baseAddress, frameData, 0, dataLength);

        // Immediately free native memory
        try
        {
          try
          {
            (cameraService as IosCameraService).FreePhotoData(baseAddress);
            baseAddress = IntPtr.Zero;
          }
          catch (Exception freeError)
          {
            Debug.LogError($"CameraCapture :: Error freeing preview frame after error: {freeError.Message}");
          }
          // LogMemoryUsage("After freeing preview frame memory");
        }
        catch (Exception e)
        {
          Debug.LogError($"CameraCapture :: Error freeing preview frame data: {e.Message}");
          // Continue since we have our copy
        }

        // Validate copied data
        if (frameData.All(b => b == 0))
        {
          Debug.LogError("CameraCapture :: OnPreviewFrameReceived :: Frame data appears to be empty");
          return;
        }

        if (!_isApplicationQuitting)
        {
          UnityMainThreadDispatcher.I.Enqueue(() =>
          {
            try
            {
              UpdatePreviewTexture(frameData, width, height, videoOrientation, imageOrientation, isMirrored);
              // LogMemoryUsage("After updating preview texture");
            }
            catch (Exception e)
            {
              Debug.LogError($"CameraCapture :: Error updating preview texture: {e.Message}");
              // CleanupResources(force: true);
            }
          });
        }
      }
      catch (Exception e)
      {
        Debug.LogError($"CameraCapture :: Error processing preview frame: {e.Message}\nStack Trace: {e.StackTrace}");
        if (baseAddress != IntPtr.Zero)
        {
          try
          {
            (cameraService as IosCameraService).FreePhotoData(baseAddress);
            baseAddress = IntPtr.Zero;
          }
          catch (Exception freeError)
          {
            Debug.LogError($"CameraCapture :: Error freeing preview frame after error: {freeError.Message}");
          }
        }
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
        Debug.LogError($"CameraCapture :: ValidatePhotoParameters :: Invalid dimensions: {width}x{height}");
        return false;
      }

      return true;
    }

    private bool ValidatePreviewFrameParameters(IntPtr baseAddress, int width, int height, int bytesPerRow, int dataLength)
    {
      if (baseAddress == IntPtr.Zero)
      {
        Debug.LogError("CameraCapture :: ValidatePreviewFrameParameters :: Received null pointer");
        return false;
      }

      if (width <= 0 || height <= 0 || bytesPerRow <= 0)
      {
        Debug.LogError($"CameraCapture :: ValidatePreviewFrameParameters :: Invalid dimensions: width={width}, height={height}, bytesPerRow={bytesPerRow}");
        return false;
      }

      int expectedDataLength = height * bytesPerRow;
      if (dataLength != expectedDataLength)
      {
        Debug.LogError($"CameraCapture :: ValidatePreviewFrameParameters :: Data length mismatch: received={dataLength}, expected={expectedDataLength}");
        return false;
      }

      return true;
    }

    private void UpdatePhotoTexture(byte[] photoBytes, int width, int height,
        AVCaptureVideoOrientation videoOrientation,
        UIImage.Orientation imageOrientation, bool isMirrored)
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
          LogMemoryUsage("Before creating new photo texture");

          // Create the new texture before destroying the old one
          newTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

          // Load the image data
          if (!newTexture.LoadImage(photoBytes))
          {
            throw new InvalidOperationException("Failed to load the image data into the texture");
          }

          // If we successfully created and loaded the new texture, destroy the old one
          var oldTexture = _photoTexture;
          _photoTexture = newTexture;

          if (oldTexture != null)
          {
            var textureToDestroy = oldTexture;
            UnityMainThreadDispatcher.I.Enqueue(() =>
            {
              try
              {
                Destroy(textureToDestroy);
              }
              catch (Exception e)
              {
                Debug.LogError($"CameraCapture :: Error destroying old photo texture: {e.Message}");
              }
            });
          }

          Debug.Log($"CameraCapture :: Loaded photo texture - Width: {_photoTexture.width}, Height: {_photoTexture.height}");
          LogMemoryUsage("After creating new photo texture");

          var (rotation, scale) = CalculateRotationAndScale(videoOrientation, imageOrientation, isMirrored);
          OnPhotoCaptured?.Invoke(_photoTexture, rotation, scale, isMirrored);
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
      }
    }

    private void UpdatePreviewTexture(byte[] frameData, int width, int height,
        AVCaptureVideoOrientation videoOrientation,
        UIImage.Orientation imageOrientation, bool isMirrored)
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
            newTexture = new Texture2D(width, height, TextureFormat.BGRA32, false);
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

        var (rotation, scale) = CalculateRotationAndScale(videoOrientation, imageOrientation, isMirrored);
        OnPreviewTextureUpdated?.Invoke(_previewTexture, rotation, scale, isMirrored);
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
    }

    private (IntPtr baseAddress, int width, int height, int dataLength,
        AVCaptureVideoOrientation videoOrientation,
        UIImage.Orientation imageOrientation, bool isMirrored) ParsePhotoData(string pointerData)
    {
      string[] parts = pointerData.Split(',');
      if (parts.Length != 7)
      {
        throw new ArgumentException($"Invalid photo data received. Expected 7 parts, got {parts.Length}");
      }

      try
      {
        IntPtr baseAddress = new IntPtr(Convert.ToInt64(parts[0], CultureInfo.InvariantCulture));
        int width = int.Parse(parts[1], CultureInfo.InvariantCulture);
        int height = int.Parse(parts[2], CultureInfo.InvariantCulture);
        int dataLength = int.Parse(parts[3], CultureInfo.InvariantCulture);
        var videoOrientation = (AVCaptureVideoOrientation)int.Parse(parts[4], CultureInfo.InvariantCulture);
        var imageOrientation = (UIImage.Orientation)int.Parse(parts[5], CultureInfo.InvariantCulture);
        bool isMirrored = bool.Parse(parts[6]);

        return (baseAddress, width, height, dataLength, videoOrientation, imageOrientation, isMirrored);
      }
      catch (Exception e)
      {
        throw new ArgumentException($"Error parsing photo data: {e.Message}", e);
      }
    }

    private (IntPtr baseAddress, int width, int height, int bytesPerRow, int dataLength,
        AVCaptureVideoOrientation videoOrientation,
        UIImage.Orientation imageOrientation, bool isMirrored) ParsePreviewFrameData(string pointerData)
    {
      string[] parts = pointerData.Split(',');
      if (parts.Length != 8)
      {
        throw new ArgumentException($"Invalid preview frame data received. Expected 8 parts, got {parts.Length}");
      }

      try
      {
        IntPtr baseAddress = new IntPtr(Convert.ToInt64(parts[0], CultureInfo.InvariantCulture));
        int width = int.Parse(parts[1], CultureInfo.InvariantCulture);
        int height = int.Parse(parts[2], CultureInfo.InvariantCulture);
        int bytesPerRow = int.Parse(parts[3], CultureInfo.InvariantCulture);
        int dataLength = int.Parse(parts[4], CultureInfo.InvariantCulture);
        var videoOrientation = (AVCaptureVideoOrientation)int.Parse(parts[5], CultureInfo.InvariantCulture);
        var imageOrientation = (UIImage.Orientation)int.Parse(parts[6], CultureInfo.InvariantCulture);
        bool isMirrored = bool.Parse(parts[7]);

        Debug.Log($"CameraCapture :: ParsePreviewFrameData :: " +
                  $"Base addr : 0x{baseAddress.ToString("X")}, " +
                  $"Width: {width}, Height: {height}, " +
                  $"BytesPerRow: {bytesPerRow}, DataLength: {dataLength}, " +
                  $"VideoOrientation: {videoOrientation}, ImageOrientation: {imageOrientation}, " +
                  $"IsMirrored: {isMirrored}");
        return (baseAddress, width, height, bytesPerRow, dataLength, videoOrientation, imageOrientation, isMirrored);
      }
      catch (Exception e)
      {
        throw new ArgumentException($"Error parsing preview frame data: {e.Message}", e);
      }
    }

    private (float rotation, Vector3 scale) CalculateRotationAndScale(
        AVCaptureVideoOrientation videoOrientation,
        UIImage.Orientation imageOrientation,
        bool isMirrored)
    {
      try
      {
        DeviceOrientation orientation = GetDeviceOrientation();
        // Handle photo capture case
        float rotationAngle = IsPhotoCapture(imageOrientation) ? GetPhotoRotation(imageOrientation) : CalculatePreviewRotation(orientation, videoOrientation);

        Vector3 scale = CalculateScale(rotationAngle, imageOrientation, isMirrored);

        // Debug.Log($"CameraCapture :: Orientation calculation :: " +
        //           $"VideoOrientation: {videoOrientation}, " +
        //           $"DeviceOrientation: {orientation}, " +
        //           $"ImageOrientation: {imageOrientation}, " +
        //           $"IsMirrored: {isMirrored}, " +
        //           $"FinalRotation: {rotationAngle}, " +
        //           $"FinalScale: {scale}");

        return (rotationAngle, scale);
      }
      catch (Exception e)
      {
        Debug.LogError($"CameraCapture :: Error calculating rotation and scale: {e.Message}");
        return (0, Vector3.one);
      }
    }

    private bool IsPhotoCapture(UIImage.Orientation imageOrientation) =>
      // Photo captures will have a specific image orientation
      imageOrientation != UIImage.Orientation.Up;

    private float CalculatePreviewRotation(DeviceOrientation orientation, AVCaptureVideoOrientation videoOrientation)
    {
      // For preview, we need to consider both device and video orientation
      return (orientation, videoOrientation) switch
      {
        (DeviceOrientation.Portrait, AVCaptureVideoOrientation.LandscapeRight) => -90,
        (DeviceOrientation.Portrait, AVCaptureVideoOrientation.LandscapeLeft) => 90,
        (DeviceOrientation.PortraitUpsideDown, AVCaptureVideoOrientation.LandscapeRight) => 90,
        (DeviceOrientation.PortraitUpsideDown, AVCaptureVideoOrientation.LandscapeLeft) => -90,
        (DeviceOrientation.PortraitUpsideDown, AVCaptureVideoOrientation.Portrait) => 180,
        (DeviceOrientation.LandscapeLeft, AVCaptureVideoOrientation.Portrait) => -90,
        (DeviceOrientation.LandscapeLeft, AVCaptureVideoOrientation.PortraitUpsideDown) => 90,
        (DeviceOrientation.LandscapeLeft, AVCaptureVideoOrientation.LandscapeRight) => 180,
        (DeviceOrientation.LandscapeRight, AVCaptureVideoOrientation.Portrait) => 90,
        (DeviceOrientation.LandscapeRight, AVCaptureVideoOrientation.PortraitUpsideDown) => -90,
        (DeviceOrientation.LandscapeRight, AVCaptureVideoOrientation.LandscapeLeft) => 180,
        _ => 0
      };
    }

    private float GetPhotoRotation(UIImage.Orientation imageOrientation)
    {
      // Convert UIImage.Orientation to degrees
      // Note: These rotations are clockwise
      switch (imageOrientation)
      {
        case UIImage.Orientation.Up:
          return 0;
        case UIImage.Orientation.Down:
          return 180;
        case UIImage.Orientation.Left:
          return 90;  // Changed from 270 to 90
        case UIImage.Orientation.Right:
          return 270; // Changed from 90 to 270
        case UIImage.Orientation.UpMirrored:
          return 0;
        case UIImage.Orientation.DownMirrored:
          return 180;
        case UIImage.Orientation.LeftMirrored:
          return 90;  // Changed from 270 to 90
        case UIImage.Orientation.RightMirrored:
          return 270; // Changed from 90 to 270
        default:
          return 0;
      }
    }

    private Vector3 CalculateScale(float rotationAngle, UIImage.Orientation imageOrientation, bool isMirrored)
    {
      Vector3 scale = Vector3.one;

      // Check if we're dealing with a photo capture
      bool isPhotoCapture = IsPhotoCapture(imageOrientation);

      if (isPhotoCapture)
      {
        // For photo captures, handle mirroring based on orientation
        switch (imageOrientation)
        {
          case UIImage.Orientation.Up:
            scale.x = isMirrored ? -1f : 1f;
            break;

          case UIImage.Orientation.UpMirrored:
            scale.x = isMirrored ? 1f : -1f;
            break;

          case UIImage.Orientation.Down:
            scale.x = isMirrored ? 1f : -1f;
            scale.y = -1f;
            break;

          case UIImage.Orientation.DownMirrored:
            scale.x = isMirrored ? -1f : 1f;
            scale.y = -1f;
            break;

          case UIImage.Orientation.Left:
          case UIImage.Orientation.Right:
            scale.x = isMirrored ? -1f : 1f;
            break;

          case UIImage.Orientation.LeftMirrored:
          case UIImage.Orientation.RightMirrored:
            scale.x = isMirrored ? 1f : -1f;
            break;
        }
      }
      else
      {
        // For preview, use the rotation-based scale calculation
        bool isVerticalMirrored = imageOrientation == UIImage.Orientation.UpMirrored ||
                                 imageOrientation == UIImage.Orientation.DownMirrored;

        if (Mathf.Approximately(Mathf.Abs(rotationAngle), 0f) ||
            Mathf.Approximately(Mathf.Abs(rotationAngle), 180f))
        {
          scale.x = isMirrored ? 1f : -1f;
          scale.y = isVerticalMirrored ? -1f : 1f;
        }
        else
        {
          scale.y = isMirrored ? 1f : -1f;
          scale.x = isVerticalMirrored ? -1f : 1f;
        }
      }

      return scale;
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

#pragma warning disable IDE0051 // Remove unused private members
    private int CalculateRotationAngle(DeviceOrientation orientation, AVCaptureVideoOrientation videoOrientation)
#pragma warning restore IDE0051 // Remove unused private members
    {
      // Device orientation to angle mapping
      int deviceAngle = orientation switch
      {
        DeviceOrientation.Portrait => 0,
        DeviceOrientation.PortraitUpsideDown => 180,
        DeviceOrientation.LandscapeLeft => 270,
        DeviceOrientation.LandscapeRight => 90,
        _ => 0
      };

      // Video orientation to angle mapping
      int videoAngle = videoOrientation switch
      {
        AVCaptureVideoOrientation.Portrait => 0,
        AVCaptureVideoOrientation.PortraitUpsideDown => 180,
        AVCaptureVideoOrientation.LandscapeLeft => 270,
        AVCaptureVideoOrientation.LandscapeRight => 90,
        _ => LogAndReturnDefault()
      };

      // Helper method to log the error and return a default value.
      static int LogAndReturnDefault()
      {
        Debug.LogError("CameraCapture :: CalculateRotationAngle :: Unknown video orientation");
        return 0;
      }
      // Calculate the difference and normalize to -180 to 180 range
      int rotation = (videoAngle - deviceAngle) % 360;
      if (rotation > 180) rotation -= 360;
      if (rotation < -180) rotation += 360;

      return rotation;
    }

  }

  #region Support Classes and Interfaces

  internal struct PhotoData
  {
    public IntPtr baseAddress;
    public int width;
    public int height;
    public int dataLength;
    public AVCaptureVideoOrientation videoOrientation;
    public UIImage.Orientation imageOrientation;
    public bool isMirrored;
  }

  internal struct PreviewPhotoData
  {
    public IntPtr baseAddress;
    public int width;
    public int height;
    public int bytesPerRow;
    public int dataLength;
    public AVCaptureVideoOrientation videoOrientation;
    public UIImage.Orientation imageOrientation;
    public bool isMirrored;
  }

  public interface ICameraService
  {
    void InitializeCamera(string gameObjectName);
    void StartPreview();
    void PausePreview();
    void ResumePreview();
    void TakePhoto();
    void SwitchCamera();
    void SetColorTemperature(float temperature);
    void SetWhiteBalanceMode(int mode);
    void StopCamera();
  }

  public class UnityEditorCameraService : ICameraService
  {
    public void InitializeCamera(string gameObjectName) => throw new NotImplementedException();
    public void PausePreview() => throw new NotImplementedException();
    public void ResumePreview() => throw new NotImplementedException();
    public void SetColorTemperature(float temperature) => throw new NotImplementedException();
    public void SetWhiteBalanceMode(int mode) => throw new NotImplementedException();
    public void StartPreview() => throw new NotImplementedException();
    public void StopCamera() => throw new NotImplementedException();
    public void SwitchCamera() => throw new NotImplementedException();
    public void TakePhoto() => throw new NotImplementedException();
  }

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
    [DllImport("__Internal")]
    private static extern void _FreePhotoData(IntPtr pointer);
    [DllImport("__Internal")]
    private static extern void _SwitchCamera();
    [DllImport("__Internal")]
    private static extern void _SetWhiteBalanceMode(int mode);
    [DllImport("__Internal")]
    private static extern void _SetColorTemperature(float temperature);
    [DllImport("__Internal")]
    private static extern void _StopCamera();

    public IosCameraService()
    {
#if UNITY_IOS && !UNITY_EDITOR
      UnityBridge_setup();
#endif
    }

    public void InitializeCamera(string gameObjectName)
    {
#if UNITY_IOS && !UNITY_EDITOR
      _InitializeCamera(gameObjectName);
#endif
    }
    public void StartPreview()
    {
#if UNITY_IOS && !UNITY_EDITOR
      _StartPreview();
#endif
    }
    public void PausePreview() => _PausePreview();
    public void ResumePreview() => _ResumePreview();
    public void TakePhoto() => _TakePhoto();
    public void SwitchCamera() => _SwitchCamera();
    public void SetWhiteBalanceMode(int mode) => _SetWhiteBalanceMode(mode);
    public void SetColorTemperature(float temperature) => _SetColorTemperature(temperature);
    public void StopCamera() => _StopCamera();
    public void FreePhotoData(IntPtr pointer) => _FreePhotoData(pointer);
  }

  #endregion
}