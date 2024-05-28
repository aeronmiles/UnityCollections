using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Pool;

public static class MeshExt
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void FindNeighborVertices(this Mesh mesh, int vertexIndex, ref List<int> neighbours)
  {
    var set = HashSetPool<int>.Get();
    // Iterate over all the triangles (faces) in your mesh
    for (int i = 0; i < mesh.triangles.Length; i += 3)
    {
      if (mesh.triangles[i] == vertexIndex ||
          mesh.triangles[i + 1] == vertexIndex ||
          mesh.triangles[i + 2] == vertexIndex)
      {
        // Add unique vertices connected to the target vertex
        set.Add(mesh.triangles[i]);
        set.Add(mesh.triangles[i + 1]);
        set.Add(mesh.triangles[i + 2]);
      }
    }
    _ = set.Remove(vertexIndex);
    neighbours.Clear();
    neighbours.AddRange(set);
    HashSetPool<int>.Release(set);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Vector3 NormalsAveraged(this Mesh mesh, int[] indices, int meshDataIndex)
  {
    // @TODO: remove memory allocations
    using (var meshData = Mesh.AcquireReadOnlyMeshData(mesh))  // Request index 0 for vertex data
    {
      var mesh0 = meshData[meshDataIndex];
      NativeArray<Vector3> normals = new NativeArray<Vector3>(mesh0.vertexCount, Allocator.Temp);
      meshData[meshDataIndex].GetNormals(normals);
      Vector3 indexNormals = Vector3.zero;
      for (int i = 0; i < indices.Length; i++)
      {
        indexNormals += normals[indices[i]];
      }
      normals.Dispose();
      return indexNormals.normalized;
    } // MeshData is released automatically here
  }
}