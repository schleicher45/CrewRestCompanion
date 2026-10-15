using System;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;

namespace CrewRestConfigurator
{
    /// <summary>
    /// Sendet dieselben Benachrichtigungen wie das Plugin - zum Testen ohne vPilot und ohne VATSIM-Verbindung.
    /// </summary>
    internal static class AlarmTester
    {
        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            return client;
        }

        public static async Task<string> SendBarkAsync(ConfigModel cfg, bool critical)
        {
            string key = cfg.BarkDeviceKey.Trim();
            if (key.Length == 0)
            {
                return "Bark: kein Device Key eingetragen - übersprungen.";
            }

            string baseUrl = cfg.BarkServerUrl.Trim();
            if (baseUrl.Length == 0)
            {
                baseUrl = "https://api.day.app/";
            }
            if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
            {
                baseUrl += "/";
            }

            string title = critical ? "ALARM: EDDL_TWR" : "DM von DLH123";
            string body = critical
                ? "TEST: please contact me on 118.300"
                : "TEST: So sieht eine normale DM-Info aus.";

            string url = baseUrl + Uri.EscapeDataString(key)
                + "/" + Uri.EscapeDataString(title)
                + "/" + Uri.EscapeDataString(body)
                + "?level=" + Uri.EscapeDataString(critical ? cfg.AlarmLevel : "active")
                + "&volume=" + (critical ? cfg.AlarmVolume : 5)
                + "&call=" + (critical && cfg.AlarmCall ? 1 : 0)
                + "&sound=" + Uri.EscapeDataString(critical ? cfg.AlarmSound : cfg.InfoSound)
                + "&group=" + Uri.EscapeDataString(cfg.Group);

            try
            {
                HttpResponseMessage response = await Http.GetAsync(url).ConfigureAwait(false);
                string payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return "Bark: Push abgesetzt (HTTP " + (int)response.StatusCode + "). Antwort: " + Shorten(payload);
                }
                return "Bark FEHLER: HTTP " + (int)response.StatusCode + " - " + Shorten(payload);
            }
            catch (Exception ex)
            {
                return "Bark FEHLER: " + ex.Message;
            }
        }

        public static async Task<string> SendTestMailAsync(ConfigModel cfg)
        {
            string address = cfg.EmailAddress.Trim();
            if (address.Length == 0 || cfg.EmailAppPassword.Length == 0)
            {
                return "E-Mail: Adresse oder App-Passwort fehlt - übersprungen.";
            }

            return await Task.Run(() =>
            {
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    using (MailMessage mail = new MailMessage())
                    using (SmtpClient smtp = new SmtpClient(cfg.SmtpHost.Trim(), cfg.SmtpPort))
                    {
                        mail.From = new MailAddress(address, "Crew Rest Companion");
                        mail.To.Add(address);
                        mail.Subject = "ALARM-VPILOT";
                        mail.Body = "TEST des Crew Rest Companion vom "
                            + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")
                            + ". Wenn dein iPhone jetzt klingelt, ist die Kette komplett.";
                        mail.IsBodyHtml = false;

                        smtp.EnableSsl = cfg.SmtpUseSsl;
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(address, cfg.EmailAppPassword);
                        smtp.Timeout = 20000;

                        smtp.Send(mail);
                    }

                    return "E-Mail: Testmail mit Betreff \"ALARM-VPILOT\" an " + address + " versendet.";
                }
                catch (SmtpException ex)
                {
                    return "MAIL FEHLER (" + ex.StatusCode + "): " + ex.Message
                        + " | Haeufigste Ursache: Login-Passwort statt App-Passwort, oder 2FA nicht aktiviert.";
                }
                catch (Exception ex)
                {
                    return "MAIL FEHLER: " + ex.Message;
                }
            }).ConfigureAwait(false);
        }

        private static string Shorten(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "(leer)";
            }
            text = text.Replace("\r", " ").Replace("\n", " ").Trim();
            return text.Length <= 160 ? text : text.Substring(0, 160) + "...";
        }
    }
}
