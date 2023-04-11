using UnityEngine;

[CreateAssetMenu(fileName = "PointCache", menuName = "Data/PointCache", order = 1)]
[PreferBinarySerialization]
public class PointCache : ScriptableObject
{
    public Vector3[] Points;
}
