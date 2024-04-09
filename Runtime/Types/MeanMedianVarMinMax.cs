
using Unity.Mathematics;

namespace AM.Unity.Statistics
{
  [System.Serializable]
  public struct MeanMedianVarMinMax
  {
    public float mean;
    public float median;
    public float variance;
    public float min;
    public float max;
  }

  [System.Serializable]
  public struct MeanMedianVarMinMaxFloat2
  {
    public float2 mean;
    public float2 median;
    public float2 variance;
    public float2 min;
    public float2 max;
  }

  [System.Serializable]
  public struct MeanMedianVarMinMaxFloat3
  {
    public float3 mean;
    public float3 median;
    public float3 variance;
    public float3 min;
    public float3 max;
  }
}