using UnityEngine;

[ExecuteAlways]
public class SetVisibility : MonoBehaviour
{
  [SerializeField] bool _visibleInEditor = true;
#pragma warning disable CS0414 // Add accessibility modifiers
  [SerializeField] bool _visibleAtRuntime = true;
#pragma warning restore CS0414 // Add accessibility modifiers

  Renderer _Renderer;
  Renderer m_Renderer
  {
    get
    {
      if (!_Renderer) _Renderer = GetComponent<Renderer>();
      return _Renderer;
    }
  }

  private void OnEnable()
  {
#if UNITY_EDITOR
    m_Renderer.enabled = m_Renderer.enabled && _visibleInEditor;
#else
        if (Application.isPlaying)
        {
            m_Renderer.enabled = _visibleAtRuntime;
            Debug.Log("SetVisibility -> OnEnable() :: Set Runtime Renderer enabled: " + _visibleInEditor);
        }
#endif
  }

#if UNITY_EDITOR
  private void OnValidate()
  {
    m_Renderer.enabled = _visibleInEditor;
    Debug.Log("SetVisibility -> OnValidate() :: Set Editor Renderer enabled: " + _visibleInEditor);
  }
#endif
}
