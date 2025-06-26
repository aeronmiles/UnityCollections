using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using AM.Unity.Statistics; // Note: Assuming this namespace exists and provides .Mean() for List<float>
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug; // Alias for UnityEngine.Debug

// Requires Unity Gaming Services (UGS) Core and Analytics packages installed.
// Define 'UGS_ANALYTICS_ENABLED' in Project Settings > Player > Other Settings > Scripting Define Symbols
// if you want to enable this handler.

public static class LogHandlerFactory
{
  /// <summary>
  /// Abstract base class providing core logging functionality with enhanced features
  /// </summary>
  public abstract class BaseLogHandler : ILogHandler, IDisposable
  {
    // Configurable constants for optimization
    protected const int _DEFAULT_BUFFER_SIZE = 2048;
    private const string _DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss.ffff";

    // Thread-safe StringBuilder instance (Note: StringBuilder itself is not thread-safe for simultaneous writes)
    // If used across threads, external locking (like in FileLogHandler) is required.
    protected readonly StringBuilder stringBuilder;

    // Pre-allocated string arrays for common operations
    private static readonly string[] _LogHeaders = Enum.GetNames(typeof(LogType));

    private bool _disposed = false; // Flag to detect redundant calls

    /// <summary>
    /// Minimum LogType level to process. Logs below this level will be ignored.
    /// </summary>
    public LogType FilterLogType { get; set; } = LogType.Log;

    protected BaseLogHandler(int bufferSize = _DEFAULT_BUFFER_SIZE)
    {
      if (bufferSize <= 0)
      {
        throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size must be positive");
      }
      stringBuilder = new StringBuilder(bufferSize);
    }

    /// <summary>
    /// Formats and logs a message with the specified parameters. To be implemented by derived classes.
    /// </summary>
    public abstract void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args);

    /// <summary>
    /// Logs an exception with detailed information and context. To be implemented by derived classes.
    /// </summary>
    public abstract void LogException(Exception exception, UnityEngine.Object context);

    /// <summary>
    /// Helper to check if the log type should be processed based on FilterLogType.
    /// </summary>
    protected virtual bool IsLogTypeAllowed(LogType logType) =>
        GetSeverity(logType) >= GetSeverity(FilterLogType);

    /// <summary>
    /// Assigns a numerical severity to LogType for filtering. Higher number means higher severity.
    /// </summary>
    protected virtual int GetSeverity(LogType logType)
    {
      switch (logType)
      {
        case LogType.Log: return 1;
        case LogType.Warning: return 2;
        case LogType.Error: return 3;
        case LogType.Assert: return 4;
        case LogType.Exception: return 5;
        default: return 0;
      }
    }

    /// <summary>
    /// Writes a standardized log header with timestamp and context information into the internal StringBuilder.
    /// </summary>
    protected virtual void WriteLogHeader(LogType logType, UnityEngine.Object context)
    {
      try
      {
        stringBuilder.Clear(); // Use non-discard assignment for clarity if preferred: this.stringBuilder.Clear();
        stringBuilder
            .Append('[')
            .Append(DateTime.Now.ToString(_DATE_TIME_FORMAT))
            .Append("] [")
            .Append(_LogHeaders[(int)logType])
            .Append("] ");

        if (context != null)
        {
          stringBuilder
              .Append('[')
              .Append(context.name)
              .Append("] ");
        }
      }
      catch (Exception ex)
      {
        HandleInternalError(ex, "WriteLogHeader");
        stringBuilder.Clear();
      }
    }

