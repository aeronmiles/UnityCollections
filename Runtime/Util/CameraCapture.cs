using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class CameraCapture : MonoBehaviour
{
  [DllImport("__Internal")]
  private static extern void _TakePhoto(string gameObjectName);

  [DllImport("__Internal")]
  private static extern void _SwitchCamera();

  [DllImport("__Internal")]
  private static extern void _SetColorTemperature(float temperature);

  public delegate void PhotoCaptureCallback(byte[] photoData);
  public event PhotoCaptureCallback OnPhotoCaptured;

  public void TakePhoto()
  {
    if (Application.platform == RuntimePlatform.IPhonePlayer)
    {
      _TakePhoto(gameObject.name);
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
  public void OnPhotoTaken(string encodedPhoto)
  {
    Debug.Log("Photo taken, size: " + encodedPhoto.Length + " bytes");
    try
    {
      byte[] photoBytes = Convert.FromBase64String(encodedPhoto);
      Debug.Log($"Photo taken, size: {photoBytes.Length} bytes");

      // Invoke the event with the photo data
      OnPhotoCaptured?.Invoke(photoBytes);

      // Example: Create a texture from the photo
      Texture2D texture = new Texture2D(2, 2);
      texture.LoadImage(photoBytes);
      // Use the texture as needed (e.g., display it on a UI element)
    }
    catch (Exception e)
    {
      Debug.LogError($"Error processing photo: {e.Message}");
    }
  }
}