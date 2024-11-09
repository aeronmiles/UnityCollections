using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

// Core interfaces following Interface Segregation Principle (ISP)
public interface IServiceRegistry
{
  void RegisterService<T>(T implementation) where T : class;
  T GetService<T>() where T : class;
  bool HasService<T>() where T : class;
}

public interface IServiceLifecycle
{
  Task InitializeServices();
  Task ShutdownServices();
  bool isInitialized { get; }
}

public interface IServiceUpdater
{
  void UpdateServices();
  void FixedUpdateServices();
  void LateUpdateServices();
}

// Service container interface following Dependency Inversion Principle (DIP)
public interface IServiceContainer : IServiceRegistry, IServiceLifecycle
{
}

// Service lifecycle interfaces (ISP)
public interface IInitializable
{
  Task Initialize();
}

public interface IUpdatable
{
  void Update();
}

public interface IFixedUpdatable
{
  void FixedUpdate();
}

public interface ILateUpdatable
{
  void LateUpdate();
}

public interface IShutdownable
{
  Task Shutdown();
}

// Logger abstraction (DIP)
public interface IServiceLogger
{
  void Log(string tag, object message, UnityEngine.Object caller);
  void LogDebug(string tag, object message, UnityEngine.Object caller);
  void LogWarning(string tag, object message, UnityEngine.Object caller);
  void LogError(string tag, object message, Exception exception, UnityEngine.Object caller);
  void LogException(string tag, object message, Exception exception, UnityEngine.Object caller);
}

// Configuration abstraction (DIP)
public interface IServiceConfiguration
{
  bool enableConsoleLogging { get; }
  bool enableFileLogging { get; }
  float initializationTimeout { get; }
  int serviceInitializationRetryCount { get; }
}

// Service container implementation following Single Responsibility Principle (SRP)
public class ServiceContainer : IServiceContainer
{
  private readonly Dictionary<Type, object> _services;
  private readonly IServiceLogger _logger;
  private readonly IServiceConfiguration _configuration;

  public bool isInitialized { get; private set; }

  public ServiceContainer(IServiceLogger logger, IServiceConfiguration configuration)
  {
    logger.Log("ServiceContainer", "Initializing service container", null);
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _services = new Dictionary<Type, object>();
  }

  public void RegisterService<T>(T implementation) where T : class
  {
    var serviceType = typeof(T);
    if (_services.ContainsKey(serviceType))
    {
      _logger.LogWarning("ServiceContainer", $"Service of type {serviceType.Name} is already registered", null);
      return;
    }

    _services[serviceType] = implementation ?? throw new ArgumentNullException(nameof(implementation));
    _logger.LogDebug("ServiceContainer", $"Registered service: {serviceType.Name}", null);
  }

  public T GetService<T>() where T : class
  {
    if (!_services.TryGetValue(typeof(T), out var service))
    {
      var ex = new InvalidOperationException($"Service of type {typeof(T).Name} is not registered");
      _logger.LogException("ServiceContainer", "Failed to get service", ex, null);
    }

    return (T)service;
  }

  public bool HasService<T>() where T : class => _services.ContainsKey(typeof(T));

  public async Task InitializeServices()
  {
    if (isInitialized)
    {
      _logger.LogWarning("ServiceContainer", "Services are already initialized", null);
      return;
    }

    try
    {
      var initializables = _services.Values.OfType<IInitializable>().ToList();
      await InitializeServicesWithRetry(initializables);
      isInitialized = true;
      _logger.Log("ServiceContainer", "All services initialized successfully", null);
    }
    catch (Exception ex)
    {
      _logger.LogError("ServiceContainer", "Failed to initialize services", ex, null);
      throw;
    }
  }

  public async Task ShutdownServices()
  {
    try
    {
      var shutdownables = _services.Values.OfType<IShutdownable>().ToList();
      await Task.WhenAll(shutdownables.Select(s => s.Shutdown()));
      _services.Clear();
      isInitialized = false;
      _logger.Log("ServiceContainer", "All services shut down successfully", null);
    }
    catch (Exception ex)
    {
      _logger.LogError("ServiceContainer", "Error during services shutdown", ex, null);
      throw;
    }
  }

  private async Task InitializeServicesWithRetry(IEnumerable<IInitializable> initializables)
  {
    var retryCount = _configuration.serviceInitializationRetryCount;
    var remainingRetries = retryCount;

    while (remainingRetries > 0)
    {
      try
      {
        var timeout = TimeSpan.FromSeconds(_configuration.initializationTimeout);
        using var cts = new System.Threading.CancellationTokenSource(timeout);
        await Task.WhenAll(initializables.Select(s => s.Initialize()));
        return;
      }
      catch (Exception ex) when (remainingRetries > 1)
      {
        remainingRetries--;
        _logger.LogWarning("ServiceContainer", $"Service initialization attempt failed. Retrying... ({remainingRetries} attempts remaining)", null);
        await Task.Delay(1000); // Wait before retrying
      }
    }
  }

}

// Service updater implementation following SRP
public class ServiceUpdater : IServiceUpdater
{
  private readonly IServiceContainer serviceContainer;
  private readonly IServiceLogger logger;
  private readonly List<IUpdatable> updatables;
  private readonly List<IFixedUpdatable> fixedUpdatables;
  private readonly List<ILateUpdatable> lateUpdatables;

  public ServiceUpdater(IServiceContainer serviceContainer, IServiceLogger logger)
  {
    logger.Log("ServiceUpdater", "Initializing service updater", null);
    this.serviceContainer = serviceContainer ?? throw new ArgumentNullException(nameof(serviceContainer));
    this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

    updatables = new List<IUpdatable>();
    fixedUpdatables = new List<IFixedUpdatable>();
    lateUpdatables = new List<ILateUpdatable>();
  }

