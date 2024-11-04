using UnityEngine;
using UnityEngine.Events;

public class MonoEvents : MonoBehaviour
{
  [SerializeField] private UnityEvent _onEnable;
  [SerializeField] private UnityEvent _onDisable;
  [SerializeField] private UnityEvent _onStart;
  // [SerializeField] private UnityEvent _onUpdate;
  // [SerializeField] private UnityEvent _onFixedUpdate;
  // [SerializeField] private UnityEvent _onLateUpdate;
  [SerializeField] private UnityEvent _onDestroy;
  private void OnEnable()
  {
    _onEnable?.Invoke();
  }

  private void OnDisable()
  {
    _onDisable?.Invoke();
  }

  private void Start()
  {
    _onStart?.Invoke();
  }

  private void OnDestroy()
  {
    _onDestroy?.Invoke();
  }
}
