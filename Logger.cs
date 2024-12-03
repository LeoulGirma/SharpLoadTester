using System;
public static class Logger
{
    private static LogLevel _currentLogLevel = LogLevel.Info;
    public static void SetLogLevel(string logLevel)
    {
        if (Enum.TryParse<LogLevel>(logLevel, true, out var level))
        {
            _currentLogLevel = level;
        }
    }
    public static void LogDebug(string message)
    {
        if (_currentLogLevel <= LogLevel.Debug)
        {
            Console.WriteLine($"DEBUG: {message}");
        }
    }
    public static void LogInfo(string message)
    {
        if (_currentLogLevel <= LogLevel.Info)
        {
            Console.WriteLine(message);
        }
    }
    public static void LogWarning(string message)
    {
        if (_currentLogLevel <= LogLevel.Warning)
        {
            Console.WriteLine($"WARNING: {message}");
        }
    }
    public static void LogError(string message)
    {
        if (_currentLogLevel <= LogLevel.Error)
        {
            Console.WriteLine($"ERROR: {message}");
        }
    }
    private enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}
