using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DataStore", menuName = "UnityCollections/DataStore", order = 1)]
public class DataStoreSO : ScriptableObject
{
  public List<string> _strings = new List<string>();

  public List<string> strings
  {
    get
    {
      SetDirty();
      return _strings;
    }
  }

  public void SetDirty()
  {
#if UNITY_EDITOR
    UnityEditor.EditorUtility.SetDirty(this);
#endif
  }
}
