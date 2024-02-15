using UnityEngine;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

public static class ARUtil
{
    public static bool ARSupportedDevice()
    {
        // if(UAP_AccessibilityManager.IsEnabled())
        // {
        //     return false; // No AR when screenreader enabled
        // }

#if UNITY_EDITOR
        return false;
#endif

#if UNITY_ANDROID
        // arfoundation checkAvailability errors and doesn't return on android < 24
#pragma warning disable CS0162
        return GetOSVersion() >= 24;
#pragma warning restore CS0162
#elif UNITY_IOS
 
        if (GetOSVersion() < 11f)
        {
            Debug.Log($"ARUtil -> ARSupportedDevice() :: AR not supported on iOS versions less than 11");
            return false;
        }

        var gen = Device.generation;
 
        if ((int)gen < (int)DeviceGeneration.iPhone6Plus ||
            gen == DeviceGeneration.iPad1Gen ||
            gen == DeviceGeneration.iPad2Gen ||
            gen == DeviceGeneration.iPad3Gen ||
            gen == DeviceGeneration.iPad4Gen ||
            gen == DeviceGeneration.iPadAir1 ||
            gen == DeviceGeneration.iPadAir2 ||
            gen == DeviceGeneration.iPadMini1Gen ||
            gen == DeviceGeneration.iPadMini2Gen ||
            gen == DeviceGeneration.iPadMini3Gen ||
            gen == DeviceGeneration.iPadMini4Gen ||
            gen == DeviceGeneration.iPodTouch1Gen ||
            gen == DeviceGeneration.iPodTouch2Gen ||
            gen == DeviceGeneration.iPodTouch3Gen ||
            gen == DeviceGeneration.iPodTouch4Gen ||
            gen == DeviceGeneration.iPodTouch5Gen ||
            gen == DeviceGeneration.iPodTouch6Gen ||
            gen == DeviceGeneration.iPodTouch7Gen ||
            gen == DeviceGeneration.iPodTouchUnknown)
        {
            Debug.Log($"ARUtil -> ARSupportedDevice() :: AR not supported on iOS for device gen: {gen}");
            return false;
        }
 
        Debug.Log($"ARUtil -> ARSupportedDevice() :: iOS device gen: {gen}");
        return true;
#endif
        return false;
    }

    public static bool FaceTrackingSupported()
    {
#if UNITY_IOS
        var gen =  Device.generation;
        if ((int)gen < (int)DeviceGeneration.iPhoneX || gen.ToString().ToLower().Contains("touch"))
        {
            Debug.Log($"ARUtil -> FaceTrackingSupported() :: iOS :: Face tracking not supported on iOS for device gen: {gen}");
            return false;
        }

        return true;
#else
        bool supported = ARSupportedDevice();
        Debug.Log($"ARUtil -> FaceTrackingSupported() :: Android :: Face tracking supported on device {supported}");
        return supported;
#endif
    }

    public static int GetOSVersion()
    {
#if UNITY_IOS
        Debug.Log("ARUtil -> GetOSVersion() :: iOS :: Device.systemVersion: " + Device.systemVersion);
        string[] ver = Device.systemVersion.Split('.');
        if (int.TryParse(ver[0], out var iOSVersion))
        {
            return iOSVersion;
        }
        else
            return -1;
#else
        var version = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
        Debug.Log("ARUtil -> GetOSVersion() :: Android :: android.os.Build$VERSION: " + version);
        return version;
#endif
    }
}
