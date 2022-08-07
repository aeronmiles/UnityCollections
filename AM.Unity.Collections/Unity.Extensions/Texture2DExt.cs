using UnityEngine;
using System.IO;

public static class Texture2DExt
{
    public static Texture2D LoadTextureFromPath(this Texture2D tex, string FilePath)
    {
        // Load a PNG or JPG file from disk to a Texture2D
        // Returns null if load fails
        byte[] FileData;
        if (File.Exists(FilePath))
        {
            FileData = File.ReadAllBytes(FilePath);
            tex = new Texture2D(2, 2);
            if (tex.LoadImage(FileData))
                return tex;
        }
        return null;
    }
}
