using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SetVisibility : MonoBehaviour
{
    [SerializeField] bool m_VisibleInEditor = true;
    [SerializeField] bool m_VisibleAtRuntime = true;

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
        m_Renderer.enabled = m_Renderer.enabled && m_VisibleInEditor;
#else
        if (Application.isPlaying)
        {
            m_Renderer.enabled = m_VisibleAtRuntime;
            Debug.Log("SetVisibility -> OnEnable() :: Set Runtime Renderer enabled: " + m_VisibleInEditor);
        }
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        m_Renderer.enabled = m_VisibleInEditor;
        Debug.Log("SetVisibility -> OnValidate() :: Set Editor Renderer enabled: " + m_VisibleInEditor);
    }
#endif
}
