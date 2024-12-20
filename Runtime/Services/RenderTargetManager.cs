using System;
using System.Collections.Generic;
using UnityEngine;

// @TODO: Implement as service
[ExecuteInEditMode]
public class RenderTargetManager : MonoSingletonScene<RenderTargetManager>
{
  [Header("Debug")]
  [SerializeField] private bool _renderAll;

  [Header("Render Targets")]
  [SerializeField] private RenderTarget[] _renderTargets;
  private void OnValidate() => Validate();

  private void Start() => Validate();

  private void Validate()
  {
    foreach (var rt in _renderTargets)
    {
      rt.Validate();
    }
  }

#if UNITY_EDITOR
  private void Update()
  {
    foreach (var rt in _renderTargets)
    {
      if (rt.RenderToTarget || _renderAll)
      {
        _ = rt.Render(out _);
        rt.RenderToTarget = false;
        Debug.Log($"RenderTargetManager :: {rt.id} rendered: {rt.RenderToTarget}, {_renderAll}");
      }
    }
    _renderAll = false;
  }
#endif

  // @TODO: Implement error handling  
  public bool Render(string id, out RenderTexture rtOut)
  {
    foreach (var r in _renderTargets)
    {
      if (r.id == id)
      {
        return r.Render(out rtOut);
      }
    }
    rtOut = null;
    return false;
  }

  public int Width(string id)
  {
    foreach (var r in _renderTargets)
    {
      if (r.id == id)
      {
        return r.width;
      }
    }
    return -1;
  }

  public int Height(string id)
  {
    foreach (var r in _renderTargets)
    {
      if (r.id == id)
      {
        return r.height;
      }
    }
    return -1;
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
    public MaterialFloatSetting[] materialSetting;

    [Header("Texture Settings")]
    public RenderTexture renderTexture;
    public int width => renderTexture.width;
    public int height => renderTexture.height;
    public int padding = 0;

    [Header("Debug")]
    public bool RenderToTarget;
    public bool LogRendered = false;

    private List<float> _lastValues;
    protected virtual void PreRender()
    {
      activeStates.SetStates();
      if (materialSetting != null)
      {
        if (_lastValues == null)
        {
          _lastValues = new List<float>();
        }

        _lastValues.Clear();
        foreach (var setting in materialSetting)
        {
          _lastValues.Add(blitMaterial.GetFloat(setting.name));
          blitMaterial.SetFloat(setting.name, setting.value);
        }
      }
    }

    protected virtual void PostRender()
    {
      activeStates.ResetStates();
      if (materialSetting != null)
      {
        for (int i = 0; i < materialSetting.Length; i++)
        {
          blitMaterial.SetFloat(materialSetting[i].name, _lastValues[i]);
        }
        _lastValues.Clear();
      }
    }

    protected abstract bool RenderToTexture(out RenderTexture rtOut);

    public virtual void Validate()
    {
      if (camera == null)
      {
        camera = Camera.main;
      }
      if (renderTexture == null)
      {
        var renderWidth = Display.displays[0].renderingWidth;
        var renderHeight = Display.displays[0].renderingHeight;
        renderTexture = new RenderTexture(renderWidth, renderHeight, 24)
        {
          name = "RenderTargetBase::" + id
        };
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
#if UNITY_EDITOR
      if (LogRendered)
      {
        Debug.Log($"RenderTargetBase :: {id} rendered: {result}");
      }
#endif
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

    public override void Validate() => base.Validate();

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
      var renderWidth = Display.displays[0].renderingWidth;
      var renderHeight = Display.displays[0].renderingHeight;
      Vector3 _scale = sourceRenderer.transform.localScale;
      sourceRenderer.transform.localScale = sourceRenderer.transform.localScale.Multiply(sourceRendererScale);

      var mat = sourceRenderer.sharedMaterial;
      if (blitMaterial != null)
      {
        sourceRenderer.sharedMaterial = blitMaterial;
      }
      var result = camera.BlitCroppedToScreenBounds(ref renderTexture, sourceRenderer, renderWidth, renderHeight, null, padding, linear);

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

[Serializable]
public struct MaterialFloatSetting
{
  public string name;
  public float value;
}