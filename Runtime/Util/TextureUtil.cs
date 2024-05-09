using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System;
using System.Runtime.Serialization.Formatters.Binary;

public static class TextureUtil
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

  public static IEnumerator LoadImage(string path, Action<Texture2D> callback = null)
  {
    using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(path))
    {
      yield return uwr.SendWebRequest();

      if (uwr.result != UnityWebRequest.Result.Success)
      {
        Debug.Log(uwr.error);
      }
      else
      {
        // Get downloaded asset bundle
        Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
        callback?.Invoke(texture);
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
