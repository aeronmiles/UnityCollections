using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


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
        Debug.Log($"RenderTargetManager :: {rt.id} rendered: {rt.RenderToTarget}, {_renderAll}");
        rt.RenderToTarget = false;
      }
    }
    _renderAll = false;
  }
#endif

  public bool RenderToRenderTexture(string id, ref RenderTexture rt, bool forceReRender = false)
  {
    foreach (var r in _renderTargets)
    {
      if (r.id == id)
      {
        return r.RenderToRenderTexture(ref rt, forceReRender);
      }
    }
    return false;
  }

  // @TODO: Implement error handling  
  public bool Render(string id, out RenderTexture rtOut, bool forceReRender = false)
  {
    foreach (var r in _renderTargets)
    {
      if (r.id == id)
      {
        return r.Render(out rtOut, forceReRender);
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
    public Material blitMaterial;
    public bool cropToSourceRenderer;
    public bool cropToSquareAspectRatio;
    public Vector3 sourceRendererScale = Vector3.one;
    public bool linear = true;
    // Target Unity display index to render from (affects camera.targetDisplay)
    public int targetDisplay = 0;
    public GameObjectActiveState[] activeStates;
    public Camera camera;
    public MaterialFloatSetting[] materialSetting = new MaterialFloatSetting[0];
    public MaterialKeywordSetting[] materialKeywords = new MaterialKeywordSetting[0];

    [Header("Texture Settings")]
    public RenderTexture renderTexture;
    public int width => renderTexture.width;
    public int height => renderTexture.height;
    public int padding = 0;

    [Header("Debug")]
    public bool RenderToTarget;
    public bool LogRendered = false;

    private List<float> _lastMaterialSettingsValues = new();
    private List<bool> _lastMaterialKeywordValues = new();
    private int _lastCameraTargetDisplay = -1;
    protected virtual void PreRender()
    {
      // Swap camera target display if needed
      if (camera != null)
      {
        _lastCameraTargetDisplay = camera.targetDisplay;
        // Clamp to valid display range if available
        if (Display.displays != null && Display.displays.Length > 0)
        {
          if (targetDisplay > Display.displays.Length)
          {
            Debug.LogError("RenderTargetManager :: target display out of bounds");
          }
          else
          {
            camera.targetDisplay = targetDisplay;
          }
        }
        else
        {
          camera.targetDisplay = 0;
        }
      }

#if UNITY_EDITOR
      // In the Editor, also switch the GameView's selected display to match
      EditorUtil.PushGameViewSelectedDisplay(targetDisplay);
#endif

      activeStates.SetStates();
      if (materialSetting != null)
      {
        if (_lastMaterialSettingsValues == null)
        {
          _lastMaterialSettingsValues = new();
        }
        _lastMaterialSettingsValues.Clear();
        foreach (var setting in materialSetting)
        {
          _lastMaterialSettingsValues.Add(blitMaterial.GetFloat(setting.name));
          blitMaterial.SetFloat(setting.name, setting.value);
        }
      }

      if (materialKeywords != null)
      {
        if (_lastMaterialKeywordValues == null)
        {
          _lastMaterialKeywordValues = new();
        }
        _lastMaterialKeywordValues.Clear();
        foreach (var keyword in materialKeywords)
        {
          _lastMaterialKeywordValues.Add(blitMaterial.IsKeywordEnabled(keyword.name));
          if (keyword.enabled)
          {
            blitMaterial.EnableKeyword(keyword.name);
          }
          else
          {
            blitMaterial.DisableKeyword(keyword.name);
          }
        }
      }
    }

    protected virtual void PostRender()
    {
      // Restore camera target display
      if (camera != null && _lastCameraTargetDisplay >= 0)
      {
        camera.targetDisplay = _lastCameraTargetDisplay;
      }

#if UNITY_EDITOR
      // Restore GameView selected display
      EditorUtil.PopGameViewSelectedDisplay();
#endif

      activeStates.ResetStates();
      if (materialSetting != null)
      {
        for (int i = 0; i < materialSetting.Length; i++)
        {
          blitMaterial.SetFloat(materialSetting[i].name, _lastMaterialSettingsValues[i]);
        }
        _lastMaterialSettingsValues.Clear();
      }

      if (materialKeywords != null)
      {
        for (int i = 0; i < materialKeywords.Length; i++)
        {
          if (_lastMaterialKeywordValues[i])
          {
            blitMaterial.EnableKeyword(materialKeywords[i].name);
          }
          else
          {
            blitMaterial.DisableKeyword(materialKeywords[i].name);
          }
        }
        _lastMaterialKeywordValues.Clear();
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
        var renderWidth = camera.pixelWidth;
        var renderHeight = camera.pixelHeight;
        renderTexture = new RenderTexture(renderWidth, renderHeight, 24);
      }
    }

    private int _lastFrame = -1;

    public bool RenderToRenderTexture(ref RenderTexture rt, bool forceReRender = false)
    {
      var cacheRT = renderTexture;
      bool success;
      try
      {
        renderTexture = rt;
        success = Render(out rt, forceReRender);
      }
      catch (Exception ex)
      {
        Debug.LogError("RenderTargetManager :: RenderToRenderTexture() Exception: " + ex.ToString());
        success = false;
      }
      finally
      {
        renderTexture = cacheRT;
      }
      return success;
    }

    public bool Render(out RenderTexture rtOut, bool forceReRender = false)
    {
      if (!forceReRender && _lastFrame == Time.frameCount)
      {
        rtOut = renderTexture;
        return true;
      }

      PreRender();
      var result = RenderToTexture(out rtOut);
      PostRender();
#if UNITY_EDITOR
      if (result && LogRendered)
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
      var renderWidth = Display.displays[targetDisplay].renderingWidth;
      var renderHeight = Display.displays[targetDisplay].renderingHeight;
      // Debug.Log($"RenderTargetManager :: RenderCropped() :: width: {width}, height: {height}");
      Vector3 _scale = sourceRenderer.transform.localScale;
      sourceRenderer.transform.localScale = sourceRenderer.transform.localScale.Multiply(sourceRendererScale);

      var mat = sourceRenderer.sharedMaterial;
      if (blitMaterial != null)
      {
        sourceRenderer.sharedMaterial = blitMaterial;
      }
      var result = camera.BlitCroppedToScreenBounds(ref renderTexture, sourceRenderer, renderWidth, renderHeight, cropToSquareAspectRatio, null, padding, linear);
      // if (!result)
      // {
      //   Debug.LogError($"RenderTargetManager :: RenderCropped :: Failed to render cropped for {id}");
      // }

      // Cleanup
      sourceRenderer.sharedMaterial = mat;
      sourceRenderer.transform.localScale = _scale;
      return result;
    }

    private bool RenderScreen()
    {
      bool result;
      var width = camera.pixelWidth;
      var height = camera.pixelHeight;
      if (camera.transform.eulerAngles.z == 90f || camera.transform.eulerAngles.z == -90f)
      {
        width = height;
        height = camera.pixelWidth;
      }
#if UNITY_EDITOR
      if (Application.isPlaying)
      {
        width = (int)(width * UnityEditor.EditorGUIUtility.pixelsPerPoint);
        height = (int)(height * UnityEditor.EditorGUIUtility.pixelsPerPoint);
      }
#endif
      // Debug.Log($"RenderTargetManager :: RenderScreen() :: width: {width}, height: {height}");
      var tmpRT = RenderTexture.GetTemporary(width, height);
      try
      {
        result = camera.Blit(ref tmpRT, blitMaterial, linear);
        Graphics.Blit(tmpRT, renderTexture);
      }
      catch (Exception e)
      {
        Debug.LogError($"RenderTargetManager :: RenderScreen :: Exception rendering screen for {id} :: {e}");
        result = false;
      }
      finally
      {
        RenderTexture.ReleaseTemporary(tmpRT);
      }

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

[Serializable]
public struct MaterialKeywordSetting
{
  public string name;
  public bool enabled;
}