    /// <summary>
    /// Writes detailed exception information including inner exceptions into the internal StringBuilder.
    /// </summary>
    protected virtual void WriteExceptionDetails(Exception exception, UnityEngine.Object context)
    {
      if (_disposed) return; // Don't operate if disposed

      if (exception == null)
      {
        HandleInternalError(new ArgumentNullException(nameof(exception)), "WriteExceptionDetails (null exception)");
        stringBuilder.Clear().AppendLine("LogException called with null exception object.");
        return;
      }

      try
      {
        stringBuilder.Clear();
        AppendExceptionHeader(context);
        AppendExceptionInfo(exception, isInnerException: false);

        var currentException = exception.InnerException;
        int depth = 0;
        while (currentException != null && depth < 10)
        {
          stringBuilder.AppendLine("--- Inner Exception ---");
          AppendExceptionInfo(currentException, isInnerException: true);
          currentException = currentException.InnerException;
          depth++;
        }
        if (depth >= 10)
        {
          stringBuilder.AppendLine("--- Max inner exception depth reached ---");
        }
      }
      catch (Exception ex)
      {
        HandleInternalError(ex, "WriteExceptionDetails");
        stringBuilder.Clear().AppendLine($"Failed to format exception details: {exception.GetType().Name}");
      }
    }

    private void AppendExceptionHeader(UnityEngine.Object context)
    {
      stringBuilder
          .Append('[')
          .Append(DateTime.Now.ToString(_DATE_TIME_FORMAT))
          .AppendLine("] [EXCEPTION]");

      if (context != null)
      {
        stringBuilder
            .Append("Context: ")
            .Append(context.name)
            .Append(" (Type: ")
            .Append(context.GetType().Name)
            .AppendLine(")");
      }
    }

    private void AppendExceptionInfo(Exception exception, bool isInnerException)
    {
      stringBuilder
          .Append(isInnerException ? "  Type: " : "Type: ")
          .AppendLine(exception.GetType().FullName)
          .Append(isInnerException ? "  Message: " : "Message: ")
          .AppendLine(exception.Message)
          .Append(isInnerException ? "  StackTrace: " : "StackTrace: ")
          .AppendLine(exception.StackTrace ?? "No stack trace available");
    }

    /// <summary>
    /// Handles internal errors that occur within the logger itself by logging to Unity's default console.
    /// </summary>
    protected virtual void HandleInternalError(Exception ex, string operation) =>
        Debug.LogError($"[LogHandlerFactory] Internal error in {GetType().Name}.{operation}: {ex}");

    // --- IDisposable Implementation ---

    /// <summary>
    /// Public Dispose method following the standard pattern.
    /// </summary>
    public void Dispose()
    {
      Dispose(true); // Dispose managed and unmanaged resources
      GC.SuppressFinalize(this); // Prevent the finalizer from running
    }

    /// <summary>
    /// Protected virtual Dispose method for derived classes to override.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (_disposed)
      {
        return; // Already disposed
      }

      if (disposing)
      {
        // --- Dispose managed state (managed objects) held by BaseLogHandler ---
        // Example: If StringBuilder were disposable or needed specific cleanup:
        // stringBuilder?.Dispose(); // (StringBuilder isn't IDisposable)
        // For BaseLogHandler, clearing might be considered part of managed cleanup.
        stringBuilder?.Clear();
      }

      // --- Free unmanaged resources (unmanaged objects) held by BaseLogHandler ---
      // (BaseLogHandler holds no direct unmanaged resources)

