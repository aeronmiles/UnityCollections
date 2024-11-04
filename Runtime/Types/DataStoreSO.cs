using System.Collections.Generic;
using System.Linq;
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

  public new void SetDirty() =>
#if UNITY_EDITOR
    UnityEditor.EditorUtility.SetDirty(this);
#endif


  public void RemoveDuplicates()
  {
    _strings = _strings.Distinct().ToList();
    SetDirty();
  }
}
