using UnityEngine;
using System.Linq;

public static class WebCamTextureUtil
{
  public static bool GetWebCamTexture(out WebCamTexture webCam, int width, int height, bool isFrontFacing = false)
  {
    var devices = WebCamTexture.devices;
    foreach (var d in devices)
    {
      Debug.Log($"Webcam device: {d.name}");
      if (d.availableResolutions != null)
      {
        foreach (var r in d.availableResolutions)
        {
          Debug.Log($"Webcam device: {d.name} {r.width}x{r.height}");
        }
      }
    }
#if UNITY_EDITOR
    webCam = new WebCamTexture(devices[0].name, width, height, 30);
#else
        var device = devices.FirstOrDefault(d => d.isFrontFacing == isFrontFacing);
        if (device.name == null)
        {
            Debug.LogError($"No webcam device found with isFrontFacing={isFrontFacing}.");
            webCam = null;
            return false;
        }
        webCam = new WebCamTexture(device.name);
#endif
    return true;
  }
}
