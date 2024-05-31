#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshFilter))]
public class MeshInfo : Editor
{
  // void OnSceneGUI()
  // {
  //     MeshFilter meshFilter = target as MeshFilter;
  //     if (meshFilter != null && meshFilter.sharedMesh != null)
  //     {
  //         ShowVertexIndices(meshFilter);
  //     }
  // }

  private void ShowVertexIndices(MeshFilter meshFilter)
  {
    Mesh mesh = meshFilter.sharedMesh;
    Vector3[] vertices = mesh.vertices;

    // Transform vertex position from local space to world space
    Transform transform = meshFilter.transform;

    // Set style for labels
    GUIStyle labelStyle = new GUIStyle();
    labelStyle.normal.textColor = Color.white;
    labelStyle.fontSize = 10;

    for (int i = 0; i < vertices.Length; i++)
    {
      Vector3 vertexWorldPosition = transform.TransformPoint(vertices[i]);
      Handles.Label(vertexWorldPosition, $" {i}", labelStyle);
    }
  }
}
#endif