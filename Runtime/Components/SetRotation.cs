using UnityEngine;

public class SetRotation : MonoBehaviour
{
  [SerializeField] private Vector3 _rotation;
  private void OnEnable()
  {
    transform.localEulerAngles = _rotation;
  }
}
