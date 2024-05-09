using System;
using UnityEngine;

[ExecuteInEditMode]
public class RenderTargetManager : MonoSingletonScene<RenderTargetManager>
{
  [SerializeField] private bool _renderAll;
  [SerializeField] private RenderTarget[] _renderTargets;

  private void OnValidate()
  {
    foreach (var rt in _renderTargets)
    {
      rt.OnValidate();
    }
  }

  private void Update()
  {
#if UNITY_EDITOR
    foreach (var rt in _renderTargets)
    {
      if (rt.RenderToTarget || _renderAll)
      {
        _ = rt.Render(out _);
        rt.RenderToTarget = false;
      }
    }
    _renderAll = false;
#endif
  }

  public bool Render(string id, out RenderTexture rtOut)
  {
    var rt = Array.Find(_renderTargets, rt => rt.id == id);
    if (rt != null)
    {
      return rt.Render(out rtOut);
    }
    rtOut = new RenderTexture(1, 1, 24);
    return false;
  }

  [Serializable]
  public abstract class RenderTargetBase : IID
  {
    public string _id;
    public string id => _id;
    public Renderer sourceRenderer;
    public bool cropToSourceRenderer;
    public Vector3 sourceRendererScale = Vector3.one;
    public bool linear = true;
    public GameObjectActiveState[] activeStates;
    public Camera camera;
    public Material blitMaterial;

    [Header("Texture Settings")]
    public RenderTexture renderTexture;
    public int padding = 0;

    [Header("Debug")]
    public bool RenderToTarget;

    protected virtual void PreRender()
    {
      activeStates.SetStates();
    }

    protected virtual void PostRender()
    {
      activeStates.ResetStates();
    }

    protected abstract bool RenderToTexture(out RenderTexture rtOut);

    public virtual void OnValidate()
    {
      if (blitMaterial != null)
      {
        Debug.LogWarning("Blit materials array length is greater than 1. Minimize to 1 for performance.");
      }
      if (camera == null)
      {
        camera = Camera.main;
      }
      if (renderTexture == null)
      {
        renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
      }
      RenderToTarget = true;
    }

    private int _lastFrame = -1;

    public bool Render(out RenderTexture rtOut)
    {
      if (_lastFrame == Time.frameCount)
      {
        rtOut = renderTexture;
        return true;
      }

      PreRender();
      var result = RenderToTexture(out rtOut);
      PostRender();

      _lastFrame = Time.frameCount;
      return result;
    }

    public bool Render(RenderTexture rtOut)
    {
      var currentRT = renderTexture;
      renderTexture = rtOut;
      PreRender();
      var result = RenderToTexture(out rtOut);
      PostRender();
      renderTexture = currentRT;
      return result;
    }
  }

  [Serializable]
  public class RenderTarget : RenderTargetBase
  {
    public RenderTarget(string id, Renderer sourceRenderer, bool linear, GameObjectActiveState[] activeStates, Camera camera, Material blitMaterial, RenderTexture renderTexture, int padding, Vector3 scale)
    {
      this._id = id;
      this.sourceRenderer = sourceRenderer;
      this.linear = linear;
      this.activeStates = activeStates;
      this.camera = camera;
      this.blitMaterial = blitMaterial;
      this.renderTexture = renderTexture;
      this.padding = padding;
      this.sourceRendererScale = scale;
    }

    public override void OnValidate()
    {
      base.OnValidate();
    }

    protected override bool RenderToTexture(out RenderTexture rtOut)
    {
      bool result;
      if (sourceRenderer != null)
      {
        if (cropToSourceRenderer)
        {
          result = RenderCropped();
        }
        else
        {
          result = RenderScreen();
        }
      }
      else
      {
        result = camera.Blit(ref renderTexture, blitMaterial, linear);
      }
      rtOut = renderTexture;
      return result;
    }

    protected bool RenderCropped()
    {
      Vector3 _scale = sourceRenderer.transform.localScale;
      sourceRenderer.transform.localScale = sourceRenderer.transform.localScale.Multiply(sourceRendererScale);

      // @TODO: Add support for multiple blit materials
      var mat = sourceRenderer.sharedMaterial;
      if (blitMaterial != null)
      {
        sourceRenderer.sharedMaterial = blitMaterial;
      }
      var result = camera.BlitCroppedToScreenBounds(ref renderTexture, sourceRenderer, null, padding, linear);

      // camera.BlitCroppedToTarget(ref renderTexture, sourceRenderer, null, padding);
      // _tex = camera.BlitCroppedToScreenBounds(sourceRenderer, null, 256, padding);
      // Graphics.Blit(_tex, renderTexture);

      // Cleanup
      sourceRenderer.sharedMaterial = mat;
      sourceRenderer.transform.localScale = _scale;
      return result;
    }

    private bool RenderScreen()
    {
      bool result;
      var mat = sourceRenderer.sharedMaterial;
      sourceRenderer.sharedMaterial = blitMaterial;
      result = camera.Blit(ref renderTexture, null, linear);
      sourceRenderer.sharedMaterial = mat;
      return result;
    }
  }
}