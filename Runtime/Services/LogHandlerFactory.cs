using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using AM.Unity.Statistics;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class LogHandlerFactory
{
  /// <summary>
  /// Abstract base class providing core logging functionality with enhanced features
  /// following SOLID principles and functional programming practices.
  /// </summary>
  public abstract class BaseLogHandler : ILogHandler, IDisposable
  {
    // Configurable constants for optimization
    private const int DEFAULT_BUFFER_SIZE = 2048;
    private const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss.ffff";

    // Thread-safe StringBuilder instance
    protected readonly StringBuilder stringBuilder;

    // Pre-allocated string arrays for common operations
    private static readonly string[] LogHeaders = Enum.GetNames(typeof(LogType));

    protected BaseLogHandler(int bufferSize = DEFAULT_BUFFER_SIZE)
    {
      if (bufferSize <= 0)
        throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size must be positive");

      stringBuilder = new StringBuilder(bufferSize);
    }

    /// <summary>
    /// Formats and logs a message with the specified parameters.
    /// </summary>
    public abstract void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args);

    /// <summary>
    /// Logs an exception with detailed information and context.
    /// </summary>
    public abstract void LogException(Exception exception, UnityEngine.Object context);

    /// <summary>
    /// Writes a standardized log header with timestamp and context information.
    /// </summary>
    protected virtual void WriteLogHeader(LogType logType, UnityEngine.Object context)
    {
      try
      {
        stringBuilder.Clear();
        _ = stringBuilder
            .Append('[')
            .Append(DateTime.Now.ToString(DATE_TIME_FORMAT))
            .Append("] [")
            .Append(LogHeaders[(int)logType])
            .Append("] ");

        if (context != null)
        {
          _ = stringBuilder
              .Append(" [")
              .Append(context.name)
              .Append("] ");
        }
      }
      catch (Exception ex)
      {
        HandleInternalError(ex, "WriteLogHeader");
      }
    }

    /// <summary>
    /// Writes detailed exception information including inner exceptions.
    /// </summary>
    protected virtual void WriteExceptionDetails(Exception exception, UnityEngine.Object context)
    {
      if (exception == null)
        throw new ArgumentNullException(nameof(exception));

      try
      {
        stringBuilder.Clear();
        AppendExceptionHeader(context);
        AppendExceptionInfo(exception, isInnerException: false);

        // Handle inner exceptions recursively
        var currentException = exception.InnerException;
        while (currentException != null)
        {
          stringBuilder.AppendLine("Inner Exception:");
          AppendExceptionInfo(currentException, isInnerException: true);
          currentException = currentException.InnerException;
        }
      }
      catch (Exception ex)
      {
        HandleInternalError(ex, "WriteExceptionDetails");
      }
    }

    private void AppendExceptionHeader(UnityEngine.Object context)
    {
      _ = stringBuilder
          .Append('[')
          .Append(DateTime.Now.ToString(DATE_TIME_FORMAT))
          .AppendLine("] [EXCEPTION]");

      if (context != null)
      {
        _ = stringBuilder
            .Append("Context: ")
            .AppendLine(context.name);
      }
    }

    private void AppendExceptionInfo(Exception exception, bool isInnerException)
    {
      _ = stringBuilder
          .Append("Type: ")
          .AppendLine(exception.GetType().FullName)
          .Append("Message: ")
          .AppendLine(exception.Message)
          .Append("StackTrace: ")
          .AppendLine(exception.StackTrace ?? "No stack trace available");
    }

    /// <summary>
    /// Handles internal errors that occur within the logger itself.
    /// </summary>
    protected virtual void HandleInternalError(Exception ex, string operation)
    {
      Debug.LogError($"Internal error in BaseLogHandler.{operation}: {ex.Message}");
    }

    /// <summary>
    /// Performs cleanup of managed resources.
    /// </summary>
    public virtual void Dispose()
    {
      stringBuilder.Clear();
    }
  }

  public class DebugLogHandler : BaseLogHandler
  {
    public override void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
      try
      {
        WriteLogHeader(logType, context);
        stringBuilder.AppendLine(string.Format(format, args));

        // Now safe to use Debug.Log as it will only be forwarded to file
        switch (logType)
        {
          case LogType.Log:
            Debug.Log(stringBuilder.ToString(), context);
            break;
          case LogType.Warning:
            Debug.LogWarning(stringBuilder.ToString(), context);
            break;
          case LogType.Error:
            Debug.LogError(stringBuilder.ToString(), context);
            break;
          default:
            Debug.Log(stringBuilder.ToString(), context);
            break;
        }
      }
      catch (FormatException ex)
      {
        Debug.LogException(ex, context);
        // Debug.LogError($"DebugLogHandler :: FormatException in LogFormat: {ex.Message}\n" +
        //               $"Format string: {format}\n" +
        //               $"Arguments: {string.Join(", ", args)}");
      }
    }

    public override void LogException(Exception exception, UnityEngine.Object context)
    {
      WriteExceptionDetails(exception, context);
      Debug.LogError(stringBuilder.ToString(), context);
    }
  }

  public class FileLogHandler : BaseLogHandler
  {
    private StreamWriter _currentFileWriter;
    private readonly string _baseFilePath;
    private readonly long _maxFileSizeBytes;
    private long _currentFileSize;
    private readonly object _lockObject = new object();

    /// <summary>
    /// Creates a new FileLogHandler with log rotation capabilities
    /// </summary>
    /// <param name="baseFilePath">Base path where log files will be stored</param>
    /// <param name="maxFileSizeMB">Maximum size of each log file in megabytes (default 10MB)</param>
    public FileLogHandler(string baseFilePath, int maxFileSizeMB = 10) : base()
    {
      _baseFilePath = baseFilePath;
      _maxFileSizeBytes = maxFileSizeMB * 1024 * 1024; // Convert MB to bytes
      CreateNewLogFile();
    }

    private void CreateNewLogFile()
    {
      string directory = Path.GetDirectoryName(_baseFilePath);
      string fileNameWithoutExt = Path.GetFileNameWithoutExtension(_baseFilePath);
      string extension = Path.GetExtension(_baseFilePath);

      // Generate timestamp for the new log file
      string timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
      string newFileName = $"{fileNameWithoutExt}-{timestamp}{extension}";
      string fullPath = Path.Combine(directory ?? "", newFileName);

      // Close existing writer if any
      _currentFileWriter?.Dispose();

      // Create directory if it doesn't exist
      Directory.CreateDirectory(directory ?? "");

      // Create new writer
      _currentFileWriter = new StreamWriter(fullPath, true);
      _currentFileSize = 0;

      // Write header to new log file
      string header = $"=== Log file created at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===\n";
      _currentFileWriter.WriteLine(header);
      _currentFileWriter.Flush();
      _currentFileSize += header.Length;
    }

    private void CheckRotation(int contentLength)
    {
      if (_currentFileSize + contentLength > _maxFileSizeBytes)
      {
        CreateNewLogFile();
      }
    }

    public override void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
      lock (_lockObject)
      {
        try
        {
          WriteLogHeader(logType, context);
          stringBuilder.AppendLine(string.Format(format, args));

          string content = stringBuilder.ToString();
          CheckRotation(content.Length);

          _currentFileWriter.Write(content);
          _currentFileWriter.Flush();
          _currentFileSize += content.Length;
        }
        catch (Exception ex)
        {
          // Fallback to console logging if file operations fail
          Debug.LogException(ex, context);
          // Debug.LogError($"FileLogHandler :: Failed to write to log file: {ex.Message}\n" +
          //               $"Format string: {format}\n" +
          //               $"Arguments: {string.Join(", ", args)}");
        }
      }
    }

    public override void LogException(Exception exception, UnityEngine.Object context)
    {
      lock (_lockObject)
      {
        try
        {
          WriteExceptionDetails(exception, context);
          string content = stringBuilder.ToString();
          CheckRotation(content.Length);

          _currentFileWriter.Write(content);
          _currentFileWriter.Flush();
          _currentFileSize += content.Length;
        }
        catch (Exception ex)
        {
          // Fallback to console logging if file operations fail
          Debug.LogException(exception, context);
        }
      }
    }

    public void Dispose()
    {
      lock (_lockObject)
      {
        _currentFileWriter?.Dispose();
      }
    }
  }

  public class StatLogger : Logger
  {
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private float _min;
    private float _max;
    private readonly List<float> _values = new List<float>();

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