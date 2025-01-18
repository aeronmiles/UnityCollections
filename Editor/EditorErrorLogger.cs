using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;

namespace CustomTools.Logging
{
    [InitializeOnLoad]
    public class EditorErrorLogger
    {
        private static readonly string LogFilePath;
        private static StringBuilder errorBuffer;
        private static StringBuilder warningBuffer;
        private static bool isLoggingPaused;
        private static float autoSaveInterval = 300f;
        private static double lastSaveTime;
  
        static EditorErrorLogger()
        {
            LogFilePath = "/Volumes/T500/Dev/github/vcv-vr/UnityCompileLogs.log";
            Debug.Log("<color=orange>Initializing EditorErrorLogger</color> at path: " + LogFilePath);
            errorBuffer = new StringBuilder();
            warningBuffer = new StringBuilder();
            isLoggingPaused = false;
            Initialize();
        }

        private static void Initialize()
        {
            try
            {
                string directoryPath = Path.GetDirectoryName(LogFilePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                if (File.Exists(LogFilePath))
                {
                    File.Delete(LogFilePath);
                }
                Application.logMessageReceived += OnLogMessageReceived;
                // EditorApplication.update += OnEditorUpdate;
                lastSaveTime = EditorApplication.timeSinceStartup;
                SaveErrorLog();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize EditorErrorLogger: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            try
            {
                if (isLoggingPaused)
                    return;

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logEntry = $"[{timestamp}] {type}: {condition}\n{stackTrace}\n\n";

                if (type == LogType.Error || type == LogType.Exception)
                {
                    errorBuffer.Append(logEntry);
                }
                else if (type == LogType.Warning)
                {
                    Debug.Log($"Appending warning message to buffer: {logEntry}");
                    warningBuffer.Append(logEntry);
                }
                
                SaveErrorLog();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in OnLogMessageReceived: {e.Message}");
            }
        }

        // private static void OnEditorUpdate()
        // {
        //     try
        //     {
        //         if (EditorApplication.timeSinceStartup - lastSaveTime >= autoSaveInterval)
        //         {
        //             SaveErrorLog();
        //             lastSaveTime = EditorApplication.timeSinceStartup;
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         Debug.LogError($"Error in OnEditorUpdate: {e.Message}");
        //     }
        // }

        private static void SaveErrorLog()
        {
            try
            {
                string headerMessage;
                if (errorBuffer.Length == 0 && warningBuffer.Length == 0)
                {
                    headerMessage = "Unity 3D: Compiled with No Issues\n";
                }
                else if (errorBuffer.Length == 0 && warningBuffer.Length > 0)
                {
                    headerMessage = "Unity 3D: Compiled with Warnings\n";
                }
                else
                {
                    headerMessage = "Unity 3D: failed to Compile with the following Issues\n";
                }

                string currentContent = headerMessage + errorBuffer.ToString() + "\n" + warningBuffer.ToString();
                errorBuffer.Clear();
                warningBuffer.Clear();  

                File.WriteAllText(LogFilePath, currentContent);
                Debug.Log("Saving log content..." + currentContent.Length + " characters written");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save error log: {e.Message}\nStack trace: {e.StackTrace}");
            }
        }

        [MenuItem("Tools/Error Logger/Toggle Logging")]
        private static void ToggleLogging()
        {
            isLoggingPaused = !isLoggingPaused;
            Debug.Log($"Error logging is now {(isLoggingPaused ? "paused" : "active")}");
        }

        [MenuItem("Tools/Error Logger/Open Log File")]
        private static void OpenLogFile()
        {
            Debug.Log($"Attempting to open log file at: {LogFilePath}");
            if (File.Exists(LogFilePath))
            {
                EditorUtility.OpenWithDefaultApp(LogFilePath);
                Debug.Log("Log file opened");
            }
            else
            {
                Debug.Log($"No log file exists at {LogFilePath}");
            }
        }

        [MenuItem("Tools/Error Logger/Clear Log")]
        private static void ClearLog()
        {
            Debug.Log($"Attempting to clear log file at: {LogFilePath}");
            if (File.Exists(LogFilePath))
            {
                File.Delete(LogFilePath);
                Debug.Log("Error log cleared");
            }
        }

        [MenuItem("Tools/Error Logger/Generate Test Error")]
        private static void GenerateTestError()
        {
            Debug.LogError("This is a test error message");
        }
    }
}
