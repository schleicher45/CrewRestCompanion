using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace CrewRestConfigurator
{
    /// <summary>
    /// Findet den vPilot Plugins-Ordner, installiert die DLL und hält den Ordner sauber:
    /// genau eine Sicherung, alte Versionen in einem Unterordner, maximal drei davon.
    /// </summary>
    internal static class PluginInstaller
    {
        /// <summary>Zieldateiname - bewusst ohne Leerzeichen, damit er zur Anleitung passt.</summary>
        public const string TargetDllName = "CrewRestCompanion.dll";

        public const string ConfigFileName = "config.txt";

        /// <summary>Unterordner für abgelöste Versionen. vPilot lädt nur DLLs direkt im Plugins-Ordner.</summary>
        public const string ArchiveDirName = "_alte-versionen";

        /// <summary>So viele alte Versionen bleiben erhalten, ältere werden gelöscht.</summary>
        private const int MaxArchived = 3;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFileW(string fileName);

        public static string VPilotRoot()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "vPilot");
        }

        public static string DefaultPluginDir()
        {
            return Path.Combine(VPilotRoot(), "Plugins");
        }

        public static bool IsVPilotInstalled()
        {
            try
            {
                return File.Exists(Path.Combine(VPilotRoot(), "vPilot.exe"));
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string ConfigPath(string pluginDir)
        {
            return Path.Combine(pluginDir, ConfigFileName);
        }

        /// <summary>Erkennt Dateien, die von diesem Plugin stammen - auch alte Namen und Sicherungen.</summary>
        private static bool IsOurFile(string path)
        {
            string name = Path.GetFileName(path).Replace(" ", string.Empty).Replace("-", string.Empty);
            return name.IndexOf("CrewRestCompanion", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("ALARM", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Sucht eine Plugin-DLL in der Nähe der Anwendung bzw. im Projektordner.</summary>
        public static string FindSourceDll()
        {
            List<string> roots = new List<string>();
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            roots.Add(appDir);

            DirectoryInfo probe = new DirectoryInfo(appDir);
            for (int i = 0; i < 8 && probe != null; i++)
            {
                roots.Add(probe.FullName);
                probe = probe.Parent;
            }

            string newest = null;
            DateTime newestTime = DateTime.MinValue;

            foreach (string root in roots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                string[] candidates;
                try
                {
                    candidates = Directory.GetFiles(root, "*.dll", SearchOption.AllDirectories);
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (string file in candidates)
                {
                    string name = Path.GetFileName(file).Replace(" ", string.Empty).Replace("-", string.Empty);
                    if (name.IndexOf("CrewRestCompanion", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    DateTime stamp = File.GetLastWriteTimeUtc(file);
                    if (stamp > newestTime)
                    {
                        newestTime = stamp;
                        newest = file;
                    }
                }

                if (newest != null)
                {
                    break;
                }
            }

            return newest ?? string.Empty;
        }

        /// <summary>Beschreibt, was aktuell im Plugins-Ordner liegt.</summary>
        public static string DescribeInstallation(string pluginDir)
        {
            if (string.IsNullOrEmpty(pluginDir))
            {
                return "Kein Plugins-Ordner angegeben.";
            }

            if (!Directory.Exists(pluginDir))
            {
                return IsVPilotInstalled()
                    ? "Der Plugins-Ordner existiert noch nicht - vPilot legt ihn nicht von selbst an. "
                        + "\"Installieren + speichern\" erstellt ihn."
                    : "Hier ist kein vPilot zu finden. Falls vPilot woanders installiert ist, den Ordner "
                        + "oben von Hand auswählen - sonst wird er beim Installieren neu angelegt.";
            }

            List<string> active = new List<string>();
            foreach (string file in Directory.GetFiles(pluginDir, "*.dll", SearchOption.TopDirectoryOnly))
            {
                if (IsOurFile(file))
                {
                    active.Add(file);
                }
            }

            if (active.Count == 0)
            {
                return "Noch nicht installiert - unten auf \"Speichern und installieren\" klicken.";
            }

            List<string> parts = new List<string>();
            foreach (string file in active)
            {
                parts.Add("Version " + VersionOf(file) + " (installiert am "
                    + File.GetLastWriteTime(file).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ")");
            }

            string result = "Aktiv: " + string.Join(" + ", parts.ToArray());

            if (active.Count > 1)
            {
                result += "  ACHTUNG: mehrere Versionen aktiv - vPilot würde doppelt alarmieren. "
                    + "\"Speichern und installieren\" räumt das auf.";
            }

            int archived = CountArchived(pluginDir);
            if (archived > 0)
            {
                result += "  " + archived + " ältere Version(en) liegen zur Sicherheit daneben.";
            }

            if (!File.Exists(ConfigPath(pluginDir)))
            {
                result += "  Einstellungen sind noch nicht gespeichert.";
            }

            return result;
        }

        private static string VersionOf(string file)
        {
            try
            {
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(file);
                if (!string.IsNullOrEmpty(info.FileVersion))
                {
                    return info.FileVersion;
                }
            }
            catch (Exception)
            {
                // Version ist nur Zusatzinfo.
            }
            return "?";
        }

        private static int CountArchived(string pluginDir)
        {
            string archive = Path.Combine(pluginDir, ArchiveDirName);
            if (!Directory.Exists(archive))
            {
                return 0;
            }

            try
            {
                return Directory.GetFiles(archive).Length;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Installiert die DLL als CrewRestCompanion.dll.
        /// Hält genau eine Sicherung (.bak), verschiebt abgelöste Versionen in den
        /// Unterordner _alte-versionen und behält dort höchstens drei.
        /// </summary>
        public static string Install(string sourceDll, string pluginDir, bool tidyUp)
        {
            if (string.IsNullOrEmpty(sourceDll) || !File.Exists(sourceDll))
            {
                throw new FileNotFoundException("Die Plugin-Datei wurde nicht gefunden: " + sourceDll);
            }
            if (string.IsNullOrEmpty(pluginDir))
            {
                throw new DirectoryNotFoundException("Kein Plugins-Ordner angegeben.");
            }

            List<string> log = new List<string>();

            if (!Directory.Exists(pluginDir))
            {
                Directory.CreateDirectory(pluginDir);
                log.Add("Plugins-Ordner neu angelegt: " + pluginDir);
            }

            string target = Path.Combine(pluginDir, TargetDllName);

            if (File.Exists(target) && SameContent(sourceDll, target))
            {
                log.Add("Diese Version ist bereits installiert - nichts geändert.");
            }
            else
            {
                if (File.Exists(target))
                {
                    // Immer dieselbe Sicherungsdatei: es bleibt bei genau einer.
                    string backup = target + ".bak";
                    File.Copy(target, backup, true);
                    log.Add("Vorherige Version " + VersionOf(backup) + " gesichert.");
                }

                File.Copy(sourceDll, target, true);
                log.Add("Plugin installiert, Version " + VersionOf(target) + ".");
            }

            if (Unblock(target))
            {
                log.Add("Windows-Sperre für heruntergeladene Dateien entfernt - vPilot hätte das Plugin sonst ignoriert.");
            }

            if (tidyUp)
            {
                log.AddRange(TidyUp(pluginDir, target));
            }

            return string.Join(Environment.NewLine, log.ToArray());
        }

        /// <summary>
        /// Schiebt abgelöste Versionen und alte Sicherungen in den Unterordner und
        /// behält dort nur die jüngsten Dateien.
        /// </summary>
        private static List<string> TidyUp(string pluginDir, string target)
        {
            List<string> log = new List<string>();
            List<string> toArchive = new List<string>();

            foreach (string file in Directory.GetFiles(pluginDir, "*", SearchOption.TopDirectoryOnly))
            {
                if (string.Equals(file, target, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (string.Equals(file, target + ".bak", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (!IsOurFile(file))
                {
                    continue;
                }

                string extension = Path.GetExtension(file);
                bool isDll = extension.Equals(".dll", StringComparison.OrdinalIgnoreCase);
                bool isOldBackup = Path.GetFileName(file).IndexOf(".bak-", StringComparison.OrdinalIgnoreCase) >= 0
                    || Path.GetFileName(file).IndexOf(".alt-", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isDll || isOldBackup)
                {
                    toArchive.Add(file);
                }
            }

            if (toArchive.Count == 0)
            {
                return PruneArchive(pluginDir);
            }

            string archive = Path.Combine(pluginDir, ArchiveDirName);
            Directory.CreateDirectory(archive);

            foreach (string file in toArchive)
            {
                // Endung .alt, damit die Datei selbst dann nicht geladen wird,
                // wenn eine künftige vPilot-Version Unterordner durchsucht.
                string name = Path.GetFileName(file);
                if (!name.EndsWith(".alt", StringComparison.OrdinalIgnoreCase))
                {
                    name += ".alt";
                }

                string destination = Path.Combine(archive, name);

                try
                {
                    if (File.Exists(destination))
                    {
                        File.Delete(destination);
                    }
                    File.Move(file, destination);
                    log.Add("Ältere Version zur Seite gelegt: " + Path.GetFileName(file));
                }
                catch (Exception ex)
                {
                    log.Add("Konnte " + Path.GetFileName(file) + " nicht verschieben: " + ex.Message
                        + " (läuft vPilot noch?)");
                }
            }

            log.AddRange(PruneArchive(pluginDir));
            return log;
        }

        private static List<string> PruneArchive(string pluginDir)
        {
            List<string> log = new List<string>();
            string archive = Path.Combine(pluginDir, ArchiveDirName);

            if (!Directory.Exists(archive))
            {
                return log;
            }

            try
            {
                List<FileInfo> files = new List<FileInfo>();
                foreach (string file in Directory.GetFiles(archive))
                {
                    files.Add(new FileInfo(file));
                }

                if (files.Count <= MaxArchived)
                {
                    return log;
                }

                files.Sort(delegate(FileInfo a, FileInfo b)
                {
                    return b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc);
                });

                int removed = 0;
                for (int i = MaxArchived; i < files.Count; i++)
                {
                    try
                    {
                        files[i].Delete();
                        removed++;
                    }
                    catch (Exception)
                    {
                        // Datei bleibt liegen, kein Grund zum Abbruch.
                    }
                }

                if (removed > 0)
                {
                    log.Add(removed + " sehr alte Version(en) entfernt - die letzten "
                        + MaxArchived + " bleiben erhalten.");
                }
            }
            catch (Exception ex)
            {
                log.Add("Aufräumen im Unterordner fehlgeschlagen: " + ex.Message);
            }

            return log;
        }

        private static bool SameContent(string a, string b)
        {
            try
            {
                FileInfo fa = new FileInfo(a);
                FileInfo fb = new FileInfo(b);
                if (fa.Length != fb.Length)
                {
                    return false;
                }

                using (FileStream sa = File.OpenRead(a))
                using (FileStream sb = File.OpenRead(b))
                {
                    byte[] bufferA = new byte[8192];
                    byte[] bufferB = new byte[8192];
                    int read;
                    while ((read = sa.Read(bufferA, 0, bufferA.Length)) > 0)
                    {
                        int readB = sb.Read(bufferB, 0, bufferB.Length);
                        if (read != readB)
                        {
                            return false;
                        }
                        for (int i = 0; i < read; i++)
                        {
                            if (bufferA[i] != bufferB[i])
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Entfernt den Alternate Data Stream "Zone.Identifier". Windows setzt ihn bei
        /// heruntergeladenen Dateien; vPilot lädt eine so markierte DLL nicht.
        /// </summary>
        private static bool Unblock(string file)
        {
            try
            {
                return DeleteFileW(file + ":Zone.Identifier");
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void OpenInExplorer(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            Directory.CreateDirectory(path);
            Process.Start("explorer.exe", "\"" + path + "\"");
        }
    }
}
