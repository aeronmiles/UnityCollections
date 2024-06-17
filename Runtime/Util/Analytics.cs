using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class Analytics : MonoSingleton<Analytics>
{
  public static event Action<string> OnError;

  [SerializeField] private int _maxEntriesPerFile = 500;
  [SerializeField] private float _saveInterval = 60f;
  private readonly AnalyticsData _data = new();
  private long _sessionUTC;
  private float _lastSaveTime;

  private long GetCurrentTimeMilliseconds() => DateTime.Now.ToUnixTimeMilliseconds();

  private void Start()
  {
    _sessionUTC = GetCurrentTimeMilliseconds();
    LogEvent("SessionStart", "");
  }

#if UNITY_EDITOR
  private new void OnDestroy()
  {
    base.OnDestroy();
    LogEvent("SessionEnd", "");
    StartCoroutine(Save());
  }
#endif

  public void LogEvent(string name, string description, Dictionary<string, string> parameters = null)
  {
    _data.Events.Add(new AnalyticsEvent
    {
      Timecode = GetCurrentTimeMilliseconds(),
      Name = name,
      Description = description,
      Parameters = parameters ?? new Dictionary<string, string>()
    });

    if (_data.Events.Count >= _maxEntriesPerFile || Time.time - _lastSaveTime >= _saveInterval)
    {
      if (_saveCoroutine != null)
      {
        StopCoroutine(_saveCoroutine);
      }
      _saveCoroutine = StartCoroutine(Save());
    }

    if (_data.Events.Count >= _maxEntriesPerFile)
    {
      _data.Events.Clear();
      _sessionUTC = GetCurrentTimeMilliseconds();
    }
  }

  private Coroutine _saveCoroutine;
  private IEnumerator Save()
  {
    _lastSaveTime = Time.time;
    var json = JsonConvert.SerializeObject(_data, Formatting.Indented);
    var path = GetFilePath();

    var saveTask = DataUtil.SaveStringAsync(json, path);

    Debug.Log("Analytics -> Save() :: Waiting for save task to complete");

    yield return new WaitUntil(() => saveTask.IsCompleted);

    Debug.Log("Analytics -> Save() :: Save task completed");

    if (!saveTask.Result)
    {
      OnError?.Invoke($"Failed to save analytics data to {path}");
      LogEvent("SaveError", path);
    }

    _saveCoroutine = null;
  }

  private string GetFilePath()
  {
    return $"{Application.persistentDataPath}/analytics-{_sessionUTC}.json";
  }
}

[Serializable]
internal class AnalyticsEvent
{
  public long Timecode { get; set; }
  public string Name { get; set; }
  public string Description { get; set; }
  public Dictionary<string, string> Parameters { get; set; }
}

[Serializable]
internal class AnalyticsData
{
  public List<AnalyticsEvent> Events { get; } = new();
}