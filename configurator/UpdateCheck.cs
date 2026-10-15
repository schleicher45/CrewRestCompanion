using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CrewRestConfigurator
{
    /// <summary>Ergebnis der Update-Prüfung.</summary>
    internal sealed class UpdateInfo
    {
        public string Version = string.Empty;
        public string Url = string.Empty;
        public string Notes = string.Empty;

        /// <summary>true, wenn die gefundene Version neuer ist als die laufende.</summary>
        public bool IsNewer;

        /// <summary>Leer, wenn die Pruefung geklappt hat - sonst der Grund in Klartext.</summary>
        public string Problem = string.Empty;
    }

    /// <summary>
    /// Fragt eine kleine Datei auf GitHub ab und vergleicht die Versionsnummer mit der
    /// eigenen. Schlägt die Abfrage fehl, passiert nichts - ein Update-Hinweis ist nett,
    /// aber nichts, wofür ein Start scheitern dürfte.
    /// </summary>
    internal static class UpdateCheck
    {
        /// <summary>GitHub-Benutzername - hier eintragen, sonst findet die Prüfung nichts.</summary>
        public const string Owner = "schleicher45";

        public const string Repository = "CrewRestCompanion";

        public static string VersionUrl
        {
            get
            {
                return "https://raw.githubusercontent.com/" + Owner + "/" + Repository
                    + "/main/version.json";
            }
        }

        public static string ReleaseUrl
        {
            get { return "https://github.com/" + Owner + "/" + Repository + "/releases/latest"; }
        }

        public static string CurrentVersion()
        {
            try
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                return version.Major + "." + version.Minor + "." + version.Build;
            }
            catch (Exception)
            {
                return "0.0.0";
            }
        }

        /// <summary>
        /// Fragt die Versionsdatei ab. Gibt immer ein Ergebnis zurueck - entweder mit
        /// Versionsangabe oder mit einem Grund, warum es nicht geklappt hat.
        /// </summary>
        public static async Task<UpdateInfo> CheckAsync()
        {
            UpdateInfo info = new UpdateInfo();
            info.Url = ReleaseUrl;

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                using (HttpClient http = new HttpClient())
                {
                    http.Timeout = TimeSpan.FromSeconds(8);
                    http.DefaultRequestHeaders.Add("User-Agent", Repository + "-Configurator");

                    HttpResponseMessage antwort = await http.GetAsync(VersionUrl).ConfigureAwait(false);

                    if (antwort.StatusCode == HttpStatusCode.NotFound)
                    {
                        info.Problem = "version.json liegt nicht im Repository (noch nicht hochgeladen?)";
                        return info;
                    }

                    if (!antwort.IsSuccessStatusCode)
                    {
                        info.Problem = "Server antwortet mit HTTP " + (int)antwort.StatusCode;
                        return info;
                    }

                    string json = await antwort.Content.ReadAsStringAsync().ConfigureAwait(false);

                    info.Version = Read(json, "version");
                    info.Notes = Read(json, "notes");

                    string url = Read(json, "url");
                    if (url.Length > 0)
                    {
                        info.Url = url;
                    }

                    if (info.Version.Length == 0)
                    {
                        info.Problem = "In der version.json steht keine Versionsnummer.";
                        return info;
                    }

                    info.IsNewer = IsNewer(info.Version, CurrentVersion());
                    return info;
                }
            }
            catch (TaskCanceledException)
            {
                info.Problem = "Zeitueberschreitung";
                return info;
            }
            catch (Exception ex)
            {
                info.Problem = ex.Message;
                return info;
            }
        }

        private static string Read(string json, string key)
        {
            Match match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"([^\"]*)\"",
                RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        /// <summary>Vergleicht "1.3.0" mit "1.2.0" - rein numerisch, Teil für Teil.</summary>
        private static bool IsNewer(string candidate, string current)
        {
            int[] a = Parse(candidate);
            int[] b = Parse(current);

            if (a == null || b == null)
            {
                return false;
            }

            for (int i = 0; i < 3; i++)
            {
                if (a[i] != b[i])
                {
                    return a[i] > b[i];
                }
            }
            return false;
        }

        private static int[] Parse(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return null;
            }

            // Fuehrende Buchstaben und Punkte abschneiden: "v1.2.0", "v.1.2.0" und "1.2.0"
            // sollen alle zur selben Zahlenfolge fuehren.
            string bereinigt = version.Trim();
            while (bereinigt.Length > 0 && !char.IsDigit(bereinigt[0]))
            {
                bereinigt = bereinigt.Substring(1);
            }

            string[] parts = bereinigt.Split('.');
            int[] numbers = new int[3];

            for (int i = 0; i < 3; i++)
            {
                if (i >= parts.Length
                    || !int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out numbers[i]))
                {
                    numbers[i] = 0;
                }
            }
            return numbers;
        }
    }
}
