using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CameraExt
{
    public static void BlitToTex(this Camera camera, Texture2D tex, RenderTextureFormat format, int mipCount)
    {
        var currentTarget = camera.targetTexture;
        camera.targetTexture = RenderTextureCache.Get(tex.width, tex.height, 32, format, 0);
        camera.Render();
        RenderTexture.active = camera.targetTexture;
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        camera.targetTexture = currentTarget;
        RenderTexture.active = null;
    }
}
