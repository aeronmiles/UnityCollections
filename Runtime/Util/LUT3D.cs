using System;
using System.Collections.Generic;
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

[Serializable]
public class LutPair
{
  public Color coordColor;
  public Color lutColor;
}

public static class LutExtensions
{
  public static Texture3D CreateLut(this IEnumerable<LutPair> lutPairs, int size)
  {
    // Create a new Texture3D
    Texture3D lutTexture = new Texture3D(size, size, size, TextureFormat.RGBAFloat, false);

    // Prepare color array for Texture3D
    Color[] colors = new Color[size * size * size];
    float sizeLessOne = size - 1;
    // Fill in the colors array with LUT values
    for (int z = 0; z < size; z++)
    {
      for (int y = 0; y < size; y++)
      {
        for (int x = 0; x < size; x++)
        {
          Color interpolatedColor = InterpolateColor(lutPairs, x / sizeLessOne, y / sizeLessOne, z / sizeLessOne);
          colors[x + (y * size) + (z * size * size)] = interpolatedColor;
        }
      }
    }

    // Set the pixels of the Texture3D
    lutTexture.SetPixels(colors);
    lutTexture.Apply();

    return lutTexture;
  }

  private static Color InterpolateColor(IEnumerable<LutPair> lutPairs, float x, float y, float z)
  {
    LutPair closestPair = null;
    LutPair secondClosestPair = null;
    float closestDistance = Mathf.Infinity;
    float secondClosestDistance = Mathf.Infinity;

    foreach (LutPair pair in lutPairs)
    {
      float distance = Vector3.Distance(new Vector3(pair.coordColor.r, pair.coordColor.g, pair.coordColor.b), new Vector3(x, y, z));
      if (distance < closestDistance)
      {
        secondClosestPair = closestPair;
        secondClosestDistance = closestDistance;
        closestPair = pair;
        closestDistance = distance;
      }
      else if (distance < secondClosestDistance)
      {
        secondClosestPair = pair;
        secondClosestDistance = distance;
      }
    }

    if (closestPair == null || secondClosestPair == null)
    {
      return Color.black; // Fallback in case no closest or second closest pair found
    }

    // Compute the interpolation factor (assuming distance is a good metric for this)
    float t = closestDistance / (closestDistance + secondClosestDistance);

    // Linearly interpolate between the two closest colors
    Color interpolatedColor = Color.Lerp(closestPair.lutColor, secondClosestPair.lutColor, t);
    return interpolatedColor;
  }
}