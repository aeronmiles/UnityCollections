using UnityEngine;

public class LUT3D : MonoBehaviour
{
  [SerializeField] private TextAsset _cubeFile;
  [SerializeField] private int _lutSize = 65;
  [SerializeField] private Texture3D _lutTexture;
  [SerializeField] private Material[] _lutMaterials;
  [SerializeField] private Texture2D _preLutTexture;

  private void OnEnable()
  {
    if (_cubeFile == null)
    {
      return;
    }

    _lutTexture = GenerateLUTTexture(_cubeFile.text);
    _lutTexture.wrapMode = TextureWrapMode.Clamp;
    _lutTexture.filterMode = FilterMode.Trilinear;

    foreach (Material _lutMaterial in _lutMaterials)
    {
      if (_preLutTexture)
      {
        _lutMaterial.SetTexture("_MainTex", _preLutTexture);
      }
      _lutMaterial.SetTexture("_LutTex", _lutTexture);
    }
  }

  private Texture3D GenerateLUTTexture(string cubeData)
  {
    Color[] lutColors = ParseCubeLUT(cubeData, _lutSize);

    Texture3D lutTexture = new Texture3D(_lutSize, _lutSize, _lutSize, TextureFormat.RGBAFloat, false);
    lutTexture.SetPixels(lutColors);
    lutTexture.Apply();

    return lutTexture;
  }

  // Simple parsing function (replace with a robust library for production)
  private Color[] ParseCubeLUT(string cubeData, int lutSize)
  {
    string[] lines = cubeData.Split('\n');
    int colorIndex = 0;
    Color[] lutColors = new Color[lutSize * lutSize * lutSize];

    foreach (string line in lines)
    {
      if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
      {
        continue;
      }

      string[] values = line.Split(' ');
      if (values.Length != 3)
      {
        continue; // Skip malformed lines
      }

      lutColors[colorIndex++] = new Color(
          float.Parse(values[0]),
          float.Parse(values[1]),
          float.Parse(values[2])
      );
    }

    return lutColors;
  }
}