  public void UpdateServices()
  {
    if (!serviceContainer.isInitialized)
    {
      return;
    }

    foreach (var service in updatables)
    {
      try
      {
        service.Update();
      }
      catch (Exception ex)
      {
        logger.LogError("ServieUpdate", $"Error in {service.GetType().Name}.Update()", ex, null);
      }
    }
  }

  public void FixedUpdateServices()
  {
    if (!serviceContainer.isInitialized)
    {
      return;
    }

    foreach (var service in fixedUpdatables)
    {
      try
      {
        service.FixedUpdate();
      }
      catch (Exception ex)
      {
        logger.LogError("ServiceUpdate", $"Error in {service.GetType().Name}.FixedUpdate()", ex, null);
      }
    }
  }

  public void LateUpdateServices()
  {
    if (!serviceContainer.isInitialized)
    {
      return;
    }

    foreach (var service in lateUpdatables)
    {
      try
      {
        service.LateUpdate();
      }
      catch (Exception ex)
      {
        logger.LogError("ServiceUpdate", $"Error in {service.GetType().Name}.LateUpdate()", ex, null);
      }
    }
  }
}

// Unity MonoBehaviour wrapper following SRP
public class ServiceManager : MonoSingleton<ServiceManager>
{
  [SerializeField] private ServiceManagerConfig _configuration;

  private IServiceContainer _serviceContainer;
  private IServiceUpdater _serviceUpdater;
  private IServiceLogger _logger;
  public IServiceLogger logger
  {
    get
    {
      if (_logger == null)
      {
        _logger = new AppLogger(_configuration);
        // VerifyLogger();
      }
      return _logger;
    }
  }

  // public void VerifyLogger()
  // {
  //   _logger.Log("ServiceManager", $"[ServiceManager] Configuration status: {(_configuration != null ? "OK" : "Missing")}", this);
  //   if (_configuration != null)
  //   {
  //     _logger.Log("ServiceManager", $"[ServiceManager] Console logging enabled: {_configuration.enableConsoleLogging}", this);
  //     _logger.Log("ServiceManager", $"[ServiceManager] File logging enabled: {_configuration.enableFileLogging}", this);
  //   }
  // }

  private new void Awake()
  {
    base.Awake();
    if (_configuration == null)
    {
      Debug.LogError("Service configuration is missing!");
      return;
    }

    // Initialize core components
    // _logger = new AppLogger(_configuration);
    _serviceContainer = new ServiceContainer(logger, _configuration);
    _serviceUpdater = new ServiceUpdater(_serviceContainer, logger);

    // Run initialization in background thread
    var task = Task.Run(InitializeServices);

    try
    {
      // Wait for completion with timeout
      if (!task.Wait(TimeSpan.FromSeconds(30)))  // Adjust timeout as needed
      {
        logger.LogError("ServiceManager", "Service initialization timed out", null, this);
        return;
      }
    }
    catch (AggregateException ae)
    {
      // Handle exceptions from the task
      logger.LogError("ServiceManager", "Failed to initialize services",
          ae.InnerException ?? ae, this);
    }
    catch (Exception ex)
    {
      logger.LogError("ServiceManager", "Unexpected error during service initialization",
          ex, this);
    }
  }

  private async Task InitializeServices()
  {
    // Register core services
    RegisterCoreServices();

    // Initialize all services
    await _serviceContainer.InitializeServices();
  }

  private void RegisterCoreServices()
  {
    // Register your core services here
  }

  private void Update() => _serviceUpdater.UpdateServices();

  private void FixedUpdate() => _serviceUpdater.FixedUpdateServices();

  private void LateUpdate() => _serviceUpdater.LateUpdateServices();

  private new async void OnDestroy()
  {
    if (_serviceContainer != null)
    {
      await _serviceContainer.ShutdownServices();
    }
    base.OnDestroy();
  }

  // Public methods for other components to access services
  public T GetService<T>() where T : class => _serviceContainer?.GetService<T>();

  public void RegisterService<T>(T implementation) where T : class => _serviceContainer.RegisterService(implementation);
}

// Unity-specific logger implementation
public class UnityLogger : IServiceLogger
{
  private readonly IServiceConfiguration _configuration;

  public UnityLogger(IServiceConfiguration configuration)
  {
    _configuration = configuration;
  }

  public void Log(string tag, object message, UnityEngine.Object caller)
  {
    if (_configuration.enableConsoleLogging)
    {
      Debug.Log($"[{tag}] {message}", caller);
    }
  }

  public void LogDebug(string tag, object message, UnityEngine.Object caller)
  {
    if (_configuration.enableConsoleLogging)
    {
      Debug.Log($"[{tag}] {message}", caller);
    }
  }

  public void LogWarning(string tag, object message, UnityEngine.Object caller)
  {
    if (_configuration.enableConsoleLogging)
    {
      Debug.LogWarning($"[{tag}] {message}", caller);
    }
  }

  public void LogError(string tag, object message, Exception exception, UnityEngine.Object caller)
  {
    if (_configuration.enableConsoleLogging)
    {
      var errorMessage = exception != null
          ? $"[{tag}] {message}: {exception.Message}"
          : $"[{tag}] {message}";
      Debug.LogError(errorMessage, caller);
    }
  }

  public void LogException(string tag, object message, Exception exception, UnityEngine.Object caller)
  {
    if (_configuration.enableConsoleLogging)
    {
      var errorMessage = exception != null
          ? $"[{tag}] {message}: {exception.Message}\n{exception.StackTrace}"
          : $"[{tag}] {message}";
      Debug.LogError(errorMessage, caller);
    }
  }
}