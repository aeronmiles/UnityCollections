// AppLogger.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine; // Assuming LogType, Debug, Application, etc. are needed
using Object = UnityEngine.Object; // Alias to avoid collision with System.Object


/// <summary>
/// Arguments for the OnLogError event.
/// </summary>
public struct LogErrorArgs
{
  public string tag { get; set; }
  public object message { get; set; }
  public Exception exception { get; set; }
  public Object caller { get; set; }
}

/// <summary>
/// Central logging service for the application.
/// Can be configured via ServiceManagerConfig or injected with handlers directly.
/// Manages distribution of log messages to various handlers (Console, File, etc.)
/// and optionally intercepts Unity's native Debug logs.
/// Handlers can be added or removed dynamically.
/// </summary>
public class AppLogger : IServiceLogger, IDisposable
{
  public static readonly string Separator = " :: ";
  public event Action<LogErrorArgs> OnLogError;

  // Use List<ILogHandler> for mutability and lock for thread safety
  private readonly List<ILogHandler> _logHandlers = new List<ILogHandler>();
  private readonly object _handlerLock = new object(); // Lock object for synchronization

  private readonly bool _isDebugBuild;
  private readonly StringBuilder _stringBuilder;
  private bool _disposed = false; // To detect redundant calls to Dispose
  // Tracks only handlers created *internally* by this instance that need disposal
  private readonly List<IDisposable> _ownedDisposableHandlers = new List<IDisposable>();

  /// <summary>
  /// Constructor for backward compatibility using ServiceManagerConfig.
  /// Creates handlers internally based on config, adds them to the handler list,
  /// and sets up Unity interception if configured.
  /// </summary>
  /// <param name="config">The configuration asset.</param>
  public AppLogger(ServiceManagerConfig config)
  {
    if (config == null)
    {
      throw new ArgumentNullException(nameof(config), "ServiceManagerConfig cannot be null.");
    }

    _isDebugBuild = Debug.isDebugBuild;
    _stringBuilder = new StringBuilder(2048);

    // --- Configure Console Logging ---
    LogType consoleFilterLevel = _isDebugBuild ? LogType.Log : config.logType;
    if (config.enableConsoleLogging)
    {
      var consoleHandler = new LogHandlerFactory.ConsoleLogHandler { FilterLogType = consoleFilterLevel };
      _ = AddHandlerInternal(consoleHandler); // Add to the main list
      _ownedDisposableHandlers.Add(consoleHandler); // Track for disposal
    }

    // --- Configure File Logging ---
    if (config.enableFileLogging)
    {
      try
      {
        string logPath = Path.Combine(Application.persistentDataPath, "log.log");
        var fileHandler = new LogHandlerFactory.FileLogHandler(logPath) { FilterLogType = config.logType };
        _ = AddHandlerInternal(fileHandler); // Add to the main list
        _ownedDisposableHandlers.Add(fileHandler); // Track for disposal
      }
      catch (Exception ex)
      {
        Debug.LogError($"[AppLogger] Failed to initialize FileLogHandler: {ex.Message}");
        // Fallback console logger if no handlers exist yet
        lock (_handlerLock) // Lock needed before checking _logHandlers
        {
          if (!_logHandlers.Any())
          {
            var fallbackConsole = new LogHandlerFactory.ConsoleLogHandler { FilterLogType = consoleFilterLevel };
            _ = AddHandlerInternal(fallbackConsole, false); // Add without lock (already holding)
            Debug.LogWarning("[AppLogger] Added fallback console logger due to FileLogHandler init failure.");
          }
        }
      }
    }

    // --- Configure Unity Log Interception ---
    if (config.enableUnityLogInterception)
    {
      try
      {
        // IMPORTANT: Pass the *live* list (_logHandlers) to the interceptor
        // Assuming UnityLogInterceptor keeps the reference
        var unityInterceptor = new LogHandlerFactory.UnityLogInterceptor(_logHandlers);
        Debug.unityLogger.logHandler = unityInterceptor;

        int handlerCount;
        lock (_handlerLock) { handlerCount = _logHandlers.Count; } // Safely get count

        string interceptMsg = $"UnityLogInterceptor installed. Initial Target Handlers: {handlerCount}";
        Debug.Log($"[AppLogger] {interceptMsg}"); // Use Debug.Log before interceptor might be fully active or if logging is async

        if (handlerCount == 0)
        {
          Debug.LogWarning("[AppLogger] UnityLogInterceptor installed, but no target handlers configured initially (Console/File). Native Unity logs might only appear via interceptor's direct handling if any.");
        }
      }
      catch (Exception ex)
      {
        Debug.LogError($"[AppLogger] Failed to set UnityLogInterceptor: {ex.Message}");
      }
    }
    else
    {
      Debug.Log("[AppLogger] UnityLogInterceptor is disabled via config.");
    }


    // --- Log Initial Status ---
    string initSource = $"Initialized via Config ({(_isDebugBuild ? "DEBUG" : "RELEASE")} BUILD)";
    int finalHandlerCount;
    lock (_handlerLock) { finalHandlerCount = _logHandlers.Count; }
    string initDetails = $"Console: {config.enableConsoleLogging}, File: {config.enableFileLogging}, Filter: {config.logType}, UnityIntercept: {config.enableUnityLogInterception}, Initial Handlers: {finalHandlerCount}";
    Log("AppLogger", $"{initSource}. {initDetails}", null);
  }

