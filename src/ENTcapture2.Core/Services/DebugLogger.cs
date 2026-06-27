using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ENTcapture2.Core.Services;

/// <summary>
/// Provides file-based logging for debugging. Logs are stored in 
/// LocalApplicationData/ENTcapture2/logs with a new file created each day.
/// </summary>
public sealed class DebugLogger
{
    private const int MaxLogRetentionDays = 7;
    private static readonly object _lockObject = new object();
    private static string? _logsDirectory;
    private static string? _currentLogFilePath;
    private static DateTime? _currentLogDate;

    private static readonly Channel<(string Level, string Message, DateTime Timestamp)> _logChannel;
    private static readonly Task _processTask;

    static DebugLogger()
    {
        _logChannel = Channel.CreateUnbounded<(string Level, string Message, DateTime Timestamp)>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                AllowSynchronousContinuations = false
            });

        _processTask = Task.Run(ProcessQueueAsync);

        AppDomain.CurrentDomain.ProcessExit += (s, e) => Shutdown();
    }

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
        DateTime now = DateTime.Now;
        try
        {
            _logChannel.Writer.TryWrite((level, message, now));
        }
        catch
        {
            // Silently fail if logging fails to avoid crashing the application
        }
    }

    private static async Task ProcessQueueAsync()
    {
        var reader = _logChannel.Reader;
        try
        {
            while (await reader.WaitToReadAsync())
            {
                while (reader.TryRead(out var logEntry))
                {
                    WriteToFile(logEntry.Level, logEntry.Message, logEntry.Timestamp);
                }
            }
        }
        catch
        {
            // Silently fail if processing fails
        }
    }

    private static void WriteToFile(string level, string message, DateTime now)
    {
        lock (_lockObject)
        {
            try
            {
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

    public static void Shutdown()
    {
        try
        {
            _logChannel.Writer.TryComplete();
            _processTask.Wait(3000);
        }
        catch
        {
            // Silently fail if shutdown fails
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
            CleanupOldLogs();
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

    private static void CleanupOldLogs()
    {
        try
        {
            if (_logsDirectory == null) return;
            var directoryInfo = new DirectoryInfo(_logsDirectory);
            if (!directoryInfo.Exists) return;

            var files = directoryInfo.GetFiles("entcapture2_*.log");
            DateTime cutoffDate = DateTime.Now.AddDays(-MaxLogRetentionDays);

            foreach (var file in files)
            {
                if (file.CreationTime < cutoffDate || file.LastWriteTime < cutoffDate)
                {
                    file.Delete();
                }
            }
        }
        catch
        {
            // Silently fail if cleanup fails
        }
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

