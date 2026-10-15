using System;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace CrewRestCompanion
{
    /// <summary>
    /// Versand aller Benachrichtigungen: Bark-Push (mit Wiederholung) und E-Mail-Alarm.
    /// Jeder Versand läuft im Hintergrund - der vPilot-Thread wird nie blockiert.
    /// </summary>
    internal sealed class Notifier
    {
        private static readonly HttpClient Http = CreateHttpClient();

        private readonly PluginConfig _config;
        private readonly object _sync = new object();
        private DateTime _lastAlarmUtc = DateTime.MinValue;
        private CancellationTokenSource _running;

        public Notifier(PluginConfig config)
        {
            _config = config;
        }

        private static HttpClient CreateHttpClient()
        {
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            return client;
        }

        /// <summary>
        /// Loest einen Alarm aus: Bark mit Wiederholung plus eine E-Mail.
        /// Gibt false zurück, wenn die Sperrzeit noch läuft.
        /// </summary>
        public bool Alarm(string reason, string from, string message)
        {
            lock (_sync)
            {
                DateTime now = DateTime.UtcNow;
                if (now - _lastAlarmUtc < TimeSpan.FromSeconds(_config.CooldownS))
                {
                    Log.Debug("Alarm unterdrueckt (Sperrzeit läuft): " + reason + " / " + from);
                    return false;
                }
                _lastAlarmUtc = now;
            }

            Log.Info("ALARM (" + reason + ") von " + from + ": " + message);

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationTokenSource previous = Interlocked.Exchange(ref _running, cts);
            if (previous != null)
            {
                previous.Cancel();
                previous.Dispose();
            }

            string title = "ALARM: " + from;
            string body = string.IsNullOrEmpty(message) ? reason : message;

            Task.Run(() => RunAlarmAsync(title, body, reason, from, message, cts.Token));
            return true;
        }

        /// <summary>Leise Info ohne Weckfunktion (Piloten-DM, Lotse online, Start).</summary>
        public void Info(string title, string body, string sound, int volume)
        {
            if (!_config.HasBark)
            {
                return;
            }

            string url = BuildBarkUrl(title, body, "active", volume, 0, sound);
            Task.Run(() => SendBarkAsync(url));
        }

        private async Task RunAlarmAsync(string title, string body, string reason,
            string from, string message, CancellationToken token)
        {
            if (_config.HasMail)
            {
                await SendAlarmMailAsync(reason, from, message).ConfigureAwait(false);
            }

            if (!_config.HasBark)
            {
                return;
            }

            string url = BuildBarkUrl(title, body, _config.AlarmLevel, _config.AlarmVolume,
                _config.AlarmCall, _config.AlarmSound);

            for (int i = 0; i < _config.RepeatCount; i++)
            {
                if (token.IsCancellationRequested)
                {
                    Log.Debug("Alarmwiederholung abgebrochen nach " + i + " Durchgängen.");
                    return;
                }

                await SendBarkAsync(url).ConfigureAwait(false);

                if (i < _config.RepeatCount - 1)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(_config.RepeatDelayS), token).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                }
            }
        }

        /// <summary>Bricht laufende Alarmwiederholungen ab (Disconnect, vPilot schliesst).</summary>
        public void CancelPending()
        {
            CancellationTokenSource previous = Interlocked.Exchange(ref _running, null);
            if (previous != null)
            {
                previous.Cancel();
                previous.Dispose();
            }
        }

        private string BuildBarkUrl(string title, string body, string level, int volume, int call, string sound)
        {
            string baseUrl = _config.BarkServerUrl.Trim();
            if (baseUrl.Length == 0)
            {
                baseUrl = "https://api.day.app/";
            }
            if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
            {
                baseUrl += "/";
            }

            if (string.IsNullOrEmpty(body))
            {
                body = " ";
            }

            return baseUrl
                + Uri.EscapeDataString(_config.BarkDeviceKey.Trim()) + "/"
                + Uri.EscapeDataString(title) + "/"
                + Uri.EscapeDataString(body)
                + "?level=" + Uri.EscapeDataString(level)
                + "&volume=" + volume
                + "&call=" + call
                + "&sound=" + Uri.EscapeDataString(sound)
                + "&group=" + Uri.EscapeDataString(_config.Group);
        }

        private static async Task SendBarkAsync(string url)
        {
            try
            {
                HttpResponseMessage response = await Http.GetAsync(url).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    Log.Debug("Bark OK (HTTP " + (int)response.StatusCode + ").");
                }
                else
                {
                    Log.Error("BARK: HTTP " + (int)response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Log.Error("BARK: " + ex.Message);
            }
        }

        private async Task SendAlarmMailAsync(string reason, string from, string message)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (MailMessage mail = new MailMessage())
                    using (SmtpClient smtp = new SmtpClient(_config.SmtpHost.Trim(), _config.SmtpPort))
                    {
                        string address = _config.EmailAddress.Trim();
                        mail.From = new MailAddress(address, "Crew Rest Companion");
                        mail.To.Add(address);
                        mail.Subject = "ALARM-VPILOT";
                        mail.IsBodyHtml = false;
                        mail.Body = "Achtung! " + reason + Environment.NewLine
                            + "Von: " + from + Environment.NewLine
                            + "Text: " + message + Environment.NewLine
                            + "Zeit: " + DateTime.Now.ToString("HH:mm:ss");

                        smtp.EnableSsl = _config.SmtpUseSsl;
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(address, _config.EmailAppPassword);
                        smtp.Timeout = 20000;

                        smtp.Send(mail);
                    }

                    Log.Info("Alarm-Mail versendet.");
                }
                catch (SmtpException ex)
                {
                    Log.Error("MAIL (" + ex.StatusCode + "): " + ex.Message
                        + " - häufigste Ursache: Login-Passwort statt App-Passwort.");
                }
                catch (Exception ex)
                {
                    Log.Error("MAIL: " + ex.Message);
                }
            }).ConfigureAwait(false);
        }
    }
}
