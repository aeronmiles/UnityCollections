
using System;
using UnityEngine.Android;

public static class NativeUtil
{
#if UNITY_ANDROID
    public static void GetDevicePermission(string permission, Action<bool> onPermission)
    {
        if (Permission.HasUserAuthorizedPermission(permission))
        {
            onPermission(true);
        }
        else
        {
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionDenied += (response) => { onPermission(false); };
            callbacks.PermissionGranted += (response) => { onPermission(true); };
            callbacks.PermissionDeniedAndDontAskAgain += (response) => { onPermission(false); };
            Permission.RequestUserPermission(permission, callbacks);
        }
    }
#elif UNITY_IOS
    public static void GetDevicePermission(string permission, Action<bool> onPermission)
    {
        onPermission(true);
        throw new NotImplementedException();
    }
#endif
}