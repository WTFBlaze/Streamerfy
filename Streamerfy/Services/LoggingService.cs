using System.IO;
using System.Runtime.CompilerServices;

namespace Streamerfy.Services
{
    public static class LoggingService
    {
        private const int MaxLogFiles = 8;
        private static string _timestampedLogPath;
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;

            CleanupOldLogs();

            // Reset latest.log
            File.WriteAllText(App.LatestLogPath, string.Empty);

            // Create new timestamped log file
            string timestamp = DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss");
            _timestampedLogPath = Path.Combine(App.LogsFolder, $"{timestamp}.log");
            AddLog($"Log started at {DateTime.Now:G}", ConsoleColor.Green);

            _initialized = true;
        }

        public static void AddLog(string message, ConsoleColor color = ConsoleColor.Gray, [CallerFilePath] string filePath = "")
        {
            string className = Path.GetFileNameWithoutExtension(filePath); // e.g., "MainWindow"
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string formatted = $"[{timestamp}] ({className}) : {message}";

            Console.ForegroundColor = color;
            Console.WriteLine(formatted);
            Console.ResetColor();

            try
            {
                File.AppendAllText(App.LatestLogPath, formatted + Environment.NewLine);
                File.AppendAllText(_timestampedLogPath, formatted + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Logger] Failed to write log: {ex.Message}");
            }
        }

        #region Internal Methods
        private static void CleanupOldLogs()
        {
            var logFiles = Directory.GetFiles(App.LogsFolder, "*.log")
                .OrderBy(File.GetCreationTime)
                .ToList();

            while (logFiles.Count >= MaxLogFiles)
            {
                try
                {
                    File.Delete(logFiles[0]);
                    logFiles.RemoveAt(0);
                }
                catch { break; }
            }
        }
        #endregion
    }
}
