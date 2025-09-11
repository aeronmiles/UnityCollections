using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// This class tells Unity how to draw any field marked with the [ReadOnly] attribute.
/// </summary>
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyAttributeDrawer : PropertyDrawer
{
  // Override the OnGUI method to draw the property
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
  {
    // Save the previous GUI state
    bool previousGUIState = GUI.enabled;

    // Disable editing
    GUI.enabled = false;

    // Draw the property field as a disabled field
    EditorGUI.PropertyField(position, property, label);

    // Restore the previous GUI state
    GUI.enabled = previousGUIState;
  }
}
#endif