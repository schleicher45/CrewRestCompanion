using System;
using System.IO;

namespace CrewRestCompanion
{
    /// <summary>
    /// Schreibt plugin.log neben die DLL. Stufe wird aus der config.txt gesetzt:
    /// off = gar nichts, info = Standard, debug = ausführlich.
    /// </summary>
    internal static class Log
    {
        private const int LevelOff = 0;
        private const int LevelInfo = 1;
        private const int LevelDebug = 2;

        private static readonly object Sync = new object();
        private static string _path;
        private static int _level = LevelInfo;

        public static void Configure(string directory, string level)
        {
            _path = Path.Combine(directory ?? ".", "plugin.log");

            if (string.IsNullOrEmpty(level)) { _level = LevelInfo; }
            else if (level.StartsWith("off", StringComparison.OrdinalIgnoreCase)) { _level = LevelOff; }
            else if (level.StartsWith("debug", StringComparison.OrdinalIgnoreCase)) { _level = LevelDebug; }
            else { _level = LevelInfo; }
        }

        public static bool IsDebug
        {
            get { return _level >= LevelDebug; }
        }

        public static void Info(string message)
        {
            Write("INFO ", message, LevelInfo);
        }

        public static void Error(string message)
        {
            Write("FEHLER", message, LevelInfo);
        }

        public static void Debug(string message)
        {
            Write("DEBUG", message, LevelDebug);
        }

        private static void Write(string tag, string message, int required)
        {
            if (_level < required || _level == LevelOff || string.IsNullOrEmpty(_path))
            {
                return;
            }

            try
            {
                lock (Sync)
                {
                    File.AppendAllText(_path,
                        string.Format("{0:yyyy-MM-dd HH:mm:ss} [{1}] {2}{3}",
                            DateTime.Now, tag, message, Environment.NewLine));
                }
            }
            catch
            {
                // Logging darf den Flugbetrieb nie stören.
            }
        }
    }
}
