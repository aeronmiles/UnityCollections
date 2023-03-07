using UnityEngine;
using System.Linq;
using System.Collections;

public static class WebCamTextureUtil
{
    public static bool GetWebCamTexture(out WebCamTexture webCam, int width, int height, bool isFrontFacing = false)
    {
        var devices = WebCamTexture.devices;
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
