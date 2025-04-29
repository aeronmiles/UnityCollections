using UnityEngine.UIElements;

public static class UIToolKitExt
{
  // Helper method to check if an element is visible in the hierarchy
  public static bool IsElementVisible(this VisualElement element)
  {
    // Check if the element itself is visible
    if (!element.visible || element.resolvedStyle.display == DisplayStyle.None ||
        element.resolvedStyle.opacity == 0)
    {
      return false;
    }

    // Check if any parent element is hidden
    var parent = element.parent;
    while (parent != null)
    {
      if (!parent.visible || parent.resolvedStyle.display == DisplayStyle.None ||
          parent.resolvedStyle.opacity == 0)
      {
        return false;
      }
      parent = parent.parent;
    }

    return true;
  }
}