// Implementation of service configuration
using UnityEngine;

[CreateAssetMenu(fileName = "ServiceManagerConfig", menuName = "Services/ServiceManagerConfig")]
public class ServiceManagerConfig : ScriptableObject, IServiceConfiguration
{
#pragma warning disable IDE0032 // Use auto property
  [Tooltip("Enable console logging, in Debug always enabled")]
  [SerializeField] private bool _enableConsoleLogging = true;
  [Tooltip("Enable file logging, in Debug always enabled")]
  [SerializeField] private bool _enableFileLogging = true;
  [SerializeField] private float _initializationTimeout = 30f;
  [SerializeField] private int _serviceInitializationRetryCount = 3;
#pragma warning restore IDE0032 // Use auto property

  public bool enableConsoleLogging => _enableConsoleLogging;
  public bool enableFileLogging => _enableFileLogging;
  public float initializationTimeout => _initializationTimeout;
  public int serviceInitializationRetryCount => _serviceInitializationRetryCount;

  public void SetAsDirty()
  {
#if UNITY_EDITOR
    UnityEditor.EditorUtility.SetDirty(this);
#endif
  }

  private void OnValidate() => SetAsDirty();
}
