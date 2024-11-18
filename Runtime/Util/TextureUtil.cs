using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System;
using System.Runtime.Serialization.Formatters.Binary;

public static class TextureUtil
{
  public static void SaveAsPNGToPath(this Texture2D texture, string path)
  {
    // Convert Texture2D to PNG byte array
    byte[] pngData = texture.EncodeToPNG();
    if (pngData == null)
    {
      Debug.LogError("Failed to convert texture to PNG.");
      return;
    }

    // Get the path to the persistent data directory
    Debug.Log("Saving texture to: " + path);

    // Write the byte array to a file
    try
    {
      File.WriteAllBytes(path, pngData);
      Debug.Log("Texture saved successfully.");
    }
    catch (System.Exception e)
    {
      Debug.LogError("Failed to save texture to file: " + e.Message);
    }
  }

  public static bool LoadTextureFromPath(out Texture2D tex, string FilePath)
  {
    // Load a PNG or JPG file from disk to a Texture2D
    // Returns null if load fails
    byte[] FileData;
    if (File.Exists(FilePath))
    {
      FileData = File.ReadAllBytes(FilePath);
      tex = new Texture2D(2, 2);
      tex.name = "TextureUtil::LoadTextureFromPath::tex";
      return tex.LoadImage(FileData);
    }
    tex = null;
    return false;
  }

  public static IEnumerator LoadImage(string path, Action<Texture2D> callback = null, TextureFormat textureFormat = TextureFormat.RGBA32)
  {
    using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(path))
    {
      yield return uwr.SendWebRequest();

      if (uwr.result != UnityWebRequest.Result.Success)
      {
        Debug.LogError(uwr.error);
      }
      else
      {
        // Get downloaded texture data
        Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(uwr);

        if (downloadedTexture.format == textureFormat)
        {
          callback?.Invoke(downloadedTexture);
        }
        else
        {
          // Create a new Texture2D with the specified format
          Texture2D texture = new Texture2D(downloadedTexture.width, downloadedTexture.height, textureFormat, false);
          texture.name = "TextureUtil::LoadImage::texture";
          // Copy the pixels from the downloaded texture to the new texture
          texture.SetPixels32(downloadedTexture.GetPixels32());
          texture.Apply();
          callback?.Invoke(texture);
        }
      }
    }
  }

  public static byte[] SerializeToBytes(Texture3D texture3D)
  {
    BinaryFormatter bf = new BinaryFormatter();
    using (MemoryStream ms = new MemoryStream())
    {
      bf.Serialize(ms, texture3D.GetPixels());
      return ms.ToArray();
    }
  }

  public static Texture3D DeserializeTexture3D(byte[] textureData)
  {
    BinaryFormatter bf = new BinaryFormatter();
    using (MemoryStream ms = new MemoryStream(textureData))
    {
      Color[] pixels = (Color[])bf.Deserialize(ms);
      int size = Mathf.CeilToInt(Mathf.Pow(pixels.Length, 1f / 3f));
      Texture3D texture3D = new Texture3D(size, size, size, TextureFormat.RGBA32, false);
      texture3D.SetPixels(pixels);
      texture3D.Apply();
      return texture3D;
    }
  }

  public static Texture3D LoadTexture3DFromBytes(string filepath) => DeserializeTexture3D(File.ReadAllBytes(filepath));

}
