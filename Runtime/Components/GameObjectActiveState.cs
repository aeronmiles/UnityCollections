using System;
using UnityEngine;

[Serializable]
public class GameObjectActiveState
{
  public GameObject gameObject;
  public CanvasRenderer canvasRenderer;
  public Renderer renderer;
  public bool active;
  private bool _initialState;

  public void SetState()
  {
    if (gameObject != null)
    {
      _initialState = gameObject.activeSelf;
      gameObject.SetActive(active);
    }
    if (renderer != null)
    {
      _initialState = renderer.enabled;
      renderer.enabled = active;
    }
    if (canvasRenderer != null)
    {
      _initialState = canvasRenderer.GetAlpha() > 0;
      canvasRenderer.SetAlpha(active ? 1 : 0);
    }
  }

  public void ResetState()
  {
    if (gameObject != null)
    {
      gameObject.SetActive(_initialState);
    }
    if (renderer != null)
    {
      renderer.enabled = _initialState;
    }
    if (canvasRenderer != null)
    {
      canvasRenderer.SetAlpha(_initialState ? 1 : 0);
    }
  }
}

public static class GameObjectActiveStateExt
{
  public static void SetStates(this GameObjectActiveState[] states)
  {
    foreach (var state in states)
    {
      state.SetState();
    }
  }

  public static void ResetStates(this GameObjectActiveState[] states)
  {
    foreach (var state in states)
    {
      state.ResetState();
    }
  }
}