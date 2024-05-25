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
      MapDeviceToOutput(deviceId, _outputDisplay);
    }
    SetCoordinateTransformationMatrix(deviceId);
  }

  // Function to execute a shell command and return the output
  private string ExecuteCommand(string command)
  {
    ProcessStartInfo processStartInfo = new ProcessStartInfo
    {
      FileName = "/bin/bash",
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

  // Function to set the coordinate transformation matrix
  private void SetCoordinateTransformationMatrix(int deviceId)
  {
    if (deviceId < 0)
    {
      return;
    }

    string command = $"xinput set-prop {deviceId} 'Coordinate Transformation Matrix' 1 0 0 0 1 0 0 0 1";
    _ = ExecuteCommand(command);
  }
}
