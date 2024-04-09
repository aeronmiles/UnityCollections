using Unity.Collections;
using UnityEngine;

public static class BoundsExt
{
  public static Rect ToRect(this Bounds objectBounds)
  {
    return new Rect(objectBounds.min.x, objectBounds.min.y, objectBounds.size.x, objectBounds.size.y);
  }

  public static bool InScreenNonZero(this Bounds bounds)
  {
    return bounds.ToRect().InScreenNonZero();
  }

  public static bool InScreenNonZero(this Rect bounds)
  {
    if (bounds.x < 0 ||
        bounds.x + bounds.width > Screen.width ||
        bounds.y < 0 ||
        bounds.y + bounds.height > Screen.height ||
        bounds.width < 1 || bounds.height < 1)
    {
      return false;
    }

    return true;
  }

  public static NativeArray<Vector3> CornerPositions(this Bounds objectBounds)
  {
    Vector3 center = objectBounds.center;
    Vector3 extents = objectBounds.extents;
    NativeArray<Vector3> corners = new NativeArray<Vector3>(8, Allocator.Temp);
    corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
    corners[1] = center + new Vector3(-extents.x, -extents.y, extents.z);
    corners[2] = center + new Vector3(-extents.x, extents.y, -extents.z);
    corners[3] = center + new Vector3(-extents.x, extents.y, extents.z);
    corners[4] = center + new Vector3(extents.x, -extents.y, -extents.z);
    corners[5] = center + new Vector3(extents.x, -extents.y, extents.z);
    corners[6] = center + new Vector3(extents.x, extents.y, -extents.z);
    corners[7] = center + new Vector3(extents.x, extents.y, extents.z);
    return corners;
  }
}