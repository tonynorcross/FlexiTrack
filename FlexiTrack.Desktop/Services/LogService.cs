using System.IO;

namespace FlexiTrack.Desktop.Services;

public static class LogService
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FlexiTrack");

    private static readonly string LogFile = Path.Combine(LogDirectory, "error.log");

    static LogService()
    {
        if (!Directory.Exists(LogDirectory))
        {
            Directory.CreateDirectory(LogDirectory);
        }
    }

    public static void Log(string message)
    {
        try
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            File.AppendAllText(LogFile, logEntry);
        }
        catch
        {
            // Ignore logging errors
        }
    }

    public static void LogError(string context, Exception ex)
    {
        var message = $"ERROR in {context}: {ex.Message}{Environment.NewLine}Stack: {ex.StackTrace}";
        if (ex.InnerException != null)
        {
            message += $"{Environment.NewLine}Inner: {ex.InnerException.Message}{Environment.NewLine}Inner Stack: {ex.InnerException.StackTrace}";
        }
        Log(message);
    }

    public static string GetLogFilePath() => LogFile;
}
