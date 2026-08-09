using System.Text;

namespace GoConsoleOS.Shared;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public static class Logger
{
    private static readonly object Lock = new();
    private static string _logPath = "";
    private static LogLevel _minLevel = LogLevel.Info;
    private static int _maxSizeMb = 10;
    private static readonly string[] LevelNames = { "DBG", "INF", "WRN", "ERR" };

    public static void Initialize(string logDirectory, string minLevel = "info", int maxSizeMb = 10)
    {
        try
        {
            Directory.CreateDirectory(logDirectory);
            _logPath = Path.Combine(logDirectory, $"goconsole_{DateTime.Now:yyyyMMdd}.log");
            _maxSizeMb = Math.Max(1, maxSizeMb);

            _minLevel = minLevel.ToLowerInvariant() switch
            {
                "debug" => LogLevel.Debug,
                "warning" => LogLevel.Warning,
                "error" => LogLevel.Error,
                _ => LogLevel.Info
            };
        }
        catch { _logPath = ""; }
    }

    public static void Debug(string message) => Write(LogLevel.Debug, message);
    public static void Info(string message) => Write(LogLevel.Info, message);
    public static void Warn(string message) => Write(LogLevel.Warning, message);
    public static void Error(string message) => Write(LogLevel.Error, message);

    private static void Write(LogLevel level, string message)
    {
        if (level < _minLevel) return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var line = $"[{timestamp}] [{LevelNames[(int)level]}] {message}";

        Console.WriteLine(line);

        if (string.IsNullOrEmpty(_logPath)) return;

        lock (Lock)
        {
            try
            {
                File.AppendAllText(_logPath, line + Environment.NewLine, Encoding.UTF8);

                if (new FileInfo(_logPath).Length > _maxSizeMb * 1024L * 1024L)
                    RotateLog();
            }
            catch { }
        }
    }

    private static void RotateLog()
    {
        try
        {
            var dir = Path.GetDirectoryName(_logPath) ?? ".";
            var name = Path.GetFileNameWithoutExtension(_logPath);
            var ext = Path.GetExtension(_logPath);
            var rotated = Path.Combine(dir, $"{name}_{DateTime.Now:HHmmss}{ext}");
            File.Move(_logPath, rotated, false);

            var files = Directory.GetFiles(dir, $"{name}_*{ext}")
                                 .OrderByDescending(f => f)
                                 .Skip(30);
            foreach (var f in files)
                try { File.Delete(f); } catch { }
        }
        catch { }
    }

    public static void Flush() { }
}
