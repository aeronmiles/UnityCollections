using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEngine;

public class MapTouchDisplay : MonoBehaviour
{
  // @TODO: Configuration setup
  [SerializeField] private string _outputDisplay = "DP-1";

  // Call the functions to list devices, parse the ID, and map the device
  private void Start()
  {
    string xinputListOutput = ExecuteCommand("xinput list");
    int deviceId = GetDeviceId(xinputListOutput);
    if (deviceId == -1)
    {
      UnityEngine.Debug.LogError("Failed to find TSTP MTouch device.");
    }
    else
    {
      UnityEngine.Debug.Log($"Mapping TSTP MTouch device with ID: {deviceId}.");

      // Get display and screen dimensions
      var (displayWidth, displayHeight, displayX, displayY, totalWidth, totalHeight) = GetDisplayDimensions(_outputDisplay);

      MapDeviceToOutput(deviceId, _outputDisplay);
      SetCoordinateTransformationMatrix(deviceId, displayWidth, displayHeight, displayX, displayY, totalWidth, totalHeight);
    }
  }

  // Function to execute a shell command and return the output
  private string ExecuteCommand(string command)
  {
    UnityEngine.Debug.Log($"Executing command: bash -c {command}");
    ProcessStartInfo processStartInfo = new ProcessStartInfo
    {
      FileName = "/usr/bin/bash",
      Arguments = $"-c \"{command}\"",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };

    using (Process process = new Process())
    {
      process.StartInfo = processStartInfo;
      _ = process.Start();

      string output = process.StandardOutput.ReadToEnd();
      string error = process.StandardError.ReadToEnd();
      UnityEngine.Debug.Log($"Command output: {output}");
      UnityEngine.Debug.Log($"Command error: {error}");

      process.WaitForExit();

      if (!string.IsNullOrEmpty(error))
      {
        UnityEngine.Debug.LogError(error);
      }

      return output;
    }
  }

  // Function to parse the device ID for "TSTP MTouch"
  private int GetDeviceId(string xinputListOutput)
  {
    string pattern = @"TSTP MTouch\s+id=(\d+)";
    Match match = Regex.Match(xinputListOutput, pattern);

    if (match.Success && int.TryParse(match.Groups[1].Value, out int deviceId))
    {
      return deviceId;
    }

    UnityEngine.Debug.LogError("TSTP MTouch device not found.");
    return -1;
  }

  // Function to map the device to the specified output
  private void MapDeviceToOutput(int deviceId, string output)
  {
    if (deviceId < 0)
    {
      return;
    }

    string command = $"xinput map-to-output {deviceId} {output}";
    _ = ExecuteCommand(command);
  }

  // Function to get the display dimensions using xrandr
  private (float displayWidth, float displayHeight, float displayX, float displayY, float totalWidth, float totalHeight) GetDisplayDimensions(string outputDisplay)
  {
    string xrandrOutput = ExecuteCommand("xrandr");
    float displayWidth = 1920f;
    float displayHeight = 1080f;
    float displayX = 0f;
    float displayY = 0f;
    float totalWidth = 2160f;
    float totalHeight = 4920f;

    // Regex to find the current resolution and position of the specified display
    string displayPattern = $@"{Regex.Escape(outputDisplay)} connected.*?(\d+)x(\d+)\+(\d+)\+(\d+)";
    Match displayMatch = Regex.Match(xrandrOutput, displayPattern);

    if (displayMatch.Success)
    {
      displayWidth = float.Parse(displayMatch.Groups[1].Value);
      displayHeight = float.Parse(displayMatch.Groups[2].Value);
      displayX = float.Parse(displayMatch.Groups[3].Value);
      displayY = float.Parse(displayMatch.Groups[4].Value);
    }

    string screenPattern = @"current (\d+) x (\d+)";
    Match screenMatch = Regex.Match(xrandrOutput, screenPattern);

    if (screenMatch.Success)
    {
      totalWidth = float.Parse(screenMatch.Groups[1].Value);
      totalHeight = float.Parse(screenMatch.Groups[2].Value);
    }

    return (displayWidth, displayHeight, displayX, displayY, totalWidth, totalHeight);
  }

  // Function to set the coordinate transformation matrix
  private void SetCoordinateTransformationMatrix(int deviceId, float displayWidth, float displayHeight, float displayX, float displayY, float totalWidth, float totalHeight)
  {
    if (deviceId < 0)
    {
      return;
    }

    // Calculate the transformation matrix to map the touch to the specific display
    float scaleX = displayWidth / totalWidth;
    float scaleY = displayHeight / totalHeight;
    float offsetX = displayX / totalWidth;
    float offsetY = displayY / totalHeight;

    string command = $"xinput set-prop {deviceId} 'Coordinate Transformation Matrix' {scaleX} 0 {offsetX} 0 {scaleY} {offsetY} 0 0 1";
    _ = ExecuteCommand(command);
  }
}
