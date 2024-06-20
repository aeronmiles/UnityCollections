using UnityEngine;
using System.Collections.Generic;

public class InputHelper : MonoBehaviour
{
  private static TouchCreator lastFakeTouch;

  public static Touch GetTouches(int index)
  {
    List<Touch> touches = new List<Touch>();
    touches.AddRange(Input.touches);
    if (touches.Count > 0) return touches[index];

    if (lastFakeTouch == null) lastFakeTouch = new TouchCreator();
    if (Input.GetMouseButtonDown(0))
    {
      lastFakeTouch.phase = TouchPhase.Began;
      lastFakeTouch.deltaPosition = new Vector2(0, 0);
      lastFakeTouch.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
      lastFakeTouch.fingerId = 0;
    }
    else if (Input.GetMouseButtonUp(0))
    {
      lastFakeTouch.phase = TouchPhase.Ended;
      Vector2 newPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
      lastFakeTouch.deltaPosition = newPosition - lastFakeTouch.position;
      lastFakeTouch.position = newPosition;
      lastFakeTouch.fingerId = 0;
    }
    else if (Input.GetMouseButton(0))
    {
      lastFakeTouch.phase = TouchPhase.Moved;
      Vector2 newPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
      lastFakeTouch.deltaPosition = newPosition - lastFakeTouch.position;
      lastFakeTouch.position = newPosition;
      lastFakeTouch.fingerId = 0;
    }
    else
    {
      lastFakeTouch = null;
    }
    if (lastFakeTouch != null) touches.Add(lastFakeTouch.Create());

    return touches[index];
  }

  public static List<Touch> GetTouches()
  {
    List<Touch> touches = new List<Touch>();
    touches.AddRange(Input.touches);
    if (touches.Count > 0) return touches;

    if (lastFakeTouch == null) lastFakeTouch = new TouchCreator();
    if (Input.GetMouseButtonDown(0))
    {
      lastFakeTouch.phase = TouchPhase.Began;
      lastFakeTouch.deltaPosition = new Vector2(0, 0);
      lastFakeTouch.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
      lastFakeTouch.fingerId = 0;
    }
    else if (Input.GetMouseButtonUp(0))
    {
      lastFakeTouch.phase = TouchPhase.Ended;
      Vector2 newPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
      lastFakeTouch.deltaPosition = newPosition - lastFakeTouch.position;
      lastFakeTouch.position = newPosition;
      lastFakeTouch.fingerId = 0;
    }
    else if (Input.GetMouseButton(0))
    {
      lastFakeTouch.phase = TouchPhase.Moved;
      Vector2 newPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
      lastFakeTouch.deltaPosition = newPosition - lastFakeTouch.position;
      lastFakeTouch.position = newPosition;
      lastFakeTouch.fingerId = 0;
    }
    else
    {
      lastFakeTouch = null;
    }
    if (lastFakeTouch != null) touches.Add(lastFakeTouch.Create());

    return touches;
  }

  public static bool TryGetTouchPosition(out Vector2 touchPosition)
  {
    if (Input.touchCount > 0)
    {
      touchPosition = Input.GetTouch(0).position;
      return true;
    }
    else if (Input.GetMouseButton(0))
    {
      var mousePosition = Input.mousePosition;
      touchPosition = new Vector2(mousePosition.x, mousePosition.y);
      return true;
    }
    touchPosition = default;
    return false;
  }
}