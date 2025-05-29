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
    
    // Simple toggle to enable/disable all logging
    public static bool enableLogging = false;
    
    // Singleton pattern
    public static LogManager Instance
    {
        get
        {
            // Don't create a new instance during application quit
            if (isApplicationQuitting)
            {
                Debug.Log("LogManager: Instance requested during application quit, returning null");
                return null;
            }
            
            if (instance == null)
            {
                try
                {
                    Debug.Log("LogManager: Creating new instance");
                    GameObject go = new GameObject("LogManager");
                    instance = go.AddComponent<LogManager>();
                    DontDestroyOnLoad(go);
                }
                catch (Exception e)
                {
                    Debug.LogError($"LogManager: Error creating instance: {e.Message}");
                }
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        // Skip all initialization if logging is disabled
        if (!enableLogging) return;
        
        try
        {
            Debug.Log($"LogManager Awake called on {gameObject.name}");
            isApplicationQuitting = false;
            if (instance != null && instance != this)
            {
                Debug.LogWarning($"LogManager: Duplicate instance detected on {gameObject.name}, destroying");
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeLogging();
        }
        catch (Exception e)
        {
            Debug.LogError($"LogManager: Error in Awake: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private void OnDestroy()
    {
        // Skip cleanup if logging is disabled
        if (!enableLogging) return;
        
        try
        {
            Debug.Log($"LogManager OnDestroy called on {gameObject.name}");
            
            // Only close the writer if this is the main instance
            if (instance == this)
            {
                Debug.Log("LogManager: Main instance being destroyed, closing log writer");
                CloseLogWriter();
                instance = null; // Clear the instance reference
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"LogManager: Error in OnDestroy: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private void OnApplicationQuit()
    {
        // Skip cleanup if logging is disabled
        if (!enableLogging) return;
        
        try
        {
            Debug.Log("LogManager: OnApplicationQuit called");
            isApplicationQuitting = true;
            CloseLogWriter();
            instance = null;
        }
        catch (Exception e)
        {
            Debug.LogError($"LogManager: Error in OnApplicationQuit: {e.Message}\n{e.StackTrace}");
        }
    }
    
    // Static method to explicitly clean up the LogManager before scene changes
    public static void Cleanup()
    {
        // Skip cleanup if logging is disabled
        if (!enableLogging) return;
        
        try
        {
            Debug.Log("LogManager: Cleanup method called");
            
            if (instance != null)
            {
                instance.CloseLogWriter();
                
                // Don't destroy the instance here - just disable logging
                // This prevents Unity from creating a new instance when we're trying to clean up
                isInitialized = false;
                
                Debug.Log("LogManager: Cleanup completed successfully");
            }
            else
            {
                Debug.Log("LogManager: Cleanup called but instance is null");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"LogManager: Error in Cleanup: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private void InitializeLogging()
    {
        // Skip initialization if logging is disabled
        if (!enableLogging) return;
        
        try
        {
            // Create logs directory if it doesn't exist
            string logsDirectory = Path.Combine(Application.dataPath, "Logs");
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
                Debug.Log($"LogManager: Created logs directory at: {logsDirectory}");
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
            Debug.Log($"LogManager: Initialized. Log file: {logFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"LogManager: Failed to initialize: {e.Message}\nStack trace: {e.StackTrace}");
            isInitialized = false;
        }
    }
    
    private void CloseLogWriter()
    {
        try
        {
            Debug.Log("LogManager: CloseLogWriter called");
            
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
                    Debug.Log("LogManager: Log file closed successfully");
                }
                catch (Exception e)
                {
                    Debug.LogError($"LogManager: Error closing log writer: {e.Message}");
                }
            }
            else
            {
                Debug.Log("LogManager: CloseLogWriter called but logWriter is null");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"LogManager: Outer error in CloseLogWriter: {e.Message}");
        }
    }
    
    public static void LogMessage(string message)
    {
        // Skip logging if disabled
        if (!enableLogging) return;
        
        // Skip logging if application is quitting
        if (isApplicationQuitting)
        {
            return;
        }
        
        if (!isInitialized)
        {
            Debug.LogWarning("LogManager: Attempted to log message before initialized");
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
                Debug.LogWarning("LogManager: LogWriter is null, attempting to reinitialize");
                var inst = Instance;
                if (inst != null)
                {
                    inst.InitializeLogging();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"LogManager: Failed to write log: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }
    
    public static void LogError(string message)
    {
        // Skip logging if disabled
        if (!enableLogging) return;
        
        // Also write to Debug.LogError to ensure we see errors in the console
        Debug.LogError($"LOG ERROR: {message}");
        
        // Skip logging if application is quitting
        if (isApplicationQuitting)
        {
            return;
        }
        
        if (!isInitialized)
        {
            Debug.LogWarning("LogManager: Attempted to log error before initialized");
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
                Debug.LogWarning("LogManager: LogWriter is null while logging error, attempting to reinitialize");
                var inst = Instance;
                if (inst != null)
                {
                    inst.InitializeLogging();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"LogManager: Failed to write error log: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }
} 