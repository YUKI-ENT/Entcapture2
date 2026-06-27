using System;
using System.IO;
using System.Text;

namespace ENTcapture2.Core.Services;

/// <summary>
/// Provides file-based logging for debugging. Logs are stored in 
/// LocalApplicationData/ENTcapture2/logs with a new file created each day.
/// </summary>
public sealed class DebugLogger
{
    private static readonly object _lockObject = new object();
    private static string? _logsDirectory;
    private static string? _currentLogFilePath;
    private static DateTime? _currentLogDate;

    public static void Info(string message)
    {
        Log("INFO", message);
    }

    public static void Warning(string message)
    {
        Log("WARN", message);
    }

    public static void Error(string message, Exception? exception = null)
    {
        string fullMessage = exception is not null
            ? $"{message}\n{exception}"
            : message;
        Log("ERROR", fullMessage);
    }

    public static void Debug(string message)
    {
        Log("DEBUG", message);
    }

    private static void Log(string level, string message)
    {
        lock (_lockObject)
        {
            try
            {
                DateTime now = DateTime.Now;
                string logsDir = GetLogsDirectory();
                string logFile = GetLogFilePath(logsDir, now);

                string logLine = $"[{now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";

                File.AppendAllText(logFile, logLine + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception)
            {
                // Silently fail if logging fails to avoid crashing the application
            }
        }
    }

    private static string GetLogsDirectory()
    {
        if (_logsDirectory is not null)
        {
            return _logsDirectory;
        }

        string appDataPath = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        _logsDirectory = Path.Combine(appDataPath, "ENTcapture2", "logs");

        try
        {
            Directory.CreateDirectory(_logsDirectory);
        }
        catch (Exception)
        {
            // If logs directory creation fails, fall back to temp directory
            _logsDirectory = Path.Combine(Path.GetTempPath(), "ENTcapture2_logs");
            try
            {
                Directory.CreateDirectory(_logsDirectory);
            }
            catch (Exception)
            {
                _logsDirectory = Path.GetTempPath();
            }
        }

        return _logsDirectory;
    }

    private static string GetLogFilePath(string logsDirectory, DateTime now)
    {
        // Check if we need a new log file (date changed)
        if (_currentLogDate is null || _currentLogDate.Value.Date != now.Date)
        {
            _currentLogDate = now;
            string fileName = $"entcapture2_{now:yyyy-MM-dd}.log";
            _currentLogFilePath = Path.Combine(logsDirectory, fileName);
        }

        return _currentLogFilePath ?? Path.Combine(logsDirectory, "entcapture2.log");
    }

    /// <summary>
    /// Get the current log file path for external reference (e.g., showing in UI).
    /// </summary>
    public static string GetCurrentLogFilePath()
    {
        string logsDir = GetLogsDirectory();
        return GetLogFilePath(logsDir, DateTime.Now);
    }
}
