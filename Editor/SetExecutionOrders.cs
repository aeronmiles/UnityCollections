#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SetExecutionOrders
{
  // Static constructor is called when the editor loads or recompiles scripts
  static SetExecutionOrders()
  {
    // AM.Unity.Collections
    SetExecutionOrder("ServiceManager", -95);
  }

  private static void SetExecutionOrder(string scriptName, int order)
  {
    // Find the script by its name
    MonoScript[] scripts = (MonoScript[])Resources.FindObjectsOfTypeAll(typeof(MonoScript));
    foreach (MonoScript script in scripts)
    {
      if (script.name == scriptName)
      {
        int currentOrder = MonoImporter.GetExecutionOrder(script);
        if (currentOrder != order)
        {
          MonoImporter.SetExecutionOrder(script, order);
          Debug.Log($"Execution order for {scriptName} set to {order}.");
        }
        else
        {
          Debug.Log($"Execution order for {scriptName} is already set to {order}.");
        }
        return;
      }
    }

    Debug.LogWarning($"Script with name {scriptName} not found.");
  }
}
#endif