  /// <summary>
  /// Constructor for Dependency Injection. Accepts pre-configured handlers.
  /// Does NOT manage UnityLogInterceptor setup - this must be done externally if needed.
  /// Does NOT manage disposal of injected handlers.
  /// </summary>
  /// <param name="logHandlers">Collection of ILogHandler instances to use initially.</param>
  public AppLogger(IEnumerable<ILogHandler> logHandlers)
  {
    if (logHandlers == null)
    {
      throw new ArgumentNullException(nameof(logHandlers));
    }

    _isDebugBuild = Debug.isDebugBuild;
    _stringBuilder = new StringBuilder(2048);
    // _logHandlers is already initialized
    // _ownedDisposableHandlers is already initialized (remains empty for DI)

    // Add the injected handlers safely
    lock (_handlerLock)
    {
      // Create a copy to avoid issues if the source enumerable changes later
      _logHandlers.AddRange(logHandlers.Where(h => h != null));
    }

    // Log initialization status
    int initialCount;
    lock (_handlerLock) { initialCount = _logHandlers.Count; }
    Log("AppLogger", $"Initialized via DI with {initialCount} injected handlers.", null);
  }

  // --- Add/Remove Handler Methods ---

  /// <summary>
  /// Adds a log handler to the logger.
  /// The logger will distribute messages to this handler.
  /// Note: Added handlers are NOT automatically disposed by AppLogger unless added to _ownedDisposableHandlers separately.
  /// </summary>
  /// <param name="handler">The ILogHandler instance to add.</param>
  /// <returns>True if the handler was added, false if it was null or already present.</returns>
  public bool AddHandler(ILogHandler handler) => AddHandlerInternal(handler, true); // Use lock by default

  /// <summary>
  /// Internal method to add handlers, optionally skipping the lock if already held.
  /// </summary>
  private bool AddHandlerInternal(ILogHandler handler, bool acquireLock = true)
  {
    if (handler == null)
    {
      LogWarning("AppLogger", "Attempted to add a null log handler.", null);
      return false;
    }

    if (acquireLock)
    {
      Monitor.Enter(_handlerLock); // Manually manage lock if needed
    }

    try
    {
      if (_logHandlers.Contains(handler))
      {
        LogWarning("AppLogger", $"Attempted to add duplicate log handler: {handler.GetType().Name}", null);
        return false; // Indicate it wasn't added now
      }
      _logHandlers.Add(handler);
      Log("AppLogger", $"Added log handler: {handler.GetType().Name}", null);
      return true;
    }
    finally
    {
      if (acquireLock)
      {
        Monitor.Exit(_handlerLock);
      }
    }
  }


  /// <summary>
  /// Removes a log handler from the logger.
  /// The logger will stop distributing messages to this handler.
  /// Note: Removing a handler does NOT dispose it. Disposal is the responsibility of the caller
  /// or the mechanism that originally created the handler (e.g., AppLogger's Dispose for config-created handlers).
  /// </summary>
  /// <param name="handler">The ILogHandler instance to remove.</param>
  /// <returns>True if the handler was found and removed, false otherwise.</returns>
  public bool RemoveHandler(ILogHandler handler)
  {
    if (handler == null)
    {
      LogWarning("AppLogger", "Attempted to remove a null log handler.", null);
      return false;
    }

    lock (_handlerLock)
    {
      bool removed = _logHandlers.Remove(handler);
      if (removed)
      {
        Log("AppLogger", $"Removed log handler: {handler.GetType().Name}", null);
      }
      else
      {
        LogWarning("AppLogger", $"Attempted to remove log handler not found: {handler.GetType().Name}", null);
      }
      return removed;
    }
  }

  // --- Distribution Methods ---

  private void DistributeLogFormat(LogType logType, string tag, object message, Object context)
  {
    if (_disposed)
    {
      return;
    }

    string formattedMessage;
    // Use StringBuilder for efficient formatting
    lock (_stringBuilder) // Lock StringBuilder if somehow accessed concurrently (unlikely for this scope)
    {
      _ = _stringBuilder.Clear().Append(tag).Append(Separator).Append(message);
      formattedMessage = _stringBuilder.ToString();
      // Don't clear here if needed inside the handler loop lock
    }

    // Iterate over a copy of the list to avoid issues if collection modified during iteration *by the same thread*
    // The lock prevents modification by *other* threads during iteration.
    List<ILogHandler> handlersSnapshot;
    lock (_handlerLock)
    {
      // Taking a snapshot is safer if a handler might try to remove itself during logging,
      // but locking the iteration is sufficient for cross-thread safety.
      // Let's stick with locking the iteration for simplicity unless issues arise.
      // handlersSnapshot = new List<ILogHandler>(_logHandlers); // Snapshot alternative
    }

    lock (_handlerLock) // Lock during the entire iteration
    {
      foreach (var handler in _logHandlers) // Iterate the live list under lock
      {
        try { handler.LogFormat(logType, context, "{0}", formattedMessage); }
        catch (Exception ex) { Debug.LogError($"[AppLogger] Failed LogFormat via {handler.GetType().Name}: {ex.Message}\nMsg: {formattedMessage}"); }
      }
    }
  }

