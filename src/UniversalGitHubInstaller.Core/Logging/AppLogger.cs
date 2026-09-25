using System;
using System.IO;

namespace UniversalGitHubInstaller.Core.Logging;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public class AppLogger
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "UniversalGitHubProjectInstaller", "logs");

    private readonly string _logFile;
    private readonly object _lock = new();

    public AppLogger(string category = "app")
    {
        if (!Directory.Exists(LogDir))
            Directory.CreateDirectory(LogDir);

        _logFile = Path.Combine(LogDir, $"{category}-{DateTime.Now:yyyy-MM-dd}.log");
    }

    public void Log(LogLevel level, string message, string? detail = null)
    {
        var entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
        if (detail != null)
            entry += $"\n  Detail: {detail}";

        lock (_lock)
        {
            File.AppendAllText(_logFile, entry + Environment.NewLine);
        }
    }

    public void Info(string message) => Log(LogLevel.Info, message);
    public void Warning(string message, string? detail = null) => Log(LogLevel.Warning, message, detail);
    public void Error(string message, string? detail = null) => Log(LogLevel.Error, message, detail);
    public void Debug(string message) => Log(LogLevel.Debug, message);

    public static string GetLogDirectory() => LogDir;
}
