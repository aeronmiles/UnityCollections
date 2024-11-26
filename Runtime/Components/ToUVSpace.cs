using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
public class ToUVSpace : MonoBehaviour
{
  [Header("Editor Only")]
  [SerializeField] private bool transform = false;
  private void Start()
  {
#if !UNITY_EDITOR
    var meshRenderer = GetComponent<MeshRenderer>();
    var meshFilter = GetComponent<MeshFilter>();
    meshFilter.sharedMesh = meshFilter.sharedMesh.ToUvSpace();
#endif
  }

#if UNITY_EDITOR
  private void Update()
  {
    if (transform)
    {
      transform = false;
      var meshFilter = GetComponent<MeshFilter>();
      meshFilter.sharedMesh = meshFilter.sharedMesh.ToUvSpace();
    }
  }
#endif
}
