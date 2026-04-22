using System;
using UnityEngine;

namespace UnityCollections.Functional
{
  /// <summary>
  /// PERFORMANCE NOTES FOR HOT PATHS:
  /// 
  /// 1. ERROR ALLOCATION: Avoid creating error instances in hot paths. Error construction should be rare 
  ///    (only on actual failures). For high-frequency validation, consider using error codes or enums instead.
  ///
  /// 2. TOSTRING() ALLOCATION: Never call ToString() on errors in hot paths. String formatting allocates.
  ///    Guard ToString() calls with #if UNITY_EDITOR || DEVELOPMENT_BUILD or defer to call sites as needed.
  ///
  /// 3. STATIC SINGLETONS: For recurring constant errors (no dynamic data), cache static readonly instances:
  ///    public static readonly ValidationError Required = new ValidationError("Required field");
  ///
  /// 4. IMPLICIT CONVERSIONS: The SimpleError implicit string conversion allocates. Avoid in hot paths.
  ///
  /// 5. FACTORY METHODS: Error factory methods (NotFound, Required, etc.) allocate new instances every call.
  ///    Use sparingly in hot code or cache common instances.
  ///
  /// For optimal performance, propagate error codes/enums in hot paths and construct rich error objects 
  /// only when needed for user-facing diagnostics or logging.
  /// </summary>

  /// <summary>
  /// Base interface for all error types to enable polymorphic error handling.
  /// </summary>
  public interface IError
{
  string Message { get; }
  string Code { get; }
}

/// <summary>
/// Base abstract class for common error implementations.
/// </summary>
[Serializable]
public abstract class BaseError : IError
{
  [SerializeField] protected string _message;
  [SerializeField] protected string _code;

  protected BaseError(string message, string code = null)
  {
    _message = message ?? string.Empty;
    _code = code ?? GetType().Name;
  }

  public string Message => _message;
  public string Code => _code;

  public override string ToString() => $"{Code}: {Message}";

  public override bool Equals(object obj)
  {
    return obj is BaseError other && 
           Code == other.Code && 
           Message == other.Message;
  }

  public override int GetHashCode()
  {
    return HashCode.Combine(Code, Message);
  }
}

/// <summary>
/// Error type for validation failures.
/// </summary>
[Serializable]
public class ValidationError : BaseError
{
  [SerializeField] private string _fieldName;

  public ValidationError(string message, string fieldName = null, string code = null) 
    : base(message, code ?? "VALIDATION_ERROR")
  {
    _fieldName = fieldName;
  }

  public string FieldName => _fieldName;

  public static ValidationError Required(string fieldName)
  {
    return new ValidationError($"Field '{fieldName}' is required", fieldName, "REQUIRED");
  }

  public static ValidationError InvalidFormat(string fieldName, string expectedFormat)
  {
    return new ValidationError($"Field '{fieldName}' has invalid format. Expected: {expectedFormat}", fieldName, "INVALID_FORMAT");
  }

  public static ValidationError OutOfRange(string fieldName, object min, object max)
  {
    return new ValidationError($"Field '{fieldName}' is out of range [{min}, {max}]", fieldName, "OUT_OF_RANGE");
  }

  public override string ToString()
  {
    return string.IsNullOrEmpty(FieldName) 
      ? base.ToString() 
      : $"{Code} ({FieldName}): {Message}";
  }
}

/// <summary>
/// Error type for network-related failures.
/// </summary>
[Serializable]
public class NetworkError : BaseError
{
  [SerializeField] private int _statusCode;
  [SerializeField] private string _url;

  public NetworkError(string message, int statusCode = 0, string url = null, string code = null) 
    : base(message, code ?? "NETWORK_ERROR")
  {
    _statusCode = statusCode;
    _url = url;
  }

  public int StatusCode => _statusCode;
  public string Url => _url;

  public static NetworkError Timeout(string url)
  {
    return new NetworkError("Request timed out", 0, url, "TIMEOUT");
  }

  public static NetworkError NotFound(string url)
  {
    return new NetworkError("Resource not found", 404, url, "NOT_FOUND");
  }

  public static NetworkError Unauthorized(string url)
  {
    return new NetworkError("Unauthorized access", 401, url, "UNAUTHORIZED");
  }

  public static NetworkError ServerError(string url, int statusCode)
  {
    return new NetworkError($"Server error: {statusCode}", statusCode, url, "SERVER_ERROR");
  }

  public override string ToString()
  {
    var result = base.ToString();
    if (StatusCode > 0) result += $" (Status: {StatusCode})";
    if (!string.IsNullOrEmpty(Url)) result += $" (URL: {Url})";
    return result;
  }
}

/// <summary>
/// Error type for file I/O operations.
/// </summary>
[Serializable]
public class FileIOError : BaseError
{
  [SerializeField] private string _filePath;
  [SerializeField] private string _operation;

