
using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class DistanceFieldCompute : MonoBehaviour
{
  [SerializeField] private RenderTargetManager _rtManager;
  public ComputeShader computeShader;
  public RenderTexture outputTexture;
  public Texture2D outputTex;

  private ComputeBuffer _outputBuffer;

  [Header("Debug")]
  [SerializeField] private bool _render;

  private void OnEnable()
  {
    _outputBuffer = new ComputeBuffer(outputTexture.width * outputTexture.height, sizeof(float) * 4);
    outputTexture.enableRandomWrite = true;
    _ = outputTexture.Create();
    _ = _rtManager.Render("SimplexNoise", out outputTexture);
  }

  private void Update()
  {
#if UNITY_EDITOR
    if (!Application.isPlaying && !_render)
    {
      return;
    }
#endif
    int kernelIndex = computeShader.FindKernel("GenerateDistanceField");
    computeShader.SetTexture(kernelIndex, "OutputTexture", outputTexture);
    int[] dimensions = new int[] { outputTexture.width, outputTexture.height };
    ComputeBuffer dimensionsBuffer = new ComputeBuffer(2, sizeof(int));
    dimensionsBuffer.SetData(dimensions);
    computeShader.SetBuffer(kernelIndex, "dimensions", dimensionsBuffer);

    // computeShader.SetBuffer(kernelIndex, "OutputBuffer", _outputBuffer);

    computeShader.Dispatch(kernelIndex, outputTexture.width / 8, outputTexture.height / 8, 1);
#if UNITY_EDITOR
    _render = false;
#endif
    StartCoroutine(outputTexture.BlitToTexAsync(outputTex));
  }
}