using UnityEngine;
using System.IO;
using System;

public class LogManager : MonoBehaviour
{
    private static LogManager instance;
    private static bool isInitialized = false;
    private static StreamWriter logWriter;
    private static string logFilePath;
    private static bool isApplicationQuitting = false;
    
    // Singleton pattern
    public static LogManager Instance
    {
        get
        {
            // Don't create a new instance during application quit
            if (isApplicationQuitting)
            {
                return null;
            }
            
            if (instance == null)
            {
                GameObject go = new GameObject("LogManager");
                instance = go.AddComponent<LogManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeLogging();
    }
    
    private void OnDestroy()
    {
        // Only close the writer if this is the main instance
        if (instance == this)
        {
            CloseLogWriter();
            instance = null; // Clear the instance reference
        }
    }
    
    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
        CloseLogWriter();
        instance = null;
    }
    
    // Static method to explicitly clean up the LogManager before scene changes
    public static void Cleanup()
    {
        if (instance != null)
        {
            instance.CloseLogWriter();
            instance = null;
        }
        isInitialized = false;
    }
    
    private void InitializeLogging()
    {
        try
        {
            // Create logs directory if it doesn't exist
            string logsDirectory = Path.Combine(Application.dataPath, "Logs");
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
                Debug.Log($"Created logs directory at: {logsDirectory}");
            }
            
            // Create a new log file with timestamp
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            logFilePath = Path.Combine(logsDirectory, $"simulation_log_{timestamp}.txt");
            
            // Create or open the log file
            logWriter = new StreamWriter(logFilePath, true);
            logWriter.AutoFlush = true;  // Ensure logs are written immediately
            
            // Write initial log entry
            logWriter.WriteLine($"=== Simulation Log Started at {DateTime.Now} ===");
            
            isInitialized = true;
            Debug.Log($"LogManager initialized. Log file: {logFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize LogManager: {e.Message}\nStack trace: {e.StackTrace}");
            isInitialized = false;
        }
    }
    
    private void CloseLogWriter()
    {
        if (logWriter != null)
        {
            try
            {
                logWriter.WriteLine($"=== Simulation Log Ended at {DateTime.Now} ===");
                logWriter.Flush();
                logWriter.Close();
                logWriter.Dispose();
                logWriter = null;
                isInitialized = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error closing log writer: {e.Message}");
            }
        }
    }
    
    public static void LogMessage(string message)
    {
        // Skip logging if application is quitting
        if (isApplicationQuitting)
        {
            return;
        }
        
        if (!isInitialized)
        {
            Debug.LogWarning("Attempted to log message before LogManager was initialized");
            return;
        }
        
        try
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logEntry = $"[{timestamp}] {message}";
            
            // Write to file
            if (logWriter != null)
            {
                logWriter.WriteLine(logEntry);
                logWriter.Flush();  // Ensure it's written immediately
            }
            else
            {
                Debug.LogWarning("LogWriter is null, attempting to reinitialize");
                var inst = Instance;
                if (inst != null)
                {
                    inst.InitializeLogging();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write log: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }
    
    public static void LogError(string message)
    {
        // Skip logging if application is quitting
        if (isApplicationQuitting)
        {
            return;
        }
        
        if (!isInitialized)
        {
            Debug.LogWarning("Attempted to log error before LogManager was initialized");
            return;
        }
        
        try
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logEntry = $"[{timestamp}] ERROR: {message}";
            
            // Write to file
            if (logWriter != null)
            {
                logWriter.WriteLine(logEntry);
                logWriter.Flush();  // Ensure it's written immediately
            }
            else
            {
                Debug.LogWarning("LogWriter is null, attempting to reinitialize");
                var inst = Instance;
                if (inst != null)
                {
                    inst.InitializeLogging();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write error log: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }
} 