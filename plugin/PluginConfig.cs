using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace CrewRestCompanion
{
    /// <summary>
    /// Alle Einstellungen aus der config.txt. Unbekannte Schlüssel werden ignoriert,
    /// fehlende Schlüssel behalten die hier gesetzten Standardwerte.
    /// </summary>
    internal sealed class PluginConfig
    {
        public const string FileName = "config.txt";

        // --- Bark ---
        public string BarkDeviceKey = string.Empty;
        public string BarkServerUrl = "https://api.day.app/";
        public string AlarmSound = "multiwayinvitation";
        public string AlarmLevel = "critical";
        public int AlarmVolume = 10;
        public int AlarmCall = 1;
        public string InfoSound = "anticipate";
        public string Group = "VATSIM";

        // --- E-Mail (iPhone-Wecker über Kurzbefehl) ---
        public string EmailAddress = string.Empty;
        public string EmailAppPassword = string.Empty;
        public string SmtpHost = "smtp.office365.com";
        public int SmtpPort = 587;
        public bool SmtpUseSsl = true;

        // --- Verhalten ---
        public int RepeatCount = 1;
        public int RepeatDelayS = 20;
        public int CooldownS = 60;
        public bool StartupMessage = true;
        public bool StartupNotification = true;
        public string LogLevel = "info";

        // --- Auslöser ---
        public bool NotifyPilotDMs;
        public bool AlarmOnSelcal = true;
        public bool AlarmOnRadioMessage = true;
        public bool NotifyControllerOnline;
        public string ControllerFilter = string.Empty;
        public int ControllerQuietSeconds = 90;

        public bool HasBark
        {
            get { return BarkDeviceKey.Trim().Length > 0; }
        }

        public bool HasMail
        {
            get { return EmailAddress.Trim().Length > 0 && EmailAppPassword.Length > 0; }
        }

        /// <summary>Präfixe aus ControllerFilter, z. B. "EDDL,EDGG". Leer = alle Lotsen.</summary>
        public string[] ControllerPrefixes()
        {
            if (ControllerFilter.Trim().Length == 0)
            {
                return new string[0];
            }

            string[] raw = ControllerFilter.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> result = new List<string>();
            foreach (string item in raw)
            {
                string trimmed = item.Trim();
                if (trimmed.Length > 0)
                {
                    result.Add(trimmed);
                }
            }
            return result.ToArray();
        }

        public static PluginConfig Load(string directory)
        {
            PluginConfig cfg = new PluginConfig();
            string path = Path.Combine(directory ?? ".", FileName);

            if (!File.Exists(path))
            {
                return cfg;
            }

            bool plainPasswordFound = false;

            try
            {
                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string[] parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length != 2)
                    {
                        continue;
                    }

                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    // Inline-Kommentare abschneiden - aber nicht im Passwort oder im verschlüsselten Wert.
                    bool keepRaw = key.Equals("EmailAppPassword", StringComparison.OrdinalIgnoreCase);
                    if (!keepRaw)
                    {
                        value = value.Split(new[] { ';', '#' }, 2)[0].Trim();
                    }

                    switch (key.ToLowerInvariant())
                    {
                        case "barkdevicekey": cfg.BarkDeviceKey = value; break;
                        case "barkserverurl": if (value.Length > 0) { cfg.BarkServerUrl = value; } break;
                        case "sound":
                        case "alarmsound": if (value.Length > 0) { cfg.AlarmSound = value; } break;
                        case "level":
                        case "alarmlevel": if (value.Length > 0) { cfg.AlarmLevel = value; } break;
                        case "volume":
                        case "alarmvolume": cfg.AlarmVolume = Int(value, cfg.AlarmVolume, 0, 10); break;
                        case "call":
                        case "alarmcall": cfg.AlarmCall = Int(value, cfg.AlarmCall, 0, 1); break;
                        case "infosound": if (value.Length > 0) { cfg.InfoSound = value; } break;
                        case "group": if (value.Length > 0) { cfg.Group = value; } break;

                        case "emailaddress": cfg.EmailAddress = value; break;
                        case "emailapppassword":
                            if (Secret.IsProtected(value))
                            {
                                cfg.EmailAppPassword = Secret.Unprotect(value);
                            }
                            else
                            {
                                cfg.EmailAppPassword = value;
                                plainPasswordFound = value.Length > 0;
                            }
                            break;
                        case "smtphost": if (value.Length > 0) { cfg.SmtpHost = value; } break;
                        case "smtpport": cfg.SmtpPort = Int(value, cfg.SmtpPort, 1, 65535); break;
                        case "smtpusessl": cfg.SmtpUseSsl = Bool(value, cfg.SmtpUseSsl); break;

                        case "repeatcount": cfg.RepeatCount = Int(value, cfg.RepeatCount, 1, 100); break;
                        case "repeatdelays": cfg.RepeatDelayS = Int(value, cfg.RepeatDelayS, 1, 600); break;
                        case "cooldowns": cfg.CooldownS = Int(value, cfg.CooldownS, 0, 3600); break;
                        case "startupmessage": cfg.StartupMessage = Bool(value, cfg.StartupMessage); break;
                        case "startupnotification": cfg.StartupNotification = Bool(value, cfg.StartupNotification); break;
                        case "loglevel": if (value.Length > 0) { cfg.LogLevel = value.ToLowerInvariant(); } break;

                        case "notifypilotdms": cfg.NotifyPilotDMs = Bool(value, cfg.NotifyPilotDMs); break;
                        case "alarmonselcal": cfg.AlarmOnSelcal = Bool(value, cfg.AlarmOnSelcal); break;
                        case "alarmonradiomessage": cfg.AlarmOnRadioMessage = Bool(value, cfg.AlarmOnRadioMessage); break;
                        case "notifycontrolleronline": cfg.NotifyControllerOnline = Bool(value, cfg.NotifyControllerOnline); break;
                        case "controllerfilter": cfg.ControllerFilter = value; break;
                        case "controllerquietseconds": cfg.ControllerQuietSeconds = Int(value, cfg.ControllerQuietSeconds, 0, 600); break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("CONFIG: " + ex.Message);
            }

            if (plainPasswordFound)
            {
                ProtectPasswordInFile(path, cfg.EmailAppPassword);
            }

            return cfg;
        }

        /// <summary>
        /// Ersetzt ein im Klartext gefundenes Passwort durch den DPAPI-Wert.
        /// Laeuft genau einmal - danach steht in der Datei nur noch "enc:...".
        /// </summary>
        private static void ProtectPasswordInFile(string path, string plainPassword)
        {
            try
            {
                string protectedValue = Secret.Protect(plainPassword);
                if (!Secret.IsProtected(protectedValue))
                {
                    return;
                }

                string[] lines = File.ReadAllLines(path);
                bool changed = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    Match match = Regex.Match(lines[i], @"^\s*EmailAppPassword\s*=", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        lines[i] = "EmailAppPassword=" + protectedValue;
                        changed = true;
                    }
                }

                if (changed)
                {
                    File.WriteAllLines(path, lines);
                    Log.Info("Mail-Passwort in der config.txt verschlüsselt (DPAPI, nur für diesen Windows-Benutzer lesbar).");
                }
            }
            catch (Exception ex)
            {
                Log.Error("CONFIG-Verschlüsselung: " + ex.Message);
            }
        }

        private static int Int(string value, int fallback, int min, int max)
        {
            int parsed;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                && parsed >= min && parsed <= max)
            {
                return parsed;
            }
            return fallback;
        }

        private static bool Bool(string value, bool fallback)
        {
            string v = value.Trim().ToLowerInvariant();
            if (v == "1" || v == "true" || v == "ja" || v == "yes" || v == "on") { return true; }
            if (v == "0" || v == "false" || v == "nein" || v == "no" || v == "off") { return false; }
            return fallback;
        }
    }
}
