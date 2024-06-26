using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using AM.Unity.Statistics;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class LogUtil
{
  public abstract class BaseLogHandler : ILogHandler, IDisposable
  {
    protected readonly StringBuilder stringBuilder;
    protected readonly StringWriter stringWriter;

    protected BaseLogHandler()
    {
      stringBuilder = new StringBuilder(256);
      stringWriter = new StringWriter(stringBuilder);
    }

    public abstract void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args);
    public abstract void LogException(Exception exception, UnityEngine.Object context);

    protected void WriteLogHeader(LogType logType, UnityEngine.Object context)
    {
      stringBuilder.Clear();
      stringWriter.Write($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logType}] ");
      if (context != null)
        stringWriter.Write($"[{context.name}] ");
    }

    protected void WriteExceptionDetails(Exception exception, UnityEngine.Object context)
    {
      stringBuilder.Clear();
      stringWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [EXCEPTION]");
      if (context != null)
        stringWriter.WriteLine($"Context: {context.name}");
      stringWriter.WriteLine($"Type: {exception.GetType().FullName}");
      stringWriter.WriteLine($"Message: {exception.Message}");
      stringWriter.WriteLine($"StackTrace: {exception.StackTrace}");
      if (exception.InnerException != null)
      {
        stringWriter.WriteLine("Inner Exception:");
        stringWriter.WriteLine($"Type: {exception.InnerException.GetType().FullName}");
        stringWriter.WriteLine($"Message: {exception.InnerException.Message}");
        stringWriter.WriteLine($"StackTrace: {exception.InnerException.StackTrace}");
      }
    }

    public virtual void Dispose()
    {
      stringWriter.Dispose();
    }
  }

  public class DebugLogHandler : BaseLogHandler
  {
    public override void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
      try
      {
        WriteLogHeader(logType, context);
        // string formattedMessage = string.Format(format, args);
        Debug.unityLogger.LogFormat(logType, context, format, args);
      }
      catch (FormatException ex)
      {
        Debug.unityLogger.LogError("DebugLogHandler", $"FormatException in LogFormat: {ex.Message}");
        Debug.unityLogger.LogError("DebugLogHandler", $"Format string: {format}");
        Debug.unityLogger.LogError("DebugLogHandler", $"Arguments: {string.Join(", ", args)}");
      }
    }

    public override void LogException(Exception exception, UnityEngine.Object context)
    {
      WriteExceptionDetails(exception, context);
      Debug.unityLogger.LogException(exception, context);
    }
  }

  public class FileLogHandler : BaseLogHandler
  {
    private readonly StreamWriter fileWriter;

    public FileLogHandler(string filePath) : base()
    {
      fileWriter = new StreamWriter(filePath, true);
    }

    public override void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
      WriteLogHeader(logType, context);
      stringWriter.WriteLine(format, args);
      fileWriter.Write(stringBuilder);
      fileWriter.Flush();
    }

    public override void LogException(Exception exception, UnityEngine.Object context)
    {
      WriteExceptionDetails(exception, context);
      fileWriter.Write(stringBuilder);
      fileWriter.Flush();
    }

    public override void Dispose()
    {
      base.Dispose();
      fileWriter.Dispose();
    }
  }

  public class StatLogger : Logger
  {
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private float _min;
    private float _max;
    private List<float> _values = new List<float>();

    public StatLogger() : base(new DebugLogHandler())
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
      Debug.Log($"Elapsed: {_stopwatch.ElapsedMilliseconds}ms");

    }

    public void LogMinMaxCurrent() => Log($"min={_min}, max={_max}, Mean={_values.Mean()}");
  }
}