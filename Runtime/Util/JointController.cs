using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class JointController : MonoBehaviour
{
    [SerializeField] private Transform m_jointParent;
    [SerializeField] private string m_jointIdentifier = "Bone";
    [SerializeField] private bool m_clearJointHierarchy = false;
    [SerializeField] Vector3 m_orientToChildsRotationOffset = Vector3.zero;

    [Header("Debug")]
    [SerializeField] private bool m_showGizmos = true;

    public List<CachedTransform> CachedTransforms;

    public List<Transform> Joints;
    public List<float> JointLengths = new();
    public int Count => JointLengths.Count;

    [SerializeField]
    private List<Vector3> _prePosePositions = new();
    public List<Vector3> PrePosePositions
    {
        get
        {
            _prePosePositions.Clear();
            foreach (var t in CachedTransforms)
            {
                var pos = m_jointParent.TransformPoint(t.localPosition);
                _prePosePositions.Add(pos);
            }

            return _prePosePositions;
        }
    }

    private List<Quaternion> _prePoseRotations = new();
    public List<Quaternion> PrePoseRotations
    {
        get
        {
            _prePoseRotations.Clear();
            foreach (var t in CachedTransforms)
            {
                _prePoseRotations.Add(m_jointParent.rotation * t.localRotation);
            }

            return _prePoseRotations;
        }
    }

    private void OnDrawGizmos()
    {
        if (Joints == null || Joints.Count == 0)
            OnEnable();

        if (m_showGizmos)
            Joints.ForEach(b =>
            {
                Gizmos.color = Color.black;
                Gizmos.DrawWireSphere(b.position, 0.0035f);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(b.position, b.position + b.right * 0.01f);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(b.position, b.position + b.up * 0.01f);
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(b.position, b.position + b.forward * 0.01f);
            });
    }

    private void OnEnable()
    {
        // Set cached transforms before entering play mode
        if (!Application.isPlaying)
        {
            Joints = m_jointParent.GetComponentsInChildren<Transform>(true).Where(j => j.name.Contains(m_jointIdentifier)).ToList();
            CachedTransforms = Joints.ToCachedTransforms();
            JointLengths.Clear();
            foreach (var b in Joints)
            {
                JointLengths.Add((b.childCount > 0) ? Vector3.Distance(b.transform.position, b.GetChild(0).transform.position) : 0f);
            }
        }
        else if (m_clearJointHierarchy)
        {
            Joints.ForEach(j => j.transform.parent = m_jointParent);
            CachedTransforms = Joints.ToCachedTransforms();
        }
    }

    public void SetPosition(int index, Vector3 position)
    {
        Joints[index].position = position;
    }

    public void OrientToChilds()
    {
        int last = Joints.Count - 1;
        for (int i = 0; i < last; i++)
        {
            Vector3 direction = Joints[i + 1].position - Joints[i].position;
            Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);
            Joints[i].rotation = lookRotation * Quaternion.Euler(m_orientToChildsRotationOffset.x, m_orientToChildsRotationOffset.y, m_orientToChildsRotationOffset.z);
        }
    }

    public void Reset()
    {
        CachedTransforms.ResetTransforms();
    }
}

