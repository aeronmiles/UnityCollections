using UnityEngine;

public class ResetTransformsOnEnable : MonoBehaviour
{
    [SerializeField] bool m_resetPosition = true;
    [SerializeField] bool m_resetRotation = true;
    [SerializeField] bool m_resetScale = false;
    
    private void OnEnable()
    {
        Debug.Log($"ResetTransformToOrigin -> OnEnable() :: Resetting transforms :: position: {m_resetPosition}, rotation: {m_resetRotation}, scale: {m_resetScale}", this);
        transform.SetPositionAndRotation(m_resetPosition ? Vector3.zero : transform.position, m_resetRotation ? Quaternion.identity : transform.rotation);
        if (m_resetScale) transform.localScale = Vector3.one;
    }
}
