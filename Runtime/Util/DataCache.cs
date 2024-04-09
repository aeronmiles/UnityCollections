using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class DataCache : MonoSingleton<DataCache>
{
  public static string DataCachePath = Path.Combine(Application.dataPath, "..", ".cache");
  private readonly List<IID> _cache = new();

  public static void Set<T>(T data) where T : IID
  {
    I._cache.Set(data);
  }

  public static bool TryGet<T>(string id, out T data) where T : IID
  {
    return I._cache.TryGet(id, out data);
  }

  public static IEnumerator Get<T>(DataFetcher<T> dataFetcher, IDataSerializer dataSerializer, Action<T> onSuccess, Action onError = null) where T : class, IID, new()
  {
    var id = dataFetcher.id;

    // Check for cached data
    if (I._cache.TryGet(id, out object cachedData) && cachedData is T)
    {
      Debug.Log($"Using cached data for {id}");
      onSuccess?.Invoke(cachedData as T);
      yield break;
    }

    // Load from file system if cache doesn't exist
    string cacheFilePath = Path.Combine(DataCachePath, id);
    if (File.Exists(cacheFilePath))
    {
      try
      {
        T data = new();
        if (dataSerializer.Load(ref data, cacheFilePath))
        {
          I._cache.Set(data);
          Debug.Log($"Loaded cached data for {id}");
          onSuccess?.Invoke(data);
          yield break;
        }
      }
      catch (Exception ex)
      {
        Debug.LogError($"Failed to load cached data for {id}: {ex.Message}");
        onError?.Invoke();
        yield break;
      }
    }

    // Fetch from external source
    yield return dataFetcher.Fetch(data =>
    {
      I._cache.Set(data);
      try
      {
        _ = dataSerializer.Save(data, cacheFilePath);
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"Failed to save data to cache: {ex.Message}");
      }
      onSuccess?.Invoke(data);
    }, onError);
  }
}

public abstract class DataFetcher<T> : IID where T : class
{
  private string _id;
  public string id => _id.ToString();
  protected string uri;

  public DataFetcher(string uri, string id)
  {
    this.uri = uri;
    this._id = id;
  }

  public abstract IEnumerator Fetch(Action<T> onSuccess = null, Action onError = null);
}

// @TODO: File fetcher / DataFetcher implementation
// public class Texture2DFileFetcher : DataFetcher<Texture2D>
// {
//     public Texture2DFileFetcher(string uri, string id) : base(uri, id)
//     {
//     }

//     private bool Fetch(out Texture2D tex, string path)
//     {
//         tex = new Texture2D(2, 2);
//         return DataUtil.LoadTexture(out tex, path);
//     }

//     public override IEnumerator Fetch(Action<Texture2D> onSuccess = null, Action onError = null)
//     {
//         if (Fetch(out Texture2D tex, uri))
//         {
//             onSuccess?.Invoke(tex);
//         }
//         else
//         {
//         }
//     }
// }

public class Texture2DFetcher : DataFetcher<Texture2D>
{
  public Texture2DFetcher(string uri) : base(uri, Path.GetFileName(uri))
  {
  }

  public override IEnumerator Fetch(Action<Texture2D> onSuccess = null, Action onError = null)
  {
    using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(uri))
    {
      Debug.Log($"Texture2DFetch -> Fetch() :: URI: " + uri);
      yield return request.SendWebRequest();

      if (request.result == UnityWebRequest.Result.Success)
      {
        Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);
        onSuccess?.Invoke(downloadedTexture);
      }
      else
      {
        Debug.LogError("Texture2DFetch -> Image download failed: " + request.downloadHandler.error);
      }
    }
  }
}

public abstract class JsonFetcher<T> : DataFetcher<T> where T : class
{
  protected JsonFetcher(string uri, string id) : base(uri, id)
  {
  }

  public override IEnumerator Fetch(Action<T> onSuccess = null, Action onError = null)
  {
    using (UnityWebRequest request = UnityWebRequest.Get(uri))
    {
      yield return request.SendWebRequest();
      if (request.result != UnityWebRequest.Result.Success)
      {
        Debug.LogError($"Failed to download json from {uri}, error: " + request.downloadHandler.error);
        onError?.Invoke();
        yield break;
      }
      onSuccess?.Invoke(JsonConvert.DeserializeObject<T>(request.downloadHandler.text));
    }
  }
}