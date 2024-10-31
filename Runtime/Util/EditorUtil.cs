#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
#endif
using UnityEngine;

public static class EditorUtil
{
  /// <summary>
  /// Collapses the component in the Unity Inspector.
  /// </summary>
  /// <param name="component">The MonoBehaviour component to collapse in the inspector.</param>
  public static void CollapseInInspector(MonoBehaviour component)
  {
#if UNITY_EDITOR

    // Start a coroutine to collapse the component in the inspector
    component.StartCoroutine(CollapseInspectorCoroutine(component));
#endif
  }

#if UNITY_EDITOR
  private static IEnumerator CollapseInspectorCoroutine(MonoBehaviour component)
  {
    // Temporarily hide the component from the inspector
    component.hideFlags = HideFlags.HideInInspector;

    // Wait for the end of the frame to ensure the inspector updates
    yield return new WaitForEndOfFrame();

    // Restore the visibility of the component, but it will be collapsed
    component.hideFlags = HideFlags.None;

    // Ensure the inspector is refreshed
    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
  }
#endif

  public static void AddNewTag(string newTag)
  {
#if UNITY_EDITOR
    // Get the tag manager asset
    SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

    SerializedProperty tagsProp = tagManager.FindProperty("tags");

    // Check if the tag already exists
    bool found = false;
    for (int i = 0; i < tagsProp.arraySize; i++)
    {
      SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
      if (t.stringValue.Equals(newTag)) { found = true; break; }
    }

    // If the tag is not found, add it
    if (!found)
    {
      tagsProp.InsertArrayElementAtIndex(0);
      SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
      newTagProp.stringValue = newTag;
      tagManager.ApplyModifiedProperties();
      Debug.Log("Tag: " + newTag + " has been added successfully.");
    }
    else
    {
      Debug.LogWarning("Tag: " + newTag + " already exists.");
    }
#endif
  }
}
