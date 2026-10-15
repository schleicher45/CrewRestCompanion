using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace CrewRestConfigurator
{
    internal sealed class MainForm : Form
    {
        // --- Installation ---
        private readonly TextBox txtPluginDir = new TextBox();
        private readonly TextBox txtDllSource = new TextBox();
        private readonly Label lblInstallStatus = new Label();
        private readonly ComboBox cmbLogLevel = new FlatCombo();
        private readonly CheckBox chkStartupMessage = new CheckBox();

        // --- Alarm ---
        private readonly TextBox txtBarkKey = new TextBox();
        private readonly TextBox txtBarkUrl = new TextBox();
        private readonly TextBox txtAlarmSound = new TextBox();
        private readonly ComboBox cmbAlarmLevel = new FlatCombo();
        private readonly NumericUpDown numVolume = new DarkNumeric();
        private readonly CheckBox chkCall = new CheckBox();
        private readonly TextBox txtEmail = new TextBox();
        private readonly TextBox txtPassword = new TextBox();
        private readonly TextBox txtSmtpHost = new TextBox();
        private readonly TextBox txtSmtpPort = new TextBox();
        private readonly CheckBox chkSsl = new CheckBox();
        private readonly CheckBox chkShowPassword = new CheckBox();
        private readonly NumericUpDown numCooldown = new DarkNumeric();
        private readonly CheckBox chkStartupNotification = new CheckBox();

        // --- Ereignisse ---
        private readonly CheckBox chkSelcal = new CheckBox();
        private readonly CheckBox chkRadio = new CheckBox();
        private readonly CheckBox chkPilotDms = new CheckBox();
        private readonly CheckBox chkController = new CheckBox();
        private readonly TextBox txtControllerFilter = new TextBox();
        private readonly NumericUpDown numControllerQuiet = new DarkNumeric();

        // --- Aktionen ---
        private readonly Button btnTestBark = new Button();
        private readonly Button btnTestBarkAlarm = new Button();
        private readonly Button btnTestMail = new Button();
        private readonly TextBox txtLog = new TextBox();

        // --- Navigation ---
        private readonly List<Button> _navButtons = new List<Button>();
        private readonly Button _themeToggle = new Button();
        private readonly Label _brand = new Label();
        private readonly Label _brandSub = new Label();
        private readonly Label _footer = new Label();
        private readonly Button _updateNotice = new Button();
        private string _updateUrl = string.Empty;
        private Panel _sidebar;
        private Panel _content;
        private readonly List<Panel> _pages = new List<Panel>();
        private readonly Panel _navIndicator = new Panel();

        // Werte ohne eigenes Bedienelement - werden unveraendert durchgereicht.
        private string _infoSound = "anticipate";
        private string _group = "VATSIM";
        private int _repeatCount = 1;
        private int _repeatDelayS = 20;
        private List<KeyValuePair<string, string>> _carriedUnknown = new List<KeyValuePair<string, string>>();

        private const int PageWidth = 658;
        private const int PageHeight = 512;

        public MainForm()
        {
            Ui.Dark = UiSettings.LoadDark();

            Text = "Crew Rest Companion";
            Font = Ui.Body;
            ClientSize = new Size(880, 736);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Ui.Background;

            _sidebar = BuildSidebar();
            Controls.Add(_sidebar);
            _content = BuildContent();
            Controls.Add(_content);

            Load += OnFormLoad;
        }

        // ------------------------------------------------------------ Rahmen

        private Panel BuildSidebar()
        {
            Panel bar = new Panel();
            bar.Location = new Point(0, 0);
            bar.Size = new Size(190, ClientSize.Height);
            bar.BackColor = Ui.Sidebar;

            _brand.Text = "Crew Rest" + Environment.NewLine + "Companion";
            _brand.Font = Ui.Brand;
            _brand.ForeColor = Ui.SidebarStrong;
            _brand.Location = new Point(22, 28);
            _brand.Size = new Size(150, 52);
            bar.Controls.Add(_brand);

            _brandSub.Text = "Konfiguration";
            _brandSub.Font = Ui.HintFont;
            _brandSub.ForeColor = Ui.SidebarFaint;
            _brandSub.Location = new Point(24, 84);
            _brandSub.Size = new Size(150, 18);
            bar.Controls.Add(_brandSub);

            _navIndicator.Size = new Size(3, 44);
            _navIndicator.BackColor = Ui.Accent;
            bar.Controls.Add(_navIndicator);

            string[] names = { "Installation", "Alarm", "Ereignisse" };
            for (int i = 0; i < names.Length; i++)
            {
                Button item = new Button();
                item.Text = "   " + names[i];
                item.Tag = i;
                item.Location = new Point(0, 128 + i * 46);
                item.Size = new Size(190, 44);
                item.FlatStyle = FlatStyle.Flat;
                item.FlatAppearance.BorderSize = 0;
                item.FlatAppearance.MouseOverBackColor = Ui.SidebarHover;
                item.BackColor = Ui.Sidebar;
                item.ForeColor = Ui.SidebarText;
                item.Font = Ui.Nav;
                item.TextAlign = ContentAlignment.MiddleLeft;
                item.Cursor = Cursors.Hand;
                item.Click += OnNavClick;
                bar.Controls.Add(item);
                _navButtons.Add(item);
            }

            _updateNotice.Text = string.Empty;
            _updateNotice.Location = new Point(0, ClientSize.Height - 142);
            _updateNotice.Size = new Size(190, 38);
            _updateNotice.FlatStyle = FlatStyle.Flat;
            _updateNotice.FlatAppearance.BorderSize = 0;
            _updateNotice.FlatAppearance.MouseOverBackColor = Ui.SidebarHover;
            _updateNotice.BackColor = Ui.Sidebar;
            _updateNotice.ForeColor = Ui.Accent;
            _updateNotice.Font = Ui.HintFont;
            _updateNotice.TextAlign = ContentAlignment.MiddleLeft;
            _updateNotice.Cursor = Cursors.Hand;
            _updateNotice.Visible = false;
            _updateNotice.Click += OnUpdateNoticeClick;
            bar.Controls.Add(_updateNotice);

            _themeToggle.Text = "   Heller Modus";
            _themeToggle.Location = new Point(0, ClientSize.Height - 96);
            _themeToggle.Size = new Size(190, 38);
            _themeToggle.FlatStyle = FlatStyle.Flat;
            _themeToggle.FlatAppearance.BorderSize = 0;
            _themeToggle.FlatAppearance.MouseOverBackColor = Ui.SidebarHover;
            _themeToggle.BackColor = Ui.Sidebar;
            _themeToggle.ForeColor = Ui.SidebarText;
            _themeToggle.Font = Ui.HintFont;
            _themeToggle.TextAlign = ContentAlignment.MiddleLeft;
            _themeToggle.Cursor = Cursors.Hand;
            _themeToggle.Click += OnToggleTheme;
            bar.Controls.Add(_themeToggle);

            _footer.Text = "vPilot-Plugin für VATSIM";
            _footer.Font = Ui.HintFont;
            _footer.ForeColor = Ui.SidebarFaint;
            _footer.Location = new Point(24, ClientSize.Height - 40);
            _footer.Size = new Size(160, 18);
            bar.Controls.Add(_footer);

            return bar;
        }

        private Panel BuildContent()
        {
            Panel content = new Panel();
            content.Location = new Point(190, 0);
            content.Size = new Size(690, ClientSize.Height);
            content.BackColor = Ui.Background;

            _pages.Add(BuildInstallPage());
            _pages.Add(BuildAlarmPage());
            _pages.Add(BuildEventPage());

            foreach (Panel page in _pages)
            {
                page.Location = new Point(16, 16);
                page.Size = new Size(PageWidth, PageHeight);
                page.BackColor = Ui.Background;
                page.Visible = false;
                content.Controls.Add(page);
            }

            content.Controls.Add(Ui.Primary(new Button(), "Speichern und installieren",
                16, 536, 220, 38, OnApply));
            content.Controls.Add(Ui.Quiet(new Button(), "Änderungen verwerfen",
                246, 536, 170, 38, OnRevert));
            content.Controls.Add(Ui.Quiet(new Button(), "Standardwerte",
                426, 536, 150, 38, OnDefaults));

            content.Controls.Add(Ui.Hint(
                "Übernimmt deine Einstellungen und bringt das Plugin auf Stand. Danach vPilot neu starten.",
                16, 580, PageWidth, 18));

            txtLog.Location = new Point(16, 602);
            txtLog.Size = new Size(PageWidth, 114);
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.BorderStyle = BorderStyle.FixedSingle;
            txtLog.BackColor = Color.FromArgb(250, 250, 251);
            txtLog.ForeColor = Ui.Text;
            txtLog.Font = Ui.Mono;
            content.Controls.Add(txtLog);

            return content;
        }

        private void OnNavClick(object sender, EventArgs e)
        {
            Button item = sender as Button;
            if (item == null)
            {
                return;
            }
            ShowPage(Convert.ToInt32(item.Tag));
        }

        private void ShowPage(int index)
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                _pages[i].Visible = i == index;
                _navButtons[i].BackColor = i == index ? Ui.SidebarHover : Ui.Sidebar;
                _navButtons[i].ForeColor = i == index ? Ui.SidebarStrong : Ui.SidebarText;
                _navButtons[i].FlatAppearance.MouseOverBackColor = Ui.SidebarHover;
            }

            _navIndicator.Location = new Point(0, _navButtons[index].Top);
            _navIndicator.BringToFront();
        }

        // ------------------------------------------------------------ Seiten

        private Panel BuildInstallPage()
        {
            Panel page = new Panel();

            Card setup = new Card("Installation", 0, 0, PageWidth, 232);

            setup.Controls.Add(Ui.Label("vPilot Plugins-Ordner:", 16, 50, 140));
            setup.Controls.Add(Ui.TextField(txtPluginDir, 162, 48, 364));
            setup.Controls.Add(Ui.Quiet(new Button(), "Durchsuchen", 534, 47, 108, 26, OnBrowseDir));

            setup.Controls.Add(Ui.Label("Plugin-Datei:", 16, 84, 140));
            setup.Controls.Add(Ui.TextField(txtDllSource, 162, 82, 364));
            setup.Controls.Add(Ui.Quiet(new Button(), "Durchsuchen", 534, 81, 108, 26, OnBrowseDll));

            lblInstallStatus.Location = new Point(16, 116);
            lblInstallStatus.Size = new Size(626, 40);
            lblInstallStatus.ForeColor = Ui.Accent;
            lblInstallStatus.Tag = "accent";
            setup.Controls.Add(lblInstallStatus);

            setup.Controls.Add(Ui.Quiet(new Button(), "Plugins-Ordner öffnen", 16, 160, 180, 28, OnOpenDir));

            setup.Controls.Add(Ui.Hint(
                "Fehlt der Ordner, wird er beim Speichern angelegt. Ältere Versionen werden "
                + "automatisch zur Seite gelegt.",
                16, 194, 626, 30));

            page.Controls.Add(setup);

            Card diag = new Card("Fehlersuche", 0, 244, PageWidth, 166);

            diag.Controls.Add(Ui.Label("Protokoll:", 16, 50, 80));
            diag.Controls.Add(Ui.Combo(cmbLogLevel, 100, 48, 230));
            cmbLogLevel.Items.Add("aus - nichts mitschreiben");
            cmbLogLevel.Items.Add("normal - Standard");
            cmbLogLevel.Items.Add("ausführlich - für Fehlersuche");
            cmbLogLevel.SelectedIndex = 1;

            diag.Controls.Add(Ui.Check(chkStartupMessage,
                "Startmeldung ins vPilot-Debugfenster schreiben", 16, 84, 420));

            diag.Controls.Add(Ui.Hint(
                "Die Startmeldung erscheint im Debugfenster von vPilot, nicht im Chat - vPilot lässt "
                + "Plugins nicht in den Chat schreiben.\r\n"
                + "Bei Problemen das Protokoll auf \"ausführlich\" stellen, einmal fliegen und die "
                + "Protokolldatei aus dem Plugins-Ordner mitschicken.",
                16, 112, 626, 46));

            page.Controls.Add(diag);

            return page;
        }

        private Panel BuildAlarmPage()
        {
            Panel page = new Panel();

            Card bark = new Card("Push aufs Handy (Bark)", 0, 0, PageWidth, 214);

            bark.Controls.Add(Ui.Label("Device Key:", 16, 50, 110));
            bark.Controls.Add(Ui.TextField(txtBarkKey, 132, 48, 386));
            bark.Controls.Add(Ui.Quiet(btnTestBark, "Test DM", 530, 47, 112, 26, OnTestBark));

            bark.Controls.Add(Ui.Label("Server URL:", 16, 84, 110));
            bark.Controls.Add(Ui.TextField(txtBarkUrl, 132, 82, 386));
            bark.Controls.Add(Ui.Quiet(btnTestBarkAlarm, "Test ALARM", 530, 81, 112, 26, OnTestBarkAlarm));

            bark.Controls.Add(Ui.Label("Klingelton:", 16, 118, 110));
            bark.Controls.Add(Ui.TextField(txtAlarmSound, 132, 116, 140));

            bark.Controls.Add(Ui.Label("Stufe:", 288, 118, 46));
            bark.Controls.Add(Ui.Combo(cmbAlarmLevel, 338, 116, 120));
            cmbAlarmLevel.Items.Add("critical");
            cmbAlarmLevel.Items.Add("timeSensitive");
            cmbAlarmLevel.Items.Add("active");
            cmbAlarmLevel.Items.Add("passive");
            cmbAlarmLevel.SelectedIndex = 0;

            bark.Controls.Add(Ui.Label("Lautstärke:", 472, 118, 72));
            bark.Controls.Add(Ui.Number(numVolume, 550, 116, 56, 0, 10, 10));

            bark.Controls.Add(Ui.Check(chkCall,
                "Dauerton - klingelt durch, bis du es am Handy wegwischst", 132, 148, 420));

            bark.Controls.Add(Ui.Hint(
                "\"critical\" klingelt auch bei Lautlos und \"Nicht stören\".\r\n"
                + "Klingeltöne aus der Bark-App, z. B. multiwayinvitation, alarm, bell, minuet.",
                16, 174, 626, 34));

            page.Controls.Add(bark);

            Card mail = new Card("Wecker über E-Mail (iPhone-Kurzbefehl)", 0, 226, PageWidth, 196);

            mail.Controls.Add(Ui.Label("E-Mail-Adresse:", 16, 50, 110));
            mail.Controls.Add(Ui.TextField(txtEmail, 132, 48, 386));
            mail.Controls.Add(Ui.Quiet(btnTestMail, "Testmail", 530, 47, 112, 26, OnTestMail));

            mail.Controls.Add(Ui.Label("App-Passwort:", 16, 84, 110));
            txtPassword.UseSystemPasswordChar = true;
            mail.Controls.Add(Ui.TextField(txtPassword, 132, 82, 386));
            chkShowPassword.CheckedChanged += OnToggleShowPassword;
            mail.Controls.Add(Ui.Check(chkShowPassword, "zeigen", 534, 83, 108));

            mail.Controls.Add(Ui.Label("SMTP-Host:", 16, 118, 110));
            mail.Controls.Add(Ui.TextField(txtSmtpHost, 132, 116, 190));
            mail.Controls.Add(Ui.Label("Port:", 336, 118, 38));
            mail.Controls.Add(Ui.TextField(txtSmtpPort, 378, 116, 70));
            mail.Controls.Add(Ui.Check(chkSsl, "SSL / TLS", 464, 117, 100));

            mail.Controls.Add(Ui.Hint(
                "App-Passwort verwenden, nicht das normale Kennwort des Kontos.\r\n"
                + "Der Anbieter muss Push unterstützen - Outlook ja, Gmail und GMX meist nicht.\r\n"
                + "Das Passwort wird verschlüsselt gespeichert und ist nur auf diesem PC lesbar.",
                16, 144, 626, 46));

            page.Controls.Add(mail);

            Card misc = new Card(null, 0, 434, PageWidth, 76);

            misc.Controls.Add(Ui.Label("Sperrzeit (s):", 16, 16, 90));
            misc.Controls.Add(Ui.Number(numCooldown, 112, 14, 64, 0, 3600, 60));
            misc.Controls.Add(Ui.Check(chkStartupNotification,
                "Start-Push aufs Handy senden (leise)", 202, 15, 300));

            misc.Controls.Add(Ui.Hint(
                "Sperrzeit: so lange bleibt es nach einem Alarm still, damit eine Nachrichtenserie "
                + "nicht mehrfach weckt.",
                16, 44, 626, 20));

            page.Controls.Add(misc);

            return page;
        }

        private Panel BuildEventPage()
        {
            Panel page = new Panel();

            Card always = new Card("Immer aktiv", 0, 0, PageWidth, 76);
            always.Controls.Add(Ui.Hint(
                "Supervisor-Nachrichten, Nachrichten von Lotsen und \"contact me\" lösen immer Alarm aus.",
                16, 42, 626, 24));
            page.Controls.Add(always);

            Card extra = new Card("Zusätzlich", 0, 88, PageWidth, 368);

            extra.Controls.Add(Ui.Check(chkSelcal, "Alarm bei SELCAL-Ruf", 16, 50, 600));
            extra.Controls.Add(Ui.Hint(
                "So holt dich ATC im echten Betrieb aus der Ruhephase. Dringend empfohlen.",
                38, 72, 600, 18));

            extra.Controls.Add(Ui.Check(chkRadio, "Alarm bei Funkspruch an das eigene Callsign", 16, 98, 600));
            extra.Controls.Add(Ui.Hint(
                "Weckt nur, wenn dein Callsign oder \"contact me\" im Funkspruch steht - "
                + "normaler Funkverkehr nicht.",
                38, 120, 600, 32));

            extra.Controls.Add(Ui.Check(chkPilotDms, "Leise Info bei Piloten-DMs", 16, 158, 600));
            extra.Controls.Add(Ui.Hint(
                "Push ohne Weckfunktion - du siehst die Nachricht, wachst aber nicht auf.",
                38, 180, 600, 18));

            extra.Controls.Add(Ui.Check(chkController, "Info, wenn ein Lotse online geht", 16, 206, 600));
            extra.Controls.Add(Ui.Label("Filter (Präfixe):", 38, 234, 110));
            extra.Controls.Add(Ui.TextField(txtControllerFilter, 152, 232, 230));
            extra.Controls.Add(Ui.Label("Ruhezeit (s):", 400, 234, 90));
            extra.Controls.Add(Ui.Number(numControllerQuiet, 494, 232, 70, 0, 600, 90));
            extra.Controls.Add(Ui.Hint(
                "Ohne Filter wird jeder Lotse weltweit gemeldet. Mehrere Kennungen mit Komma trennen.\r\n"
                + "Die Ruhezeit unterdrückt die Meldungen direkt nach dem Verbinden.",
                38, 262, 600, 46));

            page.Controls.Add(extra);

            return page;
        }

        // ----------------------------------------------------------- Bindung

        private void OnFormLoad(object sender, EventArgs e)
        {
            SetTheme(Ui.Dark);
            ShowPage(0);
            txtPluginDir.Text = PluginInstaller.DefaultPluginDir();
            txtDllSource.Text = PluginInstaller.FindSourceDll();
            LoadConfigIntoForm();
            Log("Configurator gestartet.");
            CheckForUpdate();
            if (txtDllSource.Text.Length == 0)
            {
                Log("Keine Plugin-Datei gefunden - bitte unter Installation auswählen.");
            }
        }

        private void LoadConfigIntoForm()
        {
            string path = PluginInstaller.ConfigPath(txtPluginDir.Text.Trim());
            ApplyToForm(ConfigModel.Load(path));
            RefreshInstallStatus();
            Log(File.Exists(path) ? "Einstellungen geladen." : "Noch keine Einstellungen vorhanden - Standardwerte geladen.");
        }

        private void ApplyToForm(ConfigModel cfg)
        {
            txtBarkKey.Text = cfg.BarkDeviceKey;
            txtBarkUrl.Text = cfg.BarkServerUrl;
            txtAlarmSound.Text = cfg.AlarmSound;
            SelectByText(cmbAlarmLevel, cfg.AlarmLevel, 0);
            numVolume.Value = Clamp(cfg.AlarmVolume, numVolume);
            chkCall.Checked = cfg.AlarmCall;
            _infoSound = cfg.InfoSound;
            _group = cfg.Group;
            _repeatCount = cfg.RepeatCount;
            _repeatDelayS = cfg.RepeatDelayS;

            txtEmail.Text = cfg.EmailAddress;
            txtPassword.Text = cfg.EmailAppPassword;
            txtSmtpHost.Text = cfg.SmtpHost;
            txtSmtpPort.Text = cfg.SmtpPort.ToString(CultureInfo.InvariantCulture);
            chkSsl.Checked = cfg.SmtpUseSsl;

            chkSelcal.Checked = cfg.AlarmOnSelcal;
            chkRadio.Checked = cfg.AlarmOnRadioMessage;
            chkPilotDms.Checked = cfg.NotifyPilotDMs;
            chkController.Checked = cfg.NotifyControllerOnline;
            txtControllerFilter.Text = cfg.ControllerFilter;
            numControllerQuiet.Value = Clamp(cfg.ControllerQuietSeconds, numControllerQuiet);

            numCooldown.Value = Clamp(cfg.CooldownS, numCooldown);
            chkStartupMessage.Checked = cfg.StartupMessage;
            chkStartupNotification.Checked = cfg.StartupNotification;

            if (cfg.LogLevel.StartsWith("off", StringComparison.OrdinalIgnoreCase)) { cmbLogLevel.SelectedIndex = 0; }
            else if (cfg.LogLevel.StartsWith("debug", StringComparison.OrdinalIgnoreCase)) { cmbLogLevel.SelectedIndex = 2; }
            else { cmbLogLevel.SelectedIndex = 1; }

            _carriedUnknown = cfg.Unknown;
        }

        private static void SelectByText(ComboBox box, string value, int fallbackIndex)
        {
            for (int i = 0; i < box.Items.Count; i++)
            {
                if (string.Equals(Convert.ToString(box.Items[i]), value, StringComparison.OrdinalIgnoreCase))
                {
                    box.SelectedIndex = i;
                    return;
                }
            }
            box.SelectedIndex = fallbackIndex;
        }

        private static decimal Clamp(int value, NumericUpDown control)
        {
            decimal d = value;
            if (d < control.Minimum) { return control.Minimum; }
            if (d > control.Maximum) { return control.Maximum; }
            return d;
        }

        private ConfigModel BuildConfigFromForm()
        {
            ConfigModel cfg = new ConfigModel();

            cfg.BarkDeviceKey = txtBarkKey.Text.Trim();
            cfg.BarkServerUrl = txtBarkUrl.Text.Trim().Length == 0 ? "https://api.day.app/" : txtBarkUrl.Text.Trim();
            cfg.AlarmSound = txtAlarmSound.Text.Trim().Length == 0 ? "multiwayinvitation" : txtAlarmSound.Text.Trim();
            cfg.AlarmLevel = Convert.ToString(cmbAlarmLevel.SelectedItem);
            cfg.AlarmVolume = (int)numVolume.Value;
            cfg.AlarmCall = chkCall.Checked;
            cfg.InfoSound = _infoSound;
            cfg.Group = _group;
            cfg.RepeatCount = _repeatCount;
            cfg.RepeatDelayS = _repeatDelayS;

            cfg.EmailAddress = txtEmail.Text.Trim();
            cfg.EmailAppPassword = txtPassword.Text;
            cfg.SmtpHost = txtSmtpHost.Text.Trim();
            int port;
            if (!int.TryParse(txtSmtpPort.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out port)
                || port < 1 || port > 65535)
            {
                port = 587;
            }
            cfg.SmtpPort = port;
            cfg.SmtpUseSsl = chkSsl.Checked;

            cfg.AlarmOnSelcal = chkSelcal.Checked;
            cfg.AlarmOnRadioMessage = chkRadio.Checked;
            cfg.NotifyPilotDMs = chkPilotDms.Checked;
            cfg.NotifyControllerOnline = chkController.Checked;
            cfg.ControllerFilter = txtControllerFilter.Text.Trim();
            cfg.ControllerQuietSeconds = (int)numControllerQuiet.Value;

            cfg.CooldownS = (int)numCooldown.Value;
            cfg.StartupMessage = chkStartupMessage.Checked;
            cfg.StartupNotification = chkStartupNotification.Checked;

            if (cmbLogLevel.SelectedIndex == 0) { cfg.LogLevel = "off"; }
            else if (cmbLogLevel.SelectedIndex == 2) { cfg.LogLevel = "debug"; }
            else { cfg.LogLevel = "info"; }

            cfg.Unknown.AddRange(_carriedUnknown);
            return cfg;
        }

        // ---------------------------------------------------------- Aktionen

        private void OnBrowseDir(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "vPilot Plugins-Ordner auswählen";
                if (Directory.Exists(txtPluginDir.Text.Trim()))
                {
                    dialog.SelectedPath = txtPluginDir.Text.Trim();
                }
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    txtPluginDir.Text = dialog.SelectedPath;
                    LoadConfigIntoForm();
                }
            }
        }

        private void OnBrowseDll(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Plugin-Datei auswählen";
                dialog.Filter = "Plugin (*.dll)|*.dll|Alle Dateien (*.*)|*.*";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    txtDllSource.Text = dialog.FileName;
                    Log("Quelle gesetzt: " + dialog.FileName);
                }
            }
        }

        private void OnOpenDir(object sender, EventArgs e)
        {
            try
            {
                PluginInstaller.OpenInExplorer(txtPluginDir.Text.Trim());
            }
            catch (Exception ex)
            {
                Log("Ordner konnte nicht geöffnet werden: " + ex.Message);
            }
        }

        private void OnToggleTheme(object sender, EventArgs e)
        {
            SetTheme(!Ui.Dark);
            UiSettings.SaveDark(Ui.Dark);
            Log(Ui.Dark ? "Dunkelmodus eingeschaltet." : "Heller Modus eingeschaltet.");
        }

        private async void CheckForUpdate()
        {
            UpdateInfo info = await UpdateCheck.CheckAsync();
            if (info == null)
            {
                return;
            }

            if (info.Problem.Length > 0)
            {
                Log("Update-Prüfung nicht möglich: " + info.Problem);
                return;
            }

            if (!info.IsNewer)
            {
                Log("Update-Prüfung: Version " + UpdateCheck.CurrentVersion() + " ist aktuell.");
                return;
            }

            _updateUrl = info.Url;
            _updateNotice.Text = "   Version " + info.Version + " verfügbar";
            _updateNotice.Visible = true;

            Log("Neue Version " + info.Version + " verfügbar"
                + (info.Notes.Length > 0 ? ": " + info.Notes : ".")
                + " (links in der Leiste anklicken)");
        }

        private void OnUpdateNoticeClick(object sender, EventArgs e)
        {
            if (_updateUrl.Length == 0)
            {
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(_updateUrl);
            }
            catch (Exception ex)
            {
                Log("Seite konnte nicht geöffnet werden: " + ex.Message);
            }
        }

        private void RepaintSidebar()
        {
            if (_sidebar == null)
            {
                return;
            }

            _sidebar.BackColor = Ui.Sidebar;
            _brand.ForeColor = Ui.SidebarStrong;
            _brandSub.ForeColor = Ui.SidebarFaint;
            _footer.ForeColor = Ui.SidebarFaint;
            _navIndicator.BackColor = Ui.Accent;

            foreach (Button item in _navButtons)
            {
                item.BackColor = Ui.Sidebar;
                item.ForeColor = Ui.SidebarText;
                item.FlatAppearance.MouseOverBackColor = Ui.SidebarHover;
            }

            _updateNotice.BackColor = Ui.Sidebar;
            _updateNotice.ForeColor = Ui.Accent;
            _updateNotice.FlatAppearance.MouseOverBackColor = Ui.SidebarHover;

            _themeToggle.BackColor = Ui.Sidebar;
            _themeToggle.ForeColor = Ui.SidebarText;
            _themeToggle.FlatAppearance.MouseOverBackColor = Ui.SidebarHover;
        }

        private void SetTheme(bool dark)
        {
            Ui.Dark = dark;
            _themeToggle.Text = dark ? "   Heller Modus" : "   Dunkler Modus";
            BackColor = Ui.Background;

            if (_content != null)
            {
                _content.BackColor = Ui.Background;
                Ui.Repaint(_content);
            }

            Icon icon = Ui.AppIcon();
            if (icon != null)
            {
                Icon = icon;
            }

            RepaintSidebar();
            if (_pages.Count > 0)
            {
                for (int i = 0; i < _pages.Count; i++)
                {
                    if (_pages[i].Visible)
                    {
                        ShowPage(i);
                        break;
                    }
                }
            }

            Ui.ApplyTitleBar(this);
            Ui.ApplyScrollBars(txtLog);
        }

        private void OnToggleShowPassword(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
        }

        private void OnRevert(object sender, EventArgs e)
        {
            LoadConfigIntoForm();
            Log("Eingaben verworfen - gespeicherter Stand neu geladen.");
        }

        /// <summary>Setzt alles auf die empfohlenen Standardwerte - Zugangsdaten bleiben erhalten.</summary>
        private void OnDefaults(object sender, EventArgs e)
        {
            string question = "Alle Einstellungen auf die empfohlenen Standardwerte setzen?"
                + Environment.NewLine + Environment.NewLine
                + "Bark Device Key, E-Mail-Adresse und App-Passwort bleiben erhalten."
                + Environment.NewLine
                + "Gespeichert wird erst mit \"Speichern und installieren\".";

            if (MessageBox.Show(this, question, "Standardwerte laden",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            ConfigModel defaults = new ConfigModel();
            defaults.BarkDeviceKey = txtBarkKey.Text.Trim();
            defaults.EmailAddress = txtEmail.Text.Trim();
            defaults.EmailAppPassword = txtPassword.Text;
            defaults.Unknown.AddRange(_carriedUnknown);

            ApplyToForm(defaults);
            Log("Standardwerte geladen (Zugangsdaten behalten). Zum Übernehmen auf \"Speichern und installieren\" klicken.");
        }

        /// <summary>Prüft die Eingaben, aktualisiert das Plugin und speichert die Einstellungen.</summary>
        private void OnApply(object sender, EventArgs e)
        {
            ConfigModel cfg = BuildConfigFromForm();

            List<string> problems = cfg.Validate();
            if (problems.Count > 0)
            {
                foreach (string problem in problems)
                {
                    Log("Hinweis: " + problem);
                }

                string message = "Bitte prüfen:" + Environment.NewLine + Environment.NewLine
                    + "- " + string.Join(Environment.NewLine + "- ", problems.ToArray())
                    + Environment.NewLine + Environment.NewLine + "Trotzdem speichern?";

                if (MessageBox.Show(this, message, "Unvollständige Konfiguration",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    Log("Abgebrochen - nichts geändert.");
                    return;
                }
            }

            string pluginDir = txtPluginDir.Text.Trim();
            if (pluginDir.Length == 0)
            {
                MessageBox.Show(this, "Bitte unter Installation den vPilot Plugins-Ordner angeben.",
                    "Kein Ordner", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string dll = txtDllSource.Text.Trim();
            if (dll.Length > 0 && File.Exists(dll))
            {
                try
                {
                    string report = PluginInstaller.Install(dll, pluginDir, true);
                    foreach (string line in report.Split('\n'))
                    {
                        Log(line.TrimEnd('\r'));
                    }
                }
                catch (Exception ex)
                {
                    Log("Plugin konnte nicht aktualisiert werden: " + ex.Message);
                    MessageBox.Show(this, "Das Plugin konnte nicht installiert werden:" + Environment.NewLine
                        + ex.Message + Environment.NewLine + Environment.NewLine
                        + "Läuft vPilot noch? Dann bitte schließen und erneut versuchen.",
                        "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                Log("Keine Plugin-Datei ausgewählt - es werden nur die Einstellungen gespeichert.");
            }

            try
            {
                cfg.Save(PluginInstaller.ConfigPath(pluginDir));
                Log("Einstellungen gespeichert.");
            }
            catch (Exception ex)
            {
                Log("SPEICHERN FEHLGESCHLAGEN: " + ex.Message);
                MessageBox.Show(this, "Die Einstellungen konnten nicht gespeichert werden:" + Environment.NewLine
                    + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            RefreshInstallStatus();
            Log("Fertig. vPilot neu starten, damit die Änderungen wirken.");
        }

        private async void OnTestBark(object sender, EventArgs e)
        {
            btnTestBark.Enabled = false;
            Log("Bark-Test (normale DM) läuft...");
            Log(await AlarmTester.SendBarkAsync(BuildConfigFromForm(), false));
            btnTestBark.Enabled = true;
        }

        private async void OnTestBarkAlarm(object sender, EventArgs e)
        {
            btnTestBarkAlarm.Enabled = false;
            Log("Bark-Test (ALARM) läuft...");
            Log(await AlarmTester.SendBarkAsync(BuildConfigFromForm(), true));
            btnTestBarkAlarm.Enabled = true;
        }

        private async void OnTestMail(object sender, EventArgs e)
        {
            btnTestMail.Enabled = false;
            Log("Testmail wird versendet (kann einige Sekunden dauern)...");
            Log(await AlarmTester.SendTestMailAsync(BuildConfigFromForm()));
            btnTestMail.Enabled = true;
        }

        private void RefreshInstallStatus()
        {
            try
            {
                lblInstallStatus.Text = PluginInstaller.DescribeInstallation(txtPluginDir.Text.Trim());
            }
            catch (Exception ex)
            {
                lblInstallStatus.Text = "Status unbekannt: " + ex.Message;
            }
        }

        private void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "] "
                + message + Environment.NewLine);
        }
    }
}
