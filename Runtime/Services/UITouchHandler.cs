using UnityEngine;
using System;

public class UITouchHandler : MonoSingleton<UITouchHandler>
{
  public event Action<Vector2> OnTouchBegan;
  public event Action<Vector2> OnTouchEnd;

  // Optional parameters for touch mapping
  public bool shouldInvertY = true;
  public bool useDisplayHeight = true;
  public float coolOffPeriod { get; private set; } = 0.15f;
  private float _lastInteractionCoolOffTime = 0f;
  public float lastInteractionCoolOffTime
  {
    get => _lastInteractionCoolOffTime;
    private set
    {
      if (value < 0)
      {
        Debug.LogWarning("UITouchHandler :: Invalid cool-off time, using default");
        _lastInteractionCoolOffTime = Time.time;
        return;
      }
      _lastInteractionCoolOffTime = value;
    }
  }

  public void SetCoolOffPeriod(float period, UnityEngine.Object caller)
  {
    ServiceManager.I.logger.Log("UITouchHandler", "Setting CoolOffPeriod: " + period, caller);
    coolOffPeriod = period;
  }

  private Vector2 _pointerDownPosition;
  public Vector2 pointerDownPosition => _pointerDownPosition;
  private Vector2 _pointerUpPosition;
  public Vector2 pointerUpPosition => _pointerUpPosition;

  private bool GuardAgainstMultipleInteractions()
  {
    // Guard against multiple interactions
    if ((Time.time - lastInteractionCoolOffTime) < coolOffPeriod)
    {
      // Debug.LogWarning("UITouchHandler :: Interaction cool-off period");
      return false;
    }
    lastInteractionCoolOffTime = Time.time;
    return true;
  }

  private float _lastSetCoolOffTime = 0f;
  public void SetLastInteractionCoolOffTime(UnityEngine.Object caller)
  {
    if (Time.time - _lastSetCoolOffTime < coolOffPeriod)
    {
      ServiceManager.I.logger.LogWarning("UITouchHandler", $"Resetting cool-off period before cool-off has elapsed", caller);
    }
    _lastSetCoolOffTime = Time.time;
    lastInteractionCoolOffTime = _lastSetCoolOffTime;
  }

  private void LateUpdate()
  {
    // Use Input touches as multi-display touch events are not working
    if (InputHelper.GetTouches().Count > 0)
    {
      var touch = InputHelper.GetTouches()[0];
      _ = HandleTouch(touch.position, touch.phase);
    }
  }

  private int _totalHeight = 0;
  // @TODO: Validate this base implementation
  public virtual Vector2 MapTouchToDisplay(Vector2 touchPosition)
  {
    Debug.LogWarning("UITouchHandler :: MapTouchToDisplay :: Touch mapping needs validation");
    if (!shouldInvertY)
    {
      return touchPosition;
    }

    if (Display.displays.Length == 2)
    {
      if (useDisplayHeight && _totalHeight == 0)
      {
        _totalHeight = Display.displays[0].renderingHeight + Display.displays[1].renderingHeight;
      }
#if UNITY_EDITOR
      _totalHeight = Screen.height;
#endif
    }
    else if (Display.displays.Length > 2)
    {
      Debug.LogWarning("UITouchHandler :: More than 2 displays detected, touch mapping may not implemented");
    }

    var touchPos = new Vector2(touchPosition.x, _totalHeight > 0 ? _totalHeight - touchPosition.y : touchPosition.y);

#if DEBUG
    Debug.Log($"UITouchHandler :: Screen w:{Screen.width} h:{Screen.height}, total height: {_totalHeight} touchPos: {touchPos}");
    for (int i = 0; i < Display.displays.Length; i++)
    {
      var di = Display.displays[i];
      Debug.Log($"UITouchHandler :: display {i} w:{di.renderingWidth} h:{di.renderingHeight}");
    }
#endif

    return touchPos;
  }

  public bool HandleTouch(Vector2 touchPosition, TouchPhase phase)
  {
    switch (phase)
    {
      case TouchPhase.Began:
        return HandleTouchBegan(touchPosition);
      case TouchPhase.Ended:
        return HandleTouchEnded(touchPosition);
      default:
        return false;
    }
  }

  private bool HandleTouchBegan(Vector2 position)
  {
    if (!GuardAgainstMultipleInteractions())
    {
      return false;
    }
    var mappedPosition = MapTouchToDisplay(position);

    _pointerDownPosition = mappedPosition;
    OnTouchBegan?.Invoke(mappedPosition);
    return true;
  }

  private bool HandleTouchEnded(Vector2 position)
  {
    var mappedPosition = MapTouchToDisplay(position);
    _pointerUpPosition = mappedPosition;
    OnTouchEnd?.Invoke(mappedPosition);
    return true;
  }

  public Vector2 GetSwipeDirection() => _pointerUpPosition - _pointerDownPosition;

  public bool IsHorizontalSwipe()
  {
    Vector2 direction = GetSwipeDirection();
    return Mathf.Abs(direction.x) > Mathf.Abs(direction.y);
  }

  public bool isRightSwipe => IsHorizontalSwipe() && GetSwipeDirection().x > 0;

  public bool isLeftSwipe => IsHorizontalSwipe() && GetSwipeDirection().x < 0;

  public bool isUpSwipe => !IsHorizontalSwipe() && GetSwipeDirection().y > 0;

  public bool isDownSwipe => !IsHorizontalSwipe() && GetSwipeDirection().y < 0;
}