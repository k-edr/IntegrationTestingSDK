using System;
using System.IO;
using System.Threading;

namespace IntegrationTestingSDK.Plugin.Infrastructure
{
    /// <summary>
    ///     File logger writing to %APPDATA%\SpaceEngineers\IntegrationTestingSDK.log.
    /// </summary>
    public static class Logger
    {
        private static readonly string LogFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SpaceEngineers", "IntegrationTestingSDK.log");

        private static readonly object Lock = new();
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                var dir = Path.GetDirectoryName(LogFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            catch { }

            Info("IntegrationTestingSDK.Plugin started");
        }

        public static void Info(string message) => Write("INFO", message);
        public static void Warn(string message) => Write("WARN", message);
        public static void Error(string message) => Write("ERROR", message);
        public static void Error(string message, Exception ex) =>
            Write("ERROR", $"{message}: {ex}");

        private static void Write(string level, string message)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";

            lock (Lock)
            {
                try { File.AppendAllText(LogFile, line + Environment.NewLine); }
                catch { }
            }

            try { VRage.Utils.MyLog.Default?.WriteLine($"[IntegrationTestingSDK] {line}"); }
            catch { }
        }
    }
}
