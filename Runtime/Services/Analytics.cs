using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

// @TODO: Implement as service
public class Analytics : MonoSingleton<Analytics>
{
  public static event Action<string> OnError;
  [SerializeField] private int _maxEntriesPerFile = 500;
  [SerializeField] private float _saveInterval = 60f;
  private readonly AnalyticsData _data = new();
  private long _sessionUTC;
  private int _sessionID;
  private float _lastSaveTime;
  [SerializeField] private bool _sendEventsToLogger = true;

  private long GetCurrentTimeMilliseconds() => DateTime.Now.ToUnixTimeMilliseconds();

  private void Start()
  {
    _sessionUTC = GetCurrentTimeMilliseconds();
    _sessionID = PlayerPrefs.GetInt("__AnalyticsSessionID__", 0);
  }

  public void IncrementSessionID()
  {
    ++_sessionID;
    PlayerPrefs.SetInt("__AnalyticsSessionID__", _sessionID);
    PlayerPrefs.Save();
  }

  public void LogEvent(string name, string description = "", Dictionary<string, string> parameters = null)
  {
    var evnt = new AnalyticsEvent
    {
      sessionID = _sessionID,
      timecode = GetCurrentTimeMilliseconds(),
      name = name,
      description = description,
      parameters = parameters ?? new Dictionary<string, string>()
    };
    _data.events.Add(evnt);

    if (_data.events.Count >= _maxEntriesPerFile || Time.time - _lastSaveTime >= _saveInterval)
    {
      if (_saveCoroutine != null)
      {
        StopCoroutine(_saveCoroutine);
      }
      _saveCoroutine = StartCoroutine(Save());
    }

    if (_data.events.Count >= _maxEntriesPerFile)
    {
      _data.events.Clear();
      _sessionUTC = GetCurrentTimeMilliseconds();
    }
    if (_sendEventsToLogger)
    {
      ServiceManager.I.logger.Log("Analytics", evnt.ToString(), this);
    }
  }

  private Coroutine _saveCoroutine;
  private IEnumerator Save()
  {
    _lastSaveTime = Time.time;
    var json = JsonConvert.SerializeObject(_data, Formatting.Indented);
    var path = GetFilePath();

    var saveTask = IOUtil.SaveStringAsync(json, path);

    ServiceManager.I.logger.Log("Analytics", "Analytics -> Save() :: Waiting for save task to complete", this);

    yield return new WaitUntil(() => saveTask.IsCompleted);

    ServiceManager.I.logger.Log("Analytics", "Analytics -> Save() :: Save task completed", this);

    if (!saveTask.Result)
    {
      OnError?.Invoke($"Failed to save analytics data to {path}");
      LogEvent("analytics_save", "error", new Dictionary<string, string>() { { "path", path } });
    }

    _saveCoroutine = null;
  }

  private string GetFilePath() => $"{Application.persistentDataPath}/analytics-{_sessionUTC}.json";
}

[Serializable]
internal struct AnalyticsEvent
{
  public int sessionID;
  public long timecode;
  public string name;
  public string description;
  public Dictionary<string, string> parameters;
  public override readonly string ToString() => $"{sessionID} - {name} - {description}\n parameters: {parameters.ToKeyValueString()}";

  public string ToJson() => JsonConvert.SerializeObject(this, Formatting.Indented);
}

[Serializable]
internal class AnalyticsData
{
  public List<AnalyticsEvent> events = new();
}