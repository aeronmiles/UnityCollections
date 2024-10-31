using System;
using UnityEngine;

public class ID : MonoBehaviour
{
  public readonly Guid UUID = Guid.NewGuid();

  [SerializeField] private string _id;

  private void Awake()
  {
    EditorUtil.CollapseInInspector(this);
  }

  private void OnValidate()
  {
    _id = UUID.ToString();
  }

}
