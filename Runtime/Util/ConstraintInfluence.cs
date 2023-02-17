using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ConstraintInfluence : MonoBehaviour
{
    public Vector2 PathInfluencePositions = new Vector2(0f, 1f);
    public AnimationCurve PathInfluenceFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    public float Radius = 1f;
    public AnimationCurve Falloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    public int Priority = 0;
    [Header("Debug")]
    [SerializeField] bool m_showGizmos = true;

    private void OnDrawGizmos()
    {
        if (!m_showGizmos)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, Radius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Radius * Falloff.keys[0].time);
    }

    public float GetInfluence(Vector3 worldPos)
    {
        float distance = Vector3.Distance(transform.position, worldPos);
        float t = Mathf.Clamp01(distance / Radius);
        return Falloff.Evaluate(t);
    }

    public float GetPathOffset(Vector3 worldPos)
    {
        float distance = Vector3.Distance(transform.position, worldPos);
        return Mathf.Lerp(PathInfluencePositions.x, PathInfluencePositions.y, PathInfluenceFalloff.Evaluate(Mathf.Clamp01(distance / Radius)));
    }
}

public static class ConstraintInfluenceExt
{
    public static float GetSummedClamped(this IEnumerable<ConstraintInfluence> influences, Vector3 worldPos)
    {
        float sum = 0f;
        foreach (var i in influences)
            sum += i.GetInfluence(worldPos);

        return Mathf.Clamp01(sum);
    }

    public static ConstraintInfluence GetClosestInfluence(this IEnumerable<ConstraintInfluence> influences, Vector3 worldPos)
    {
        ConstraintInfluence closest = null;
        float closestDistance = float.MaxValue;
        foreach (var i in influences)
        {
            float distance = Vector3.Distance(i.transform.position, worldPos);
            if (distance < closestDistance)
            {
                closest = i;
                closestDistance = distance;
            }
        }

        return closest;
    }
}