using UnityEngine;

public class DisableIfNotEditor : MonoBehaviour
{
  // Start is called before the first frame update
  void Start()
  {
#if !UNITY_EDITOR
    gameObject.SetActive(false);
#endif
  }
}
