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
        Debug.Log("LogManager Awake called");
        InitializeLogging();
    }

    private void InitializeLogging()
    {
        if (isInitialized)
        {
            Debug.Log("LogManager already initialized");
            return;
        }

        try
        {
            // Create logs directory in the project folder
            string logsDirectory = Path.Combine(Application.dataPath, "Logs");
            Debug.Log($"Attempting to create/access logs directory at: {logsDirectory}");
            
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
                Debug.Log("Created Logs directory");
            }

            // Create a log file with timestamp
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            logFilePath = Path.Combine(logsDirectory, $"simulation_log_{timestamp}.txt");
            Debug.Log($"Attempting to create log file at: {logFilePath}");
            
            // Test if we can write to the file
            try
            {
                using (StreamWriter testWriter = new StreamWriter(logFilePath, true))
                {
                    testWriter.WriteLine("=== Log File Created Successfully ===");
                }
                Debug.Log($"Successfully created and wrote to log file at: {logFilePath}");
            }
            catch (Exception writeEx)
            {
                Debug.LogError($"Failed to write to log file: {writeEx.Message}\nStack trace: {writeEx.StackTrace}");
                throw; // Re-throw to be caught by outer try-catch
            }
            
            // Initialize the stream writer
            logWriter = new StreamWriter(logFilePath, true);
            logWriter.AutoFlush = true;  // Ensure logs are written immediately
            
            // Log the start of a new session
            LogMessage("=== New Simulation Session Started ===");
            LogMessage($"Time: {DateTime.Now}");
            LogMessage($"Unity Version: {Application.unityVersion}");
            LogMessage($"Platform: {Application.platform}");
            LogMessage($"Log File Path: {logFilePath}");
            LogMessage("=====================================");
            
            isInitialized = true;
            Debug.Log("LogManager successfully initialized");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize logging: {e.Message}\nStack trace: {e.StackTrace}");
            // Try to write to a fallback location
            try
            {
                string fallbackPath = Path.Combine(Application.persistentDataPath, "simulation_log.txt");
                Debug.Log($"Attempting to write to fallback location: {fallbackPath}");
                logWriter = new StreamWriter(fallbackPath, true);
                logWriter.AutoFlush = true;
                isInitialized = true;
                LogMessage("=== Logging initialized with fallback path ===");
            }
            catch (Exception fallbackEx)
            {
                Debug.LogError($"Failed to initialize fallback logging: {fallbackEx.Message}");
            }
        }
    }

    public static void LogMessage(string message)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("Attempted to log message before LogManager was initialized");
            return;
        }

        try
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logMessage = $"[{timestamp}] {message}";
            
            // Write to file
            if (logWriter != null)
            {
                logWriter.WriteLine(logMessage);
                logWriter.Flush();  // Force write to disk
            }
            else
            {
                Debug.LogError("LogWriter is null when trying to write message");
            }
            
            // Also write to Unity console for immediate feedback
            Debug.Log(logMessage);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write log: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    public static void LogError(string message)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("Attempted to log error before LogManager was initialized");
            return;
        }

        try
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logMessage = $"[{timestamp}] ERROR: {message}";
            
            // Write to file
            if (logWriter != null)
            {
                logWriter.WriteLine(logMessage);
                logWriter.Flush();  // Force write to disk
            }
            else
            {
                Debug.LogError("LogWriter is null when trying to write error");
            }
            
            // Also write to Unity console for immediate feedback
            Debug.LogError(logMessage);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write error log: {e.Message}\nStack trace: {e.StackTrace}");
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