  private void DistributeLogException(Exception exception, string tag, object originalMessage, Object context)
  {
    if (_disposed)
    {
      return;
    }

    // Similar locking strategy as DistributeLogFormat
    lock (_handlerLock)
    {
      foreach (var handler in _logHandlers)
      {
        try { handler.LogException(exception, context); }
        catch (Exception ex) { Debug.LogError($"[AppLogger] Failed LogException via {handler.GetType().Name}: {ex.Message}\nEx: {exception?.GetType().Name ?? "NULL"}"); }
      }
    }
  }

  // --- Public API Methods (IServiceLogger implementation) ---

  public void Log(string tag, object message, Object caller = null) => DistributeLogFormat(LogType.Log, tag, message, caller);

  public void LogDebug(string tag, object message, Object caller = null)
  {
    if (_isDebugBuild)
    {
      DistributeLogFormat(LogType.Log, tag, $"[DEBUG] {message}", caller);
    }
  }

  public void LogWarning(string tag, object message, Object caller = null) => DistributeLogFormat(LogType.Warning, tag, message, caller);

  public void LogError(string tag, object message, Exception exception = null, Object caller = null)
  {
    if (_disposed)
    {
      return;
    }

    string fullMessage = message?.ToString() ?? "";

    if (exception != null)
    {
      fullMessage = string.IsNullOrEmpty(fullMessage)
         ? exception.ToString()
         : $"{fullMessage}{Separator}{exception}";

      // Log combined message + exception details first
      DistributeLogFormat(LogType.Error, tag, fullMessage, caller);
      // Then distribute raw exception
      DistributeLogException(exception, tag, message, caller);
    }
    else
    {
      DistributeLogFormat(LogType.Error, tag, fullMessage, caller);
    }

    try { OnLogError?.Invoke(new LogErrorArgs { tag = tag, message = fullMessage, exception = exception, caller = caller }); }
    catch (Exception ex) { Debug.LogError($"[AppLogger] Exception in OnLogError subscriber: {ex.Message}"); }
  }

  public void LogException(string tag, object message, Exception exception, Object caller = null)
  {
    if (_disposed)
    {
      return;
    }

    if (exception == null)
    {
      LogError(tag, message ?? "LogException called with null exception.", null, caller);
      return;
    }

    string contextualMessage = message == null
       ? exception.ToString()
       : $"{message}{Separator}{exception}";

    // Log formatted message + exception as Exception type
    DistributeLogFormat(LogType.Exception, tag, contextualMessage, caller);
    // Distribute raw exception
    DistributeLogException(exception, tag, message, caller);

    try { OnLogError?.Invoke(new LogErrorArgs { tag = tag, message = contextualMessage, exception = exception, caller = caller }); }
    catch (Exception ex) { Debug.LogError($"[AppLogger] Exception in OnLogError subscriber: {ex.Message}"); }
  }

  // --- IDisposable Implementation ---

  /// <summary>
  /// Disposes owned handlers (e.g., FileLogHandler created via ServiceManagerConfig constructor).
  /// Does NOT dispose handlers added via AddHandler() or injected via constructor unless explicitly managed elsewhere.
  /// </summary>
  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (_disposed)
    {
      return;
    }

    if (disposing)
    {
      Log("AppLogger", $"Disposing... Releasing {_ownedDisposableHandlers.Count} owned handlers.", null);
      // Dispose only the handlers *this* AppLogger instance is responsible for.
      foreach (var disposableHandler in _ownedDisposableHandlers)
      {
        try
        {
          disposableHandler.Dispose();
        }
        catch (Exception ex)
        {
          // Use Debug.LogError directly as our handlers might be disposed
          Debug.LogError($"[AppLogger] Error disposing owned handler {disposableHandler.GetType().Name}: {ex.Message}");
        }
      }
      _ownedDisposableHandlers.Clear();

      // Clear main handler list reference, but don't dispose items here
      lock (_handlerLock)
      {
        _logHandlers.Clear();
      }


      // Optional: Restore default Unity logger ONLY if this specific instance set it.
      // This logic is complex and often better handled externally.
      // Example (use with caution and proper state tracking):
      // if (_didISetUnityInterceptor) // Need a flag set during config constructor
      // {
      //     if (Debug.unityLogger.logHandler is LogHandlerFactory.UnityLogInterceptor)
      //     {
      //         Debug.Log("[AppLogger] Restoring default Unity log handler.");
      //         Debug.unityLogger.logHandler = new LogHandlerFactory.ConsoleLogHandler(); // Or original
      //     }
      // }
    }

    _disposed = true;
    // No unmanaged resources directly held by AppLogger itself to free here.
  }

  ~AppLogger()
  {
    Dispose(false);
  }
}