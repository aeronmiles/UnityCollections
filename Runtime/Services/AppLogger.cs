using System;
using System.IO;
using System.Text;
using UnityEngine;

public struct LogErrorArgs
{
  public string tag { get; set; }
  public object message { get; set; }
  public Exception exception { get; set; }
  public UnityEngine.Object caller { get; set; }
}

public class AppLogger : IServiceLogger
{
  public static readonly string Separator = " :: ";
  public event Action<LogErrorArgs> OnLogError;

  private readonly Logger _consoleLogger;
  private readonly Logger _fileLogger;
  private readonly ILogHandler _logInterceptor;
  private readonly bool _isDebugBuild;
  private readonly bool _enableConsoleLogging;
  private readonly bool _enableFileLogging;
  private readonly StringBuilder _stringBuilder = new StringBuilder(2048);

  public AppLogger(ServiceManagerConfig config)
  {
    _isDebugBuild = Debug.isDebugBuild;

    _enableConsoleLogging = config.enableConsoleLogging;
    _enableFileLogging = config.enableFileLogging;

    // Initialize console logger
    _consoleLogger = new Logger(new LogHandlerFactory.DebugLogHandler())
    {
      filterLogType = _isDebugBuild ? LogType.Log : LogType.Warning
    };

    // Initialize file logger if enabled
    if (_enableFileLogging)
    {
      string logPath = Path.Combine(Application.persistentDataPath, "log.log");
      _fileLogger = new Logger(new LogHandlerFactory.FileLogHandler(logPath))
      {
        filterLogType = LogType.Log
      };
    }

    // Setup Unity log interceptor if file logging is enabled
    if (_enableFileLogging)
    {
      _logInterceptor = new UnityLogInterceptor(_fileLogger);
      Debug.unityLogger.logHandler = _logInterceptor;
    }

    // Log initial debug status
    if (_isDebugBuild)
    {
      InternalLog(LogType.Log, "AppLogger", "Debug build detected, enabling all logging", null);
    }
  }

  private void InternalLog(LogType logType, string tag, object message, UnityEngine.Object caller)
  {
    if (_enableConsoleLogging)
    {
      InternalLog(_consoleLogger, logType, tag, message, caller);
    }
    // @TODO: Make this better
    if (_enableFileLogging && _fileLogger != null)
    {
      InternalLog(_fileLogger, logType, tag, message, caller);
    }
  }

  private void InternalLog(Logger logger, LogType logType, string tag, object message, UnityEngine.Object caller)
  {
    _ = _stringBuilder.Clear().Append(tag).Append(Separator).Append(message);

    switch (logType)
    {
      case LogType.Log:
        if (caller == null)
        {
          logger.Log(_stringBuilder.ToString());
        }
        else
        {
          logger.Log(_stringBuilder.ToString(), caller);
        }
        break;
      case LogType.Warning:
        if (caller == null)
        {
          logger.LogWarning(tag, _stringBuilder.ToString());
        }
        else
        {
          logger.LogWarning(_stringBuilder.ToString(), caller);
        }
        break;
      case LogType.Error:
        if (caller == null)
        {
          logger.LogError(tag, _stringBuilder.ToString());
        }
        else
        {
          logger.LogError(_stringBuilder.ToString(), caller);
        }
        break;
      case LogType.Assert:
        if (caller == null)
        {
          logger.LogError(tag, _stringBuilder.ToString());
        }
        else
        {
          logger.LogError(_stringBuilder.ToString(), caller);
        }
        break;
      case LogType.Exception:
        if (caller == null)
        {
          logger.LogError(tag, _stringBuilder.ToString());
        }
        else
        {
          logger.LogError(_stringBuilder.ToString(), caller);
        }
        break;
      default:
        break;
    }
    _ = _stringBuilder.Clear();
  }

  public void Log(string tag, object message, UnityEngine.Object caller) => InternalLog(LogType.Log, tag, message, caller);

  public void LogDebug(string tag, object message, UnityEngine.Object caller)
  {
    if (_isDebugBuild)
    {
      InternalLog(LogType.Log, tag, message, caller);
    }
  }

  public void LogWarning(string tag, object message, UnityEngine.Object caller) => InternalLog(LogType.Warning, tag, message, caller);

  public void LogError(string tag, object message, Exception exception, UnityEngine.Object caller)
  {
    if (exception != null)
    {
      LogException(tag, message, exception, caller);
      return;
    }

    InternalLog(LogType.Error, tag, message, caller);

    OnLogError?.Invoke(new LogErrorArgs
    {
      tag = tag,
      message = message,
      exception = exception,
      caller = caller
    });
  }

  public void LogException(string tag, object message, Exception exception, UnityEngine.Object caller)
  {
    string fullMessage = message == null
        ? exception.ToString()
        : $"{message} : {exception}";

    InternalLog(LogType.Error, tag, fullMessage, caller);

    if (_enableConsoleLogging)
    {
      _consoleLogger.LogException(exception, caller);
    }
    if (_enableFileLogging && _fileLogger != null)
    {
      _fileLogger.LogException(exception, caller);
    }

    OnLogError?.Invoke(new LogErrorArgs
    {
      tag = tag,
      message = fullMessage,
      exception = exception,
      caller = caller
    });
  }

  public void DisableFileLogging()
  {
    if (_fileLogger?.logHandler is IDisposable disposable)
    {
      disposable.Dispose();
    }
  }
}

// Custom log handler to intercept all Unity debug messages
public class UnityLogInterceptor : ILogHandler
{
  private readonly ILogHandler _originalUnityHandler;
  private readonly Logger _fileLogger; // Only need file logger
  private volatile bool _isProcessingLog;

  public UnityLogInterceptor(Logger fileLogger)
  {
    _originalUnityHandler = Debug.unityLogger.logHandler;
    _fileLogger = fileLogger;
  }

  public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
  {
    if (_isProcessingLog)
    {
      return;
    }

    try
    {
      _isProcessingLog = true;

      // Let Unity handle console display normally
      _originalUnityHandler.LogFormat(logType, context, format, args);

      // Only pipe to file logger
      if (_fileLogger != null)
      {
        string message = string.Format(format, args);
        _fileLogger.LogFormat(logType, context, "[Unity] {0}", message);
      }
    }
    finally
    {
      _isProcessingLog = false;
    }
  }

  public void LogException(Exception exception, UnityEngine.Object context)
  {
    if (_isProcessingLog)
    {
      return;
    }

    try
    {
      _isProcessingLog = true;

      // Let Unity handle console display
      _originalUnityHandler.LogException(exception, context);

      // Only pipe to file logger
      _fileLogger?.LogException(exception, context);
    }
    finally
    {
      _isProcessingLog = false;
    }
  }
}

