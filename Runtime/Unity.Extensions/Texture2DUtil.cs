using UnityEngine;
using System.IO;

public static class Texture2DUtil
{
    public static bool LoadTextureFromPath(out Texture2D tex, string FilePath)
    {
        // Load a PNG or JPG file from disk to a Texture2D
        // Returns null if load fails
        byte[] FileData;
        if (File.Exists(FilePath))
        {
            FileData = File.ReadAllBytes(FilePath);
            tex = new Texture2D(2, 2);
            return tex.LoadImage(FileData);
        }
        tex = null;
        return false;
    }

    public static void CaptureScreen(out Texture2D tex)
    {
        // Capture the screen and read to Texture2D
        tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();
    }
}
