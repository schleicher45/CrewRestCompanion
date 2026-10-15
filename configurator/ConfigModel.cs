using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace CrewRestConfigurator
{
    /// <summary>
    /// Liest und schreibt die config.txt des Crew Rest Companion (Plugin ab v1.2.0).
    /// Alte Dateien aus v1.0.0 und v1.1.0 werden mitgelesen.
    /// Unbekannte Schlüssel bleiben beim Speichern erhalten.
    /// </summary>
    internal sealed class ConfigModel
    {
        // --- Bark ---
        public string BarkDeviceKey = string.Empty;
        public string BarkServerUrl = "https://api.day.app/";
        public string AlarmSound = "multiwayinvitation";
        public string AlarmLevel = "critical";
        public int AlarmVolume = 10;
        public bool AlarmCall = true;
        public string InfoSound = "anticipate";
        public string Group = "VATSIM";

        // --- E-Mail ---
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

        public readonly List<KeyValuePair<string, string>> Unknown = new List<KeyValuePair<string, string>>();

        private static readonly string[] KnownKeys =
        {
            "BarkDeviceKey", "BarkServerUrl", "AlarmSound", "Sound", "AlarmLevel", "Level",
            "AlarmVolume", "Volume", "AlarmCall", "Call", "InfoSound", "Group",
            "EmailAddress", "EmailAppPassword", "SmtpHost", "SmtpPort", "SmtpUseSsl",
            "RepeatCount", "RepeatDelayS", "CooldownS", "StartupMessage", "StartupNotification", "LogLevel",
            "NotifyPilotDMs", "AlarmOnSelcal", "AlarmOnRadioMessage",
            "NotifyControllerOnline", "ControllerFilter", "ControllerQuietSeconds", "UiTheme"
        };

        private static bool IsKnown(string key)
        {
            foreach (string k in KnownKeys)
            {
                if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public static ConfigModel Load(string path)
        {
            ConfigModel cfg = new ConfigModel();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return cfg;
            }

            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)
                    || line.StartsWith(";", StringComparison.Ordinal))
                {
                    continue;
                }

                int sep = line.IndexOf('=');
                if (sep <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, sep).Trim();
                string value = line.Substring(sep + 1).Trim();

                switch (key.ToLowerInvariant())
                {
                    case "barkdevicekey":
                        cfg.BarkDeviceKey = CleanPlaceholder(value,
                            "HIER_DEIN_KEY_EINFUEGEN", "HIER_DEIN_KEY_EINFÜGEN", "D_E_I_N_B_A_R_K_K_E_Y", "DEIN_KEY");
                        break;
                    case "barkserverurl": cfg.BarkServerUrl = Fallback(value, cfg.BarkServerUrl); break;
                    case "sound":
                    case "alarmsound": cfg.AlarmSound = Fallback(value, cfg.AlarmSound); break;
                    case "level":
                    case "alarmlevel": cfg.AlarmLevel = Fallback(value, cfg.AlarmLevel); break;
                    case "volume":
                    case "alarmvolume": cfg.AlarmVolume = ParseInt(value, cfg.AlarmVolume, 0, 10); break;
                    case "call":
                    case "alarmcall": cfg.AlarmCall = ParseInt(value, cfg.AlarmCall ? 1 : 0, 0, 1) == 1; break;
                    case "infosound": cfg.InfoSound = Fallback(value, cfg.InfoSound); break;
                    case "group": cfg.Group = Fallback(value, cfg.Group); break;

                    case "emailaddress":
                        cfg.EmailAddress = CleanPlaceholder(value, "deine.email@outlook.de", "deine.email@outlook.com");
                        break;
                    case "emailapppassword":
                        cfg.EmailAppPassword = Secret.IsProtected(value)
                            ? Secret.Unprotect(value)
                            : CleanPlaceholder(value, "dein_app_passwort", "dein_app_password");
                        break;
                    case "smtphost": cfg.SmtpHost = Fallback(value, cfg.SmtpHost); break;
                    case "smtpport": cfg.SmtpPort = ParseInt(value, cfg.SmtpPort, 1, 65535); break;
                    case "smtpusessl": cfg.SmtpUseSsl = ParseBool(value, cfg.SmtpUseSsl); break;

                    case "repeatcount": cfg.RepeatCount = ParseInt(value, cfg.RepeatCount, 1, 100); break;
                    case "repeatdelays": cfg.RepeatDelayS = ParseInt(value, cfg.RepeatDelayS, 1, 600); break;
                    case "cooldowns": cfg.CooldownS = ParseInt(value, cfg.CooldownS, 0, 3600); break;
                    case "startupmessage": cfg.StartupMessage = ParseBool(value, cfg.StartupMessage); break;
                    case "startupnotification": cfg.StartupNotification = ParseBool(value, cfg.StartupNotification); break;
                    case "loglevel": cfg.LogLevel = Fallback(value.ToLowerInvariant(), cfg.LogLevel); break;

                    case "notifypilotdms": cfg.NotifyPilotDMs = ParseBool(value, cfg.NotifyPilotDMs); break;
                    case "alarmonselcal": cfg.AlarmOnSelcal = ParseBool(value, cfg.AlarmOnSelcal); break;
                    case "alarmonradiomessage": cfg.AlarmOnRadioMessage = ParseBool(value, cfg.AlarmOnRadioMessage); break;
                    case "notifycontrolleronline": cfg.NotifyControllerOnline = ParseBool(value, cfg.NotifyControllerOnline); break;
                    case "controllerfilter": cfg.ControllerFilter = value; break;
                    case "controllerquietseconds": cfg.ControllerQuietSeconds = ParseInt(value, cfg.ControllerQuietSeconds, 0, 600); break;
                    case "uitheme": break; // alter Schluessel, wird nicht mehr geschrieben

                    default:
                        if (!IsKnown(key))
                        {
                            cfg.Unknown.Add(new KeyValuePair<string, string>(key, value));
                        }
                        break;
                }
            }

            return cfg;
        }

        private static string Fallback(string value, string fallback)
        {
            return value.Length > 0 ? value : fallback;
        }

        private static string CleanPlaceholder(string value, params string[] placeholders)
        {
            foreach (string p in placeholders)
            {
                if (string.Equals(value, p, StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }
            }
            return value;
        }

        private static int ParseInt(string value, int fallback, int min, int max)
        {
            int parsed;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                && parsed >= min && parsed <= max)
            {
                return parsed;
            }
            return fallback;
        }

        private static bool ParseBool(string value, bool fallback)
        {
            string v = value.Trim().ToLowerInvariant();
            if (v == "1" || v == "true" || v == "ja" || v == "yes" || v == "on") { return true; }
            if (v == "0" || v == "false" || v == "nein" || v == "no" || v == "off") { return false; }
            return fallback;
        }

        public List<string> Validate()
        {
            List<string> problems = new List<string>();

            bool bark = BarkDeviceKey.Trim().Length > 0;
            bool mail = EmailAddress.Trim().Length > 0 || EmailAppPassword.Length > 0;

            if (!bark && !mail)
            {
                problems.Add("Weder Bark noch E-Mail ist konfiguriert - das Plugin kann nichts melden.");
            }

            if (bark)
            {
                if (BarkDeviceKey.IndexOf('/') >= 0
                    || BarkDeviceKey.IndexOf("http", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    problems.Add("Bark Device Key: bitte nur den Key eintragen, nicht die komplette URL.");
                }
                if (!BarkServerUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    problems.Add("Bark Server URL muss mit http:// oder https:// beginnen.");
                }
            }

            if (mail)
            {
                if (EmailAddress.IndexOf('@') <= 0)
                {
                    problems.Add("E-Mail-Adresse sieht nicht wie eine Adresse aus.");
                }
                if (EmailAppPassword.Length == 0)
                {
                    problems.Add("E-Mail-App-Passwort fehlt.");
                }
                else if (EmailAppPassword.Length < 8)
                {
                    problems.Add("E-Mail-App-Passwort ist sehr kurz - ist es wirklich das App-Passwort?");
                }
                if (SmtpHost.Trim().Length == 0)
                {
                    problems.Add("SMTP-Host fehlt.");
                }
            }

            if (NotifyControllerOnline && ControllerFilter.Trim().Length == 0)
            {
                problems.Add("Lotsen-Meldung ist ohne Filter aktiv - das meldet jeden Lotsen weltweit.");
            }

            return problems;
        }

        public void Save(string path)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# --- Crew Rest Companion Config ---");
            sb.AppendLine("# Erzeugt vom Configurator am "
                + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            sb.AppendLine("# Diese Datei muss direkt neben der DLL im vPilot Plugins-Ordner liegen.");
            sb.AppendLine();

            sb.AppendLine("# 1. BARK (Push aufs Handy)");
            sb.AppendLine("# Nur den Device Key eintragen, nicht die komplette URL. Leer lassen = Bark aus.");
            sb.AppendLine("BarkDeviceKey=" + BarkDeviceKey.Trim());
            sb.AppendLine("BarkServerUrl=" + BarkServerUrl.Trim());
            sb.AppendLine("# Töne und Lautstärke des Weckalarms.");
            sb.AppendLine("AlarmSound=" + AlarmSound.Trim());
            sb.AppendLine("AlarmLevel=" + AlarmLevel.Trim());
            sb.AppendLine("AlarmVolume=" + AlarmVolume.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("# AlarmCall=1: Dauerton, das Handy klingelt durch bis zum Wegwischen.");
            sb.AppendLine("AlarmCall=" + (AlarmCall ? "1" : "0"));
            sb.AppendLine("InfoSound=" + InfoSound.Trim());
            sb.AppendLine("Group=" + Group.Trim());
            sb.AppendLine();

            sb.AppendLine("# 2. E-MAIL-ALARM (iPhone-Wecker über Kurzbefehl, Betreff ALARM-VPILOT)");
            sb.AppendLine("# Das Passwort steht DPAPI-verschlüsselt hier (Präfix enc:) und ist nur");
            sb.AppendLine("# für diesen Windows-Benutzer auf diesem PC lesbar.");
            sb.AppendLine("EmailAddress=" + EmailAddress.Trim());
            sb.AppendLine("EmailAppPassword=" + Secret.Protect(EmailAppPassword));
            sb.AppendLine("SmtpHost=" + SmtpHost.Trim());
            sb.AppendLine("SmtpPort=" + SmtpPort.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("SmtpUseSsl=" + (SmtpUseSsl ? "true" : "false"));
            sb.AppendLine();

            sb.AppendLine("# 3. AUSLOESER");
            sb.AppendLine("# Alarm gibt es immer bei Supervisor, ATC-Nachricht und \"contact me\".");
            sb.AppendLine("AlarmOnSelcal=" + Bool(AlarmOnSelcal));
            sb.AppendLine("AlarmOnRadioMessage=" + Bool(AlarmOnRadioMessage));
            sb.AppendLine("NotifyPilotDMs=" + Bool(NotifyPilotDMs));
            sb.AppendLine("NotifyControllerOnline=" + Bool(NotifyControllerOnline));
            sb.AppendLine("# Leer = alle Lotsen. Sonst Präfixe, z. B.: EDDL,EDGG,EDUU");
            sb.AppendLine("ControllerFilter=" + ControllerFilter.Trim());
            sb.AppendLine("# Ruhezeit nach dem Verbinden, damit die bereits online stehenden Lotsen keine Flut auslösen.");
            sb.AppendLine("ControllerQuietSeconds=" + ControllerQuietSeconds.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine();

            sb.AppendLine("# 4. VERHALTEN");
            sb.AppendLine("# Normalerweise 1: der Dauerton (AlarmCall=1) laesst Bark von selbst durchklingeln.");
            sb.AppendLine("# Nur hochsetzen, wenn der Alarm zusaetzlich mehrfach geschickt werden soll.");
            sb.AppendLine("RepeatCount=" + RepeatCount.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("RepeatDelayS=" + RepeatDelayS.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("# Sperrzeit nach einem Alarm, damit eine Nachrichtenserie nicht mehrfach weckt.");
            sb.AppendLine("CooldownS=" + CooldownS.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("# Startmeldung ins vPilot-Debugfenster (vPilot hat keine Schnittstelle in den Chat).");
            sb.AppendLine("StartupMessage=" + Bool(StartupMessage));
            sb.AppendLine("# Leiser Push aufs Handy beim Start - auf false stellen, wenn er stört.");
            sb.AppendLine("StartupNotification=" + Bool(StartupNotification));
            sb.AppendLine("# off = keine Logdatei, info = Standard, debug = ausführlich.");
            sb.AppendLine("LogLevel=" + LogLevel);


            if (Unknown.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("# 6. WEITERE EINSTELLUNGEN (vom Configurator nicht veraendert)");
                foreach (KeyValuePair<string, string> kv in Unknown)
                {
                    sb.AppendLine(kv.Key + "=" + kv.Value);
                }
            }

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        }

        private static string Bool(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
