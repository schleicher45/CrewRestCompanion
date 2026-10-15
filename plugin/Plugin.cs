using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RossCarlson.Vatsim.Vpilot.Plugins;

namespace CrewRestCompanion
{
    public class Plugin : IPlugin
    {
        public const string Version = "1.2.0";

        public string Name
        {
            get { return "Crew Rest Companion"; }
        }

        private IBroker _broker;
        private PluginConfig _config;
        private Notifier _notifier;

        private string _ownCallsign = string.Empty;
        private DateTime _connectedUtc = DateTime.MinValue;

        // --- Filter ---
        private static readonly Regex IsSupervisor =
            new Regex(@"\b(SUP|Supervisor)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex IsATC =
            new Regex(@"(_DEL|_GND|_TWR|_APP|_DEP|_CTR|_FSS|_RDR|_INFO|_RMP|_TMU|_ATIS)\b",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Deckt ab: "contact me", "please contact me on 121.500", "contact EDDL_TWR on 118,3", "contact me 121.5"
        private static readonly Regex ContactPhrase =
            new Regex(@"(contact\s+me|contact\s+\S+\s+on\s+1?\d{2}[.,]\d{1,3}|monitor\s+1?\d{2}[.,]\d{1,3})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static string PluginDir
        {
            get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
        }

        public void Initialize(IBroker broker)
        {
            _broker = broker;

            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            }
            catch (Exception)
            {
                // Aeltere Systeme: Standard beibehalten.
            }

            _config = PluginConfig.Load(PluginDir);
            Log.Configure(PluginDir, _config.LogLevel);
            _notifier = new Notifier(_config);

            Log.Info("Crew Rest Companion v" + Version + " gestartet. Bark="
                + (_config.HasBark ? "an" : "aus") + ", Mail="
                + (_config.HasMail ? "an" : "aus") + ", Sperrzeit=" + _config.CooldownS + "s");

            Subscribe();

            // Verzoegert, damit vPilot erst seine Oberflaeche und das Netzwerk hochfaehrt.
            Task.Run(async () =>
            {
                await Task.Delay(2000).ConfigureAwait(false);
                AnnounceStartup();
            });
        }

        private void Subscribe()
        {
            Wire("PrivateMessageReceived", delegate
            {
                _broker.PrivateMessageReceived += (s, e) => OnPrivateMessage(e);
            });

            Wire("NetworkConnected", delegate
            {
                _broker.NetworkConnected += (s, e) => OnNetworkConnected(e);
            });

            Wire("NetworkDisconnected", delegate
            {
                _broker.NetworkDisconnected += (s, e) => OnSessionOver("Netzwerkverbindung getrennt");
            });

            Wire("SessionEnded", delegate
            {
                _broker.SessionEnded += (s, e) => OnSessionOver("vPilot wird geschlossen");
            });

            if (_config.AlarmOnRadioMessage)
            {
                Wire("RadioMessageReceived", delegate
                {
                    _broker.RadioMessageReceived += (s, e) => OnRadioMessage(e);
                });
            }

            if (_config.AlarmOnSelcal)
            {
                Wire("SelcalAlertReceived", delegate
                {
                    _broker.SelcalAlertReceived += (s, e) => OnSelcal(e);
                });
            }

            if (_config.NotifyControllerOnline)
            {
                Wire("ControllerAdded", delegate
                {
                    _broker.ControllerAdded += (s, e) => OnControllerAdded(e);
                });
            }
        }

        /// <summary>Ein fehlendes Event in einer aelteren vPilot-Version darf das Plugin nicht killen.</summary>
        private static void Wire(string name, Action subscribe)
        {
            try
            {
                subscribe();
            }
            catch (Exception ex)
            {
                Log.Error("Event " + name + " nicht verfügbar: " + ex.Message);
            }
        }

        private void AnnounceStartup()
        {
            if (_config.StartupMessage)
            {
                PrintToVPilot("Crew Rest Companion v" + Version + " aktiv"
                    + (_config.HasBark ? ", Bark" : string.Empty)
                    + (_config.HasMail ? ", Mail-Alarm" : string.Empty)
                    + (!_config.HasBark && !_config.HasMail ? " - ACHTUNG: kein Benachrichtigungsweg konfiguriert!" : "."));
            }

            if (!_config.HasBark && !_config.HasMail)
            {
                Log.Error("Weder Bark noch Mail konfiguriert - es wird nichts gemeldet. Bitte config.txt prüfen.");
                return;
            }

            if (_config.StartupNotification && _config.HasBark)
            {
                _notifier.Info("Crew Rest Companion", "Bereit - v" + Version, "minuet", 2);
            }
        }

        // ------------------------------------------------------------ Events

        private void OnNetworkConnected(object e)
        {
            _ownCallsign = Read(e, "Callsign");

            _connectedUtc = DateTime.UtcNow;
            Log.Info("Verbunden als " + (_ownCallsign.Length > 0 ? _ownCallsign : "(Callsign unbekannt)") + ".");
        }

        private void OnSessionOver(string reason)
        {
            Log.Info(reason + " - laufende Alarme werden beendet.");
            _notifier.CancelPending();
            _connectedUtc = DateTime.MinValue;
        }

        private void OnPrivateMessage(object e)
        {
            string from = Read(e, "From", "Sender", "SenderCallsign");
            string message = Read(e, "Message", "Text", "Body");

            Log.Debug("PM von " + from + ": " + message);

            if (IsSupervisor.IsMatch(from))
            {
                _notifier.Alarm("Supervisor-Nachricht", from, message);
                return;
            }

            if (IsATC.IsMatch(from))
            {
                _notifier.Alarm("ATC-Nachricht", from, message);
                return;
            }

            if (ContactPhrase.IsMatch(message))
            {
                _notifier.Alarm("Contact Me", from, message);
                return;
            }

            if (_config.NotifyPilotDMs)
            {
                _notifier.Info("DM von " + from,
                    string.IsNullOrEmpty(message) ? "Neue Nachricht" : message,
                    _config.InfoSound, 5);
            }
        }

        private void OnRadioMessage(object e)
        {
            string from = Read(e, "From", "Sender", "SenderCallsign");
            string message = Read(e, "Message", "Text", "Body");

            if (message.Length == 0)
            {
                return;
            }

            bool fromController = IsATC.IsMatch(from) || IsSupervisor.IsMatch(from);
            if (!fromController)
            {
                return;
            }

            bool addressedToMe = _ownCallsign.Length > 0
                && message.IndexOf(_ownCallsign, StringComparison.OrdinalIgnoreCase) >= 0;

            if (!addressedToMe && !ContactPhrase.IsMatch(message))
            {
                Log.Debug("Funkspruch ignoriert (nicht an uns): " + from + " - " + message);
                return;
            }

            _notifier.Alarm("Funkspruch an " + (addressedToMe ? _ownCallsign : "uns"), from, message);
        }

        private void OnSelcal(object e)
        {
            string from = Read(e, "From", "Sender", "SenderCallsign");
            _notifier.Alarm("SELCAL-Ruf", from.Length > 0 ? from : "ATC", "SELCAL empfangen - bitte melden.");
        }

        private void OnControllerAdded(object e)
        {
            // Beim Verbinden meldet vPilot alle bereits online stehenden Lotsen auf einmal.
            if (_connectedUtc == DateTime.MinValue
                || DateTime.UtcNow - _connectedUtc < TimeSpan.FromSeconds(_config.ControllerQuietSeconds))
            {
                return;
            }

            string callsign = Read(e, "Callsign");
            if (callsign.Length == 0 || !IsATC.IsMatch(callsign))
            {
                return;
            }

            string[] prefixes = _config.ControllerPrefixes();
            if (prefixes.Length > 0)
            {
                bool match = false;
                foreach (string prefix in prefixes)
                {
                    if (callsign.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        match = true;
                        break;
                    }
                }
                if (!match)
                {
                    return;
                }
            }

            string frequency = string.Empty;
            try
            {
                object rawFrequency = GetMember(e, "Frequency");
                if (rawFrequency != null)
                {
                    int khz = Convert.ToInt32(rawFrequency);
                    if (khz > 0)
                    {
                        frequency = " auf " + (khz / 1000.0).ToString("0.000", CultureInfo.InvariantCulture) + " MHz";
                    }
                }
            }
            catch (Exception)
            {
                // Frequenz ist nur Zusatzinfo.
            }

            Log.Info("Lotse online: " + callsign + frequency);
            _notifier.Info("Lotse online", callsign + frequency + " ist jetzt besetzt.", _config.InfoSound, 4);
        }

        // ------------------------------------------------------------ Helfer

        private static string Read(object e, params string[] names)
        {
            foreach (string name in names)
            {
                try
                {
                    object value = GetMember(e, name);
                    if (value != null)
                    {
                        string text = Convert.ToString(value);
                        if (!string.IsNullOrEmpty(text))
                        {
                            return text;
                        }
                    }
                }
                catch (Exception)
                {
                    // Naechste Eigenschaft probieren.
                }
            }
            return string.Empty;
        }

        private static object GetMember(object source, string name)
        {
            if (source == null)
            {
                return null;
            }

            PropertyInfo property = source.GetType().GetProperty(name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return property == null ? null : property.GetValue(source, null);
        }

        private void PrintToVPilot(string message)
        {
            if (_broker == null)
            {
                return;
            }

            try
            {
                _broker.PostDebugMessage(message);
            }
            catch (Exception ex)
            {
                Log.Error("Chatausgabe: " + ex.Message);
            }
        }
    }
}
