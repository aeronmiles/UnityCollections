using UnityEngine.UIElements;
using System.Text;
using System.Linq;

public static class VisualElementExt
{
  public static bool HasParentOfType<T>(this VisualElement element) where T : VisualElement
  {
    if (element.parent == null)
    {
      return false;
    }
    else if (element.parent is T)
    {
      return true;
    }
    else
    {
      return element.parent.HasParentOfType<T>();
    }
  }

  public static T FindByName<T>(this VisualElement root, UnityEngine.Object caller, string name = null) where T : VisualElement
  {
    if (root == null)
    {
      UnityEngine.Debug.LogError("[VisualElementExt] FindElement() :: root is null", caller);
      return null;
    }
    if (!string.IsNullOrEmpty(name))
    {
      var el = root.Q<T>(name);
      if (el == null)
      {
        UnityEngine.Debug.LogError("[VisualElementExt] FindElement() element not found with name: " + name, caller);
      }
      return el;
    }
    UnityEngine.Debug.LogError("[VisualElementExt] FindElement() :: no name reference provided", caller);
    return null;
  }

  public static T FindByClassName<T>(this VisualElement root, UnityEngine.Object caller, string className = null) where T : VisualElement
  {
    if (root == null)
    {
      UnityEngine.Debug.LogError("[VisualElementExt] FindElement() :: root is null", caller);
      return null;
    }
    if (!string.IsNullOrEmpty(className))
    {
      var el = root.Q<T>(className: className);
      if (el == null)
      {
        UnityEngine.Debug.LogError("[VisualElementExt] FindElement() element not found with class name: " + className, caller);
      }
      return el;
    }
    UnityEngine.Debug.LogError("[VisualElementExt] FindElement() :: no class reference provided", caller);
    return null;
  }

  public static bool HasActiveClass(this VisualElement el) => el.ClassListContains("active");

  public static string HierarchyToString(this VisualElement element, int indent = 0)
  {
#if DEBUG
    if (element == null) return string.Empty;

    StringBuilder sb = new StringBuilder();
    string indentString = new string(' ', indent * 2);

    // Print element type and name
    sb.AppendLine($"{indentString}{element.GetType().Name} (Name: '{element.name}')");

    // Print classes
    var classes = element.GetClasses().ToList();
    if (classes.Count > 0)
    {
      sb.AppendLine($"{indentString}Classes: {string.Join(", ", classes)}");
    }

    // Print style properties
    var computedStyle = element.resolvedStyle;
    sb.AppendLine($"{indentString}Style Properties:");
    sb.AppendLine($"{indentString}  Position: {element.style.position.value}");
    sb.AppendLine($"{indentString}  Width: {computedStyle.width}");
    sb.AppendLine($"{indentString}  Height: {computedStyle.height}");
    sb.AppendLine($"{indentString}  Visibility: {computedStyle.visibility}");
    sb.AppendLine($"{indentString}  Display: {computedStyle.display}");
    sb.AppendLine($"{indentString}  Opacity: {computedStyle.opacity}");
    // sb.AppendLine($"{indentString}  Flex Direction: {computedStyle.flexDirection}");
    // sb.AppendLine($"{indentString}  Justify Content: {computedStyle.justifyContent}");
    // sb.AppendLine($"{indentString}  Align Items: {computedStyle.alignItems}");
    // sb.AppendLine($"{indentString}  Align Self: {computedStyle.alignSelf}");
    // sb.AppendLine($"{indentString}  Flex Grow: {computedStyle.flexGrow}");
    // sb.AppendLine($"{indentString}  Flex Shrink: {computedStyle.flexShrink}");
    // sb.AppendLine($"{indentString}  Margin: {computedStyle.marginLeft}, {computedStyle.marginTop}, {computedStyle.marginRight}, {computedStyle.marginBottom}");
    // sb.AppendLine($"{indentString}  Padding: {computedStyle.paddingLeft}, {computedStyle.paddingTop}, {computedStyle.paddingRight}, {computedStyle.paddingBottom}");
    // sb.AppendLine($"{indentString}  Border Width: {computedStyle.borderLeftWidth}, {computedStyle.borderTopWidth}, {computedStyle.borderRightWidth}, {computedStyle.borderBottomWidth}");

    // Print picking mode and enabled state
    sb.AppendLine($"{indentString}Picking Mode: {element.pickingMode}");
    sb.AppendLine($"{indentString}Enabled In Hierarchy: {element.enabledInHierarchy}");
    sb.AppendLine($"{indentString}Visible: {element.visible}");

    // Print any userData
    if (element.userData != null)
    {
      sb.AppendLine($"{indentString}UserData: {element.userData}");
    }

    // Print text content if applicable
    if (element is TextElement textElement)
    {
      sb.AppendLine($"{indentString}Text: {textElement.text}");
    }

    // Print tooltip if applicable
    if (!string.IsNullOrEmpty(element.tooltip))
    {
      sb.AppendLine($"{indentString}Tooltip: {element.tooltip}");
    }

    // Print children recursively
    foreach (var child in element.Children())
    {
      sb.Append(HierarchyToString(child, indent + 1));
    }

    return sb.ToString();
#else
    return "HierarchyToString is only available in DEBUG builds";
#endif
  }
}
