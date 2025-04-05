using UnityEngine;
using System.IO;
using System;

public class LogManager : MonoBehaviour
{
    private static string logFilePath;
    private static StreamWriter logWriter;
    private static bool isInitialized = false;

    private void Awake()
    {
        InitializeLogging();
    }

    private void InitializeLogging()
    {
        if (isInitialized) return;

        try
        {
            // Create logs directory in the project folder
            string logsDirectory = Path.Combine(Application.dataPath, "..", "Logs");
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }

            // Create a log file with timestamp
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            logFilePath = Path.Combine(logsDirectory, $"simulation_log_{timestamp}.txt");
            
            // Initialize the stream writer
            logWriter = new StreamWriter(logFilePath, true);
            logWriter.AutoFlush = true;  // Ensure logs are written immediately
            
            // Log the start of a new session
            LogMessage("=== New Simulation Session Started ===");
            LogMessage($"Time: {DateTime.Now}");
            LogMessage($"Unity Version: {Application.unityVersion}");
            LogMessage($"Platform: {Application.platform}");
            LogMessage("=====================================");
            
            isInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize logging: {e.Message}");
        }
    }

    public static void LogMessage(string message)
    {
        if (!isInitialized) return;

        try
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logMessage = $"[{timestamp}] {message}";
            
            // Write to file
            logWriter.WriteLine(logMessage);
            
            // Also write to Unity console for immediate feedback
            Debug.Log(logMessage);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write log: {e.Message}");
        }
    }

    public static void LogError(string message)
    {
        if (!isInitialized) return;

        try
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logMessage = $"[{timestamp}] ERROR: {message}";
            
            // Write to file
            logWriter.WriteLine(logMessage);
            
            // Also write to Unity console for immediate feedback
            Debug.LogError(logMessage);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write error log: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        if (logWriter != null)
        {
            try
            {
                LogMessage("=== Simulation Session Ended ===");
                logWriter.Close();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to close log writer: {e.Message}");
            }
        }
    }
} 