using System.Collections.Generic;
using System.Diagnostics;
using AM.Unity.Statistics;
using Unity.Mathematics;
using UnityEngine;

public static class LogUtil
{
  public abstract class Logger
  {
    protected readonly string name;
    private float _maxLogFrequency;
    public Logger(string name, float maxLogFrequency)
    {
      this.name = name;
      _maxLogFrequency = maxLogFrequency;
    }

    public void SetMaxLogFrequency(float maxLogFrequency) => _maxLogFrequency = maxLogFrequency;

    private float _lastLogTime;
    protected void Log(string message)
    {
      if (Time.time - _lastLogTime < _maxLogFrequency)
      {
        return;
      }
      UnityEngine.Debug.Log(message);
      _lastLogTime = Time.time;
    }
  }

  public class StatLogger : Logger
  {
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private float _min;
    private float _max;
    private List<float> _values = new List<float>();

    public StatLogger(string name, float maxLogFrequency) : base(name, maxLogFrequency)
    {
    }

    public void ResetValues()
    {
      _min = float.MaxValue;
      _max = float.MinValue;
      _values.Clear();
    }

    public void AddMinMaxCurrent(float min, float max, float current)
    {
      _min = math.min(_min, min);
      _max = math.max(_max, max);
      _values.Add(current);
    }

    public void StartStopwatch() => _stopwatch.Start();

    public void StopStopwatch()
    {
      _stopwatch.Stop();
      UnityEngine.Debug.Log($"{name}: Elapsed: {_stopwatch.ElapsedMilliseconds}ms");

    }

    public void LogMinMaxCurrent() => Log($"{name}: min={_min}, max={_max}, Mean={_values.Mean()}");
  }
}