  public FileIOError(string message, string filePath = null, string operation = null, string code = null) 
    : base(message, code ?? "FILE_IO_ERROR")
  {
    _filePath = filePath;
    _operation = operation;
  }

  public string FilePath => _filePath;
  public string Operation => _operation;

  public static FileIOError FileNotFound(string filePath)
  {
    return new FileIOError($"File not found: {filePath}", filePath, "read", "FILE_NOT_FOUND");
  }

  public static FileIOError AccessDenied(string filePath, string operation)
  {
    return new FileIOError($"Access denied to {filePath}", filePath, operation, "ACCESS_DENIED");
  }

  public static FileIOError DiskFull(string filePath)
  {
    return new FileIOError("Insufficient disk space", filePath, "write", "DISK_FULL");
  }

  public static FileIOError InvalidPath(string filePath)
  {
    return new FileIOError($"Invalid file path: {filePath}", filePath, null, "INVALID_PATH");
  }

  public override string ToString()
  {
    var result = base.ToString();
    if (!string.IsNullOrEmpty(FilePath)) result += $" (Path: {FilePath})";
    if (!string.IsNullOrEmpty(Operation)) result += $" (Operation: {Operation})";
    return result;
  }
}

/// <summary>
/// Error type for Unity asset-related operations.
/// </summary>
[Serializable]
public class UnityAssetError : BaseError
{
  [SerializeField] private string _assetPath;
  [SerializeField] private string _assetType;

  public UnityAssetError(string message, string assetPath = null, string assetType = null, string code = null) 
    : base(message, code ?? "ASSET_ERROR")
  {
    _assetPath = assetPath;
    _assetType = assetType;
  }

  public string AssetPath => _assetPath;
  public string AssetType => _assetType;

  public static UnityAssetError NotFound(string assetPath, Type assetType)
  {
    return new UnityAssetError($"Asset not found: {assetPath}", assetPath, assetType?.Name, "ASSET_NOT_FOUND");
  }

  public static UnityAssetError LoadFailed(string assetPath, Type assetType, string reason)
  {
    return new UnityAssetError($"Failed to load asset: {reason}", assetPath, assetType?.Name, "LOAD_FAILED");
  }

  public static UnityAssetError WrongType(string assetPath, Type expectedType, Type actualType)
  {
    return new UnityAssetError(
      $"Asset type mismatch. Expected: {expectedType?.Name}, Actual: {actualType?.Name}",
      assetPath, expectedType?.Name, "WRONG_TYPE");
  }

  public override string ToString()
  {
    var result = base.ToString();
    if (!string.IsNullOrEmpty(AssetPath)) result += $" (Path: {AssetPath})";
    if (!string.IsNullOrEmpty(AssetType)) result += $" (Type: {AssetType})";
    return result;
  }
}

/// <summary>
/// Error type for Unity GameObject and Component operations.
/// </summary>
[Serializable]
public class GameObjectError : BaseError
{
  [SerializeField] private string _gameObjectName;
  [SerializeField] private string _componentType;

  public GameObjectError(string message, string gameObjectName = null, string componentType = null, string code = null) 
    : base(message, code ?? "GAMEOBJECT_ERROR")
  {
    _gameObjectName = gameObjectName;
    _componentType = componentType;
  }

  public string GameObjectName => _gameObjectName;
  public string ComponentType => _componentType;

  public static GameObjectError NotFound(string gameObjectName)
  {
    return new GameObjectError($"GameObject not found: {gameObjectName}", gameObjectName, null, "GAMEOBJECT_NOT_FOUND");
  }

  public static GameObjectError ComponentNotFound(string gameObjectName, Type componentType)
  {
    return new GameObjectError(
      $"Component {componentType?.Name} not found on GameObject {gameObjectName}",
      gameObjectName, componentType?.Name, "COMPONENT_NOT_FOUND");
  }

  public static GameObjectError Inactive(string gameObjectName)
  {
    return new GameObjectError($"GameObject is inactive: {gameObjectName}", gameObjectName, null, "GAMEOBJECT_INACTIVE");
  }

  public static GameObjectError Destroyed(string gameObjectName)
  {
    return new GameObjectError($"GameObject has been destroyed: {gameObjectName}", gameObjectName, null, "GAMEOBJECT_DESTROYED");
  }

