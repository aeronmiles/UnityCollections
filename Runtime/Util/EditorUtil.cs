#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

#if UNITY_EDITOR
  // Editor helpers for selecting the previewed display in Game View.
  // Uses reflection against internal UnityEditor.GameView fields, which can vary by Unity version.
  private static readonly BindingFlags _bf = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
  private static Type EditorGameViewType => Type.GetType("UnityEditor.GameView,UnityEditor");
  private static readonly Stack<int> _gameViewDisplayStack = new Stack<int>();

  public static void PushGameViewSelectedDisplay(int display)
  {
    var t = EditorGameViewType;
    if (t == null) return;
    var gv = Resources.FindObjectsOfTypeAll(t).Cast<object>().FirstOrDefault();
    if (gv == null) return;

    var f = t.GetField("m_TargetDisplay", _bf) ?? t.GetField("m_SelectedDisplay", _bf);
    if (f == null) return;

    try
    {
      var current = (int)f.GetValue(gv);
      _gameViewDisplayStack.Push(current);
      var max = Mathf.Max(0, (Display.displays?.Length ?? 1) - 1);
      var clamped = Mathf.Clamp(display, 0, max);
      f.SetValue(gv, clamped);
      var repaint = t.GetMethod("Repaint", _bf);
      repaint?.Invoke(gv, null);
    }
    catch (Exception ex)
    {
      Debug.LogError($"Failed to set GameViewSelectedDisplay to: {display}, exception: {ex}");
    }
  }

  public static void PopGameViewSelectedDisplay()
  {
    if (_gameViewDisplayStack.Count == 0) return;
    var prev = _gameViewDisplayStack.Pop();
    var t = EditorGameViewType;
    if (t == null) return;
    var gv = Resources.FindObjectsOfTypeAll(t).Cast<object>().FirstOrDefault();
    if (gv == null) return;
    var f = t.GetField("m_TargetDisplay", _bf) ?? t.GetField("m_SelectedDisplay", _bf);
    if (f == null) return;
    try
    {
      f.SetValue(gv, prev);
      var repaint = t.GetMethod("Repaint", _bf);
      repaint?.Invoke(gv, null);
    }
    catch (Exception ex)
    {
      Debug.LogError($"Failed to Pop GameViewSelectedDisplay to: {prev}, exception: {ex}");
    }
  }
#endif
}