      _disposed = true; // Mark as disposed
    }

    /// <summary>
    /// Finalizer (Destructor) as a fallback. Calls Dispose with disposing=false.
    /// </summary>
    ~BaseLogHandler()
    {
      // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      Dispose(disposing: false);
    }
  }


  /// <summary>
  /// Logs messages to the Unity Debug console using the base class formatting.
  /// This remains the original implementation provided.
  /// </summary>
  public class DebugLogHandler : BaseLogHandler
  {
    // Note: Does not apply FilterLogType from BaseLogHandler currently.
    // Consider adding `if (!IsLogTypeAllowed(logType)) return;` if filtering is desired here.

    public override void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
      try
      {
        WriteLogHeader(logType, context);
        _ = stringBuilder.Append(string.Format(format, args)); // Append formatted message

        // Log using appropriate Unity Debug method
        string message = stringBuilder.ToString();
        switch (logType)
        {
          case LogType.Log: Debug.Log(message, context); break;
          case LogType.Warning: Debug.LogWarning(message, context); break;
          case LogType.Error: Debug.LogError(message, context); break;
          case LogType.Assert: Debug.LogAssertion(message, context); break;
          case LogType.Exception: Debug.LogError(message, context); break; // Log exceptions as errors
          default: Debug.Log(message, context); break;
        }
      }
      catch (FormatException ex)
      {
        HandleInternalError(ex, $"LogFormat (FormatException: format='{format}')");
        // Attempt to log raw format string and args on format error
        Debug.LogError($"[DebugLogHandler] FormatException: {ex.Message}\nFormat: {format}\nArgs: {string.Join(", ", args)}", context);
      }
      catch (Exception ex) // Catch broader exceptions during logging
      {
        HandleInternalError(ex, "LogFormat");
      }
    }

    public override void LogException(Exception exception, UnityEngine.Object context)
    {
      // Note: Does not apply FilterLogType currently. Add if needed.
      // if (!IsLogTypeAllowed(LogType.Exception)) return;

      try
      {
        WriteExceptionDetails(exception, context);
        // Log the formatted exception details as an error
        Debug.LogError(stringBuilder.ToString(), context);
      }
      catch (Exception ex) // Catch broader exceptions during logging
      {
        HandleInternalError(ex, "LogException");
        // Attempt minimal log if formatting failed
        Debug.LogException(exception, context);
      }
    }
  }


  /// <summary>
  /// NEW: Logs messages directly to Unity's console, bypassing BaseLogHandler formatting but using its filtering.
  /// Acts as a simpler pass-through console logger.
  /// </summary>
  public class ConsoleLogHandler : BaseLogHandler
  {
    // Reuses the base filtering logic but not the formatting methods (WriteLogHeader etc.)

    public ConsoleLogHandler(int bufferSize = 128) : base(bufferSize) { } // Smaller buffer as it's not used for formatting

    public override void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
      // Apply filtering first
      if (!IsLogTypeAllowed(logType)) return;

      try
      {
        // Directly use Unity's logging methods
        switch (logType)
        {
          case LogType.Log: Debug.LogFormat(context, format, args); break;
          case LogType.Warning: Debug.LogWarningFormat(context, format, args); break;
          case LogType.Error: Debug.LogErrorFormat(context, format, args); break;
          case LogType.Assert: Debug.LogAssertionFormat(context, format, args); break;
          case LogType.Exception: // Log exceptions passed via LogFormat as errors
            Debug.LogErrorFormat(context, format, args);
            break;
          default: Debug.LogFormat(context, format, args); break;
        }
      }
      catch (FormatException ex)
      {
        HandleInternalError(ex, $"LogFormat (FormatException: format='{format}')");
        Debug.LogError($"[ConsoleLogHandler] FormatException: {ex.Message}\nFormat: {format}\nArgs: {string.Join(", ", args)}", context);
      }
      catch (Exception ex)
      {
        HandleInternalError(ex, "LogFormat");
      }
    }

    public override void LogException(Exception exception, UnityEngine.Object context)
    {
      // Apply filtering first
      if (!IsLogTypeAllowed(LogType.Exception)) return;

      try
      {
        // Directly use Unity's exception logging
        Debug.LogException(exception, context);
      }
      catch (Exception ex) // Catch potential errors during exception logging itself
      {
        HandleInternalError(ex, "LogException");
      }
    }

    // No resources to dispose other than base (StringBuilder)
    // public override void Dispose() { base.Dispose(); }
  }


  /// <summary>
  /// Logs messages to a file with rotation and cleanup.
  /// Utilizes BaseLogHandler filtering and correct IDisposable pattern.
  /// </summary>
  public class FileLogHandler : BaseLogHandler // Inherits from the fixed BaseLogHandler
  {
    private StreamWriter _currentFileWriter;
    private readonly string _baseFilePath;
    private readonly long _maxFileSizeBytes;
    private readonly int _maxLogFiles = 50;
    private readonly bool _preserveErrorLogs = true;
    private long _currentFileSize;
    private readonly object _lockObject = new object(); // For thread safety

    private bool _fileHandlerDisposed = false; // Specific flag for this class level

    /// <summary>
    /// Creates a new FileLogHandler with log rotation capabilities
    /// </summary>
    public FileLogHandler(string baseFilePath, int maxFileSizeMB = 10, int bufferSize = _DEFAULT_BUFFER_SIZE)
        : base(bufferSize) // Call base constructor
    {
      if (string.IsNullOrWhiteSpace(baseFilePath))
        throw new ArgumentNullException(nameof(baseFilePath));
      if (maxFileSizeMB <= 0)
        maxFileSizeMB = 1;

      _baseFilePath = baseFilePath;
      _maxFileSizeBytes = (long)maxFileSizeMB * 1024 * 1024;

      lock (_lockObject)
      {
        try
        {
          CleanupLogFiles();
          CreateNewLogFile();
        }
        catch (Exception ex)
        {
          HandleInternalError(ex, "FileLogHandler Constructor");
          _currentFileWriter = null;
        }
      }
    }

    // --- CleanupLogFiles, CreateNewLogFile, CheckRotationAndCleanup methods remain the same ---
    // (Copied here for completeness, no changes needed in these specific methods)
    private void CleanupLogFiles()
    {
      // Expects lock to be held by caller if needed concurrently
      try
      {
        string directory = Path.GetDirectoryName(_baseFilePath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
          // Use HandleInternalError or Debug.LogWarning - avoid logging via potentially broken handler
          Debug.LogWarning($"[FileLogHandler] Log directory not found for cleanup: {directory}");
          return;
        }

        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(_baseFilePath);
        var files = Directory.GetFiles(directory)
            .Where(f => Path.GetFileName(f).StartsWith(fileNameWithoutExt))
            .Select(f => new FileInfo(f))
            .OrderBy(f => f.LastWriteTime)
            .ToList();

        if (files.Count > _maxLogFiles)
        {
          int filesToDeleteCount = files.Count - _maxLogFiles;
          var filesToDelete = new List<FileInfo>();

          foreach (var fileInfo in files)
          {
            if (filesToDeleteCount <= 0) break;
            bool preserve = _preserveErrorLogs &&
                           (fileInfo.Name.Contains("Error", StringComparison.OrdinalIgnoreCase) ||
                            fileInfo.Name.Contains("Exception", StringComparison.OrdinalIgnoreCase));
            if (!preserve)
            {
              filesToDelete.Add(fileInfo);
              filesToDeleteCount--;
            }
          }

          // If needed, delete oldest preserved files (if previous loop didn't free enough)
          int stillNeedToDelete = filesToDeleteCount;
          var oldestFiles = files.Where(f => !filesToDelete.Contains(f)).Take(stillNeedToDelete);
          filesToDelete.AddRange(oldestFiles);


          foreach (var fileInfo in filesToDelete)
          {
            try { fileInfo.Delete(); }
            catch (Exception ex) { Debug.LogError($"[FileLogHandler] Failed to delete old log file: {fileInfo.FullName} - {ex.Message}"); }
          }
        }
      }
      catch (Exception ex)
      {
        HandleInternalError(ex, "CleanupLogFiles");
      }
    }

    private void CreateNewLogFile()
    {
      // Expects lock to be held by caller
      try
      {
        // Close existing writer FIRST
        _currentFileWriter?.Close(); // Close should flush
        _currentFileWriter?.Dispose();
        _currentFileWriter = null;

        string directory = Path.GetDirectoryName(_baseFilePath);
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(_baseFilePath);
        string extension = Path.GetExtension(_baseFilePath);
        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmssfff");
        string newFileName = $"{fileNameWithoutExt}-{timestamp}{extension}";
        string fullPath = Path.Combine(directory ?? "", newFileName);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
        }

        // Use FileStream to ensure creation, then StreamWriter
        var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        _currentFileWriter = new StreamWriter(fileStream, Encoding.UTF8) { AutoFlush = false }; // Let's manage flush explicitly
        _currentFileSize = 0;

        string header = $"=== Log file created: {newFileName} at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===\n";
        byte[] headerBytes = Encoding.UTF8.GetBytes(header);
        _currentFileWriter.BaseStream.Write(headerBytes, 0, headerBytes.Length);
        _currentFileWriter.Flush();
        _currentFileSize += headerBytes.Length;
      }
      catch (Exception ex)
      {
        HandleInternalError(ex, "CreateNewLogFile");
        _currentFileWriter?.Dispose(); // Ensure disposal if creation fails midway
        _currentFileWriter = null;
      }
    }


    private void CheckRotationAndCleanup(long estimatedContentLength)
    {
      // Expects lock to be held by caller
      if (_currentFileWriter == null || _fileHandlerDisposed) return;

      if (_currentFileSize + estimatedContentLength > _maxFileSizeBytes)
      {
        try
        {
          string header = $"=== Log file rotating due to size limit ({_maxFileSizeBytes / (1024 * 1024)}MB) at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===\n";
          byte[] headerBytes = Encoding.UTF8.GetBytes(header);
          // Check stream usability before writing
          if (_currentFileWriter?.BaseStream?.CanWrite ?? false)
          {
            _currentFileWriter.BaseStream.Write(headerBytes, 0, headerBytes.Length);
            _currentFileWriter.Flush();
          }
        }
        catch (ObjectDisposedException) { /* Ignore if stream disposed during check */ }
        catch (Exception ex) { HandleInternalError(ex, "CheckRotationAndCleanup (Writing rotation header)"); }

        // Cleanup and create new file
        CleanupLogFiles(); // Cleanup before creating new file
        CreateNewLogFile(); // This closes the old writer
      }
    }


    // --- LogFormat and LogException methods remain the same ---
    // (Copied here for completeness)
    public override void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
      if (!IsLogTypeAllowed(logType) || _fileHandlerDisposed) return;

      lock (_lockObject)
      {
        if (_currentFileWriter == null || _fileHandlerDisposed) return;

        try
        {
          WriteLogHeader(logType, context);
          stringBuilder.AppendFormat(format, args); // Use AppendFormat
          stringBuilder.AppendLine();

          string content = stringBuilder.ToString();
          byte[] contentBytes = Encoding.UTF8.GetBytes(content);

          CheckRotationAndCleanup(contentBytes.LongLength);

          // Re-check writer after potential rotation
          if (_currentFileWriter == null || !_currentFileWriter.BaseStream.CanWrite) return;

          _currentFileWriter.BaseStream.Write(contentBytes, 0, contentBytes.Length);
          _currentFileWriter.Flush(); // Flush explicitly
          _currentFileSize += contentBytes.LongLength;
        }
        catch (FormatException ex)
        {
          HandleInternalError(ex, $"LogFormat (FormatException: format='{format}')");
          try { _currentFileWriter?.WriteLine($"[INTERNAL_ERROR] FormatException: {ex.Message}\nFormat: {format}\nArgs: {string.Join(", ", args)}"); _currentFileWriter?.Flush(); } catch { /* Ignore double fault */ }
        }
        catch (ObjectDisposedException) { /* Ignore if stream disposed concurrently */ }
        catch (Exception ex)
        {
          HandleInternalError(ex, "LogFormat");
        }
      }
    }

    public override void LogException(Exception exception, UnityEngine.Object context)
    {
      if (!IsLogTypeAllowed(LogType.Exception) || _fileHandlerDisposed) return;

      lock (_lockObject)
      {
        if (_currentFileWriter == null || _fileHandlerDisposed) return;

        try
        {
          WriteExceptionDetails(exception, context);
          stringBuilder.AppendLine();

          string content = stringBuilder.ToString();
          byte[] contentBytes = Encoding.UTF8.GetBytes(content);

          CheckRotationAndCleanup(contentBytes.LongLength);

          if (_currentFileWriter == null || !_currentFileWriter.BaseStream.CanWrite) return;

          _currentFileWriter.BaseStream.Write(contentBytes, 0, contentBytes.Length);
          _currentFileWriter.Flush();
          _currentFileSize += contentBytes.LongLength;
        }
        catch (ObjectDisposedException) { /* Ignore if stream disposed concurrently */ }
        catch (Exception ex)
        {
          HandleInternalError(ex, "LogException");
        }
      }
    }

    // --- Overridden Dispose method ---

    /// <summary>
    /// Disposes resources held by FileLogHandler, implementing the pattern correctly.
    /// Writes footer only when called explicitly (not from finalizer).
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
    protected override void Dispose(bool disposing)
    {
      // Use a lock to prevent race conditions during disposal
      lock (_lockObject)
      {
        if (_fileHandlerDisposed)
        {
          return; // Already disposed
        }

        if (disposing)
        {
          // --- Dispose managed state (managed objects) ---
          if (_currentFileWriter != null)
          {
            try
            {
              // --- Write Footer ONLY when disposing is true ---
              string footer = $"=== Log file closed at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===\n";
              byte[] footerBytes = Encoding.UTF8.GetBytes(footer);

              // Check stream usability before writing footer
              if (_currentFileWriter?.BaseStream?.CanWrite ?? false)
              {
                _currentFileWriter.BaseStream.Write(footerBytes, 0, footerBytes.Length);
                _currentFileWriter.Flush(); // Final flush
              }
            }
            catch (ObjectDisposedException)
            {
              // Ignore if the stream was already disposed somehow before footer write
            }
            catch (Exception ex)
            {
              // Log error writing footer using Unity's direct logger
              Debug.LogError($"[LogHandlerFactory] Internal error in FileLogHandler.Dispose (Writing footer): {ex}");
            }
            finally
            {
              // --- Ensure StreamWriter and underlying stream are closed/disposed ---
              _currentFileWriter.Close(); // This calls Dispose() on StreamWriter and the FileStream
              _currentFileWriter = null; // Release the reference
            }
          }
        }

        // --- Free unmanaged resources (unmanaged objects) ---
        // (FileLogHandler doesn't hold direct unmanaged resources)

        _fileHandlerDisposed = true; // Mark this instance as disposed
      }

      // --- Call the base class implementation AFTER derived class cleanup ---
      // This will handle resources owned by BaseLogHandler (like clearing StringBuilder)
      base.Dispose(disposing);
    }

    // Note: The public Dispose() and the Finalizer (~BaseLogHandler) are inherited
    // from the now-fixed BaseLogHandler. No need to redefine them here.
  }


  /// <summary>
  /// Original StatLogger - Not an ILogHandler implementation, but a Logger wrapper.
  /// Left as-is. Consider moving outside LogHandlerFactory if factory is strictly for ILogHandlers.
  /// </summary>
  public class StatLogger : Logger
  {
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private float _min = float.MaxValue; // Initialize min correctly
    private float _max = float.MinValue; // Initialize max correctly
    private readonly List<float> _values = new List<float>();

    // Constructor now specifies which ILogHandler to use for its internal logging.
    // Using DebugLogHandler retains original behavior. Could use ConsoleLogHandler too.
    public StatLogger(ILogHandler handlerToUse = null)
        : base(handlerToUse ?? new DebugLogHandler()) // Default to DebugLogHandler if none provided
    {
      ResetValues(); // Initialize min/max on creation
    }

    public void ResetValues()
    {
      _min = float.MaxValue;
      _max = float.MinValue;
      _values.Clear();
      _stopwatch.Reset(); // Also reset stopwatch
    }

    public void AddMinMaxCurrent(float min, float max, float current)
    {
      _min = math.min(_min, min);
      _max = math.max(_max, max);
      _values.Add(current);
    }

    public void StartStopwatch() => _stopwatch.Restart(); // Use Restart to reset and start

    public void StopStopwatch()
    {
      _stopwatch.Stop();
      // Log elapsed time using the internal logger instance
      this.Log($"Stopwatch Elapsed: {_stopwatch.ElapsedMilliseconds}ms");
    }

    public long GetElapsedMilliseconds() => _stopwatch.ElapsedMilliseconds;

    public void LogMinMaxCurrent()
    {
      // Check if values exist to avoid division by zero in Mean() and handle initialization state
      if (_values.Count > 0)
      {
        // Log stats using the internal logger instance
        // Assuming AM.Unity.Statistics provides .Mean() extension method for List<float>
        try
        {
          this.Log($"Stats: min={_min:F3}, max={_max:F3}, Mean={_values.Mean():F3}, Count={_values.Count}");
        }
        catch (Exception ex) // Handle potential error from Mean() if AM namespace is missing
        {
          this.Log($"Stats: min={_min:F3}, max={_max:F3}, Count={_values.Count} (Mean calculation failed: {ex.Message})");
        }
      }
      else
      {
        this.Log("Stats: No values recorded.");
      }
    }
  }


  /// <summary>
  /// Intercepts Unity's Debug.Log, Warning, Error, Exception calls
  /// and forwards them to a collection of injected ILogHandler instances,
  /// in addition to letting the original Unity console handler process them.
  /// </summary>
  public class UnityLogInterceptor : ILogHandler
  {
    private readonly ILogHandler _originalUnityHandler;
    private readonly IEnumerable<ILogHandler> _additionalHandlers;
    private volatile bool _isProcessingLog; // Prevents recursion

    /// <summary>
    /// Creates an interceptor that forwards logs to the provided handlers.
    /// </summary>
    /// <param name="additionalHandlers">The collection of handlers to forward logs to.</param>
    public UnityLogInterceptor(IEnumerable<ILogHandler> additionalHandlers)
    {
      // Store the original handler (usually the one writing to the Unity Editor console / Player log)
      _originalUnityHandler = Debug.unityLogger.logHandler;
      // Store the list of handlers provided (e.g., FileLogger, UgsLogger)
      _additionalHandlers = additionalHandlers ?? Enumerable.Empty<ILogHandler>();
    }

    /// <summary>
    /// Handles standard log messages intercepted from Unity's Debug logging.
    /// </summary>
    public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
      // Prevent re-entry if a handler logs back to Debug.Log during processing
      if (_isProcessingLog) return;

      try
      {
        _isProcessingLog = true;

        // 1. Let the original Unity handler (e.g., console) process it first
        _originalUnityHandler.LogFormat(logType, context, format, args);

        // 2. Distribute to all additional injected handlers
        // Optionally add a prefix to indicate the source was Unity's logger
        string message = "[Unity] " + string.Format(format, args);
        foreach (var handler in _additionalHandlers)
        {
          // Avoid logging to the original handler again if it happens to be in the list
          if (handler == _originalUnityHandler) continue;
          try
          {
            // Call LogFormat on the additional handler
            handler.LogFormat(logType, context, "{0}", message);
          }
          catch (Exception ex)
          {
            // Log errors during distribution directly to prevent recursion/loss
            Debug.LogError($"[UnityLogInterceptor] Failed LogFormat for {handler.GetType().Name}: {ex.Message}");
          }
        }
      }
      finally
      {
        _isProcessingLog = false; // Ensure flag is reset
      }
    }

    /// <summary>
    /// Handles exceptions intercepted from Unity's Debug logging.
    /// </summary>
    public void LogException(Exception exception, UnityEngine.Object context)
    {
      // Prevent re-entry
      if (_isProcessingLog) return;

      try
      {
        _isProcessingLog = true;

        // 1. Let the original Unity handler process it first
        _originalUnityHandler.LogException(exception, context);

        // 2. Distribute the raw exception to all additional injected handlers
        foreach (var handler in _additionalHandlers)
        {
          if (handler == _originalUnityHandler) continue;
          try
          {
            // Call LogException on the additional handler
            handler.LogException(exception, context);
          }
          catch (Exception ex)
          {
            Debug.LogError($"[UnityLogInterceptor] Failed LogException for {handler.GetType().Name}: {ex.Message}");
          }
        }
      }
      finally
      {
        _isProcessingLog = false; // Ensure flag is reset
      }
    }
  }
}