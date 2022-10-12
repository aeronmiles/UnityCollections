using UnityEngine;

[CreateAssetMenu(fileName = "PointCacheData", menuName = "Data/PointCacheData", order = 1)]
[PreferBinarySerialization]
public class PointCacheData : ScriptableObject
{
    public Vector3[] Points;
}