  public override string ToString()
  {
    var result = base.ToString();
    if (!string.IsNullOrEmpty(GameObjectName)) result += $" (GameObject: {GameObjectName})";
    if (!string.IsNullOrEmpty(ComponentType)) result += $" (Component: {ComponentType})";
    return result;
  }
}

/// <summary>
/// Error type for configuration and settings-related issues.
/// </summary>
[Serializable]
public class ConfigurationError : BaseError
{
  [SerializeField] private string _configKey;
  [SerializeField] private string _configSource;

  public ConfigurationError(string message, string configKey = null, string configSource = null, string code = null) 
    : base(message, code ?? "CONFIG_ERROR")
  {
    _configKey = configKey;
    _configSource = configSource;
  }

  public string ConfigKey => _configKey;
  public string ConfigSource => _configSource;

  public static ConfigurationError MissingKey(string configKey, string configSource)
  {
    return new ConfigurationError($"Configuration key '{configKey}' not found", configKey, configSource, "MISSING_KEY");
  }

  public static ConfigurationError InvalidValue(string configKey, string configSource, string reason)
  {
    return new ConfigurationError($"Invalid value for '{configKey}': {reason}", configKey, configSource, "INVALID_VALUE");
  }

  public static ConfigurationError SourceNotFound(string configSource)
  {
    return new ConfigurationError($"Configuration source not found: {configSource}", null, configSource, "SOURCE_NOT_FOUND");
  }

  public override string ToString()
  {
    var result = base.ToString();
    if (!string.IsNullOrEmpty(ConfigKey)) result += $" (Key: {ConfigKey})";
    if (!string.IsNullOrEmpty(ConfigSource)) result += $" (Source: {ConfigSource})";
    return result;
  }
}

/// <summary>
/// Error type for parsing and serialization operations.
/// </summary>
[Serializable]
public class ParseError : BaseError
{
  [SerializeField] private string _input;
  [SerializeField] private string _expectedType;
  [SerializeField] private int _position;

  public ParseError(string message, string input = null, string expectedType = null, int position = -1, string code = null) 
    : base(message, code ?? "PARSE_ERROR")
  {
    _input = input;
    _expectedType = expectedType;
    _position = position;
  }

  public string Input => _input;
  public string ExpectedType => _expectedType;
  public int Position => _position;

  public static ParseError InvalidFormat(string input, string expectedType)
  {
    return new ParseError($"Cannot parse '{input}' as {expectedType}", input, expectedType, -1, "INVALID_FORMAT");
  }

  public static ParseError UnexpectedToken(string input, int position, char unexpectedToken)
  {
    return new ParseError($"Unexpected token '{unexpectedToken}' at position {position}", input, null, position, "UNEXPECTED_TOKEN");
  }

  public static ParseError JsonError(string input, string jsonError)
  {
    return new ParseError($"JSON parsing error: {jsonError}", input, "JSON", -1, "JSON_ERROR");
  }

  public override string ToString()
  {
    var result = base.ToString();
    if (!string.IsNullOrEmpty(ExpectedType)) result += $" (Expected: {ExpectedType})";
    if (Position >= 0) result += $" (Position: {Position})";
    return result;
  }
}

/// <summary>
/// Simple string-based error for quick prototyping.
/// PERFORMANCE WARNING: Avoid implicit string conversion in hot paths - it allocates new instances.
/// </summary>
[Serializable]
public class SimpleError : BaseError
{
  public SimpleError(string message, string code = null) : base(message, code ?? "ERROR") { }

  /// <summary>
  /// Implicit conversion from string to SimpleError.
  /// WARNING: This allocates a new SimpleError instance. Avoid in hot paths.
  /// </summary>
  public static implicit operator SimpleError(string message) => new SimpleError(message);
}

/// <summary>
/// Extension methods for common error types.
/// </summary>
public static class ErrorExt
{
  /// <summary>
  /// Converts an Exception to a SimpleError.
  /// </summary>
  public static SimpleError ToSimpleError(this Exception exception)
  {
    return new SimpleError(exception.Message, exception.GetType().Name);
  }

  /// <summary>
  /// Creates a Result with a SimpleError.
  /// </summary>
  public static Result<T, SimpleError> ErrSimple<T>(string message)
  {
    return Result<T, SimpleError>.Err(new SimpleError(message));
  }

  /// <summary>
  /// Creates a Result with a ValidationError.
  /// </summary>
  public static Result<T, ValidationError> ErrValidation<T>(string message, string fieldName = null)
  {
    return Result<T, ValidationError>.Err(new ValidationError(message, fieldName));
  }

  /// <summary>
  /// Creates a Result with a NetworkError.
  /// </summary>
  public static Result<T, NetworkError> ErrNetwork<T>(string message, int statusCode = 0, string url = null)
  {
    return Result<T, NetworkError>.Err(new NetworkError(message, statusCode, url));
  }
}
}