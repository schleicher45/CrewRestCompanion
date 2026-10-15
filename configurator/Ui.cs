using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CrewRestConfigurator
{
    /// <summary>Farben, Schriften und Bausteine der Oberfläche an einer Stelle.</summary>
    internal static class Ui
    {
        /// <summary>Dunkelmodus ist der Standard.</summary>
        public static bool Dark = true;

        // --- Farben, je nach Modus ---
        public static Color Background { get { return Dark ? Rgb(22, 26, 34) : Rgb(244, 245, 247); } }
        public static Color Surface { get { return Dark ? Rgb(30, 36, 48) : Color.White; } }
        public static Color CardBorder { get { return Dark ? Rgb(45, 53, 68) : Rgb(226, 229, 234); } }
        public static Color Field { get { return Dark ? Rgb(16, 20, 27) : Color.White; } }
        public static Color Text { get { return Dark ? Rgb(230, 233, 239) : Rgb(31, 36, 48); } }
        public static Color Muted { get { return Dark ? Rgb(150, 160, 175) : Rgb(107, 114, 128); } }
        public static Color QuietButton { get { return Dark ? Rgb(38, 45, 58) : Color.White; } }
        public static Color QuietBorder { get { return Dark ? Rgb(60, 70, 86) : Rgb(205, 210, 218); } }
        public static Color QuietHover { get { return Dark ? Rgb(48, 57, 72) : Rgb(240, 242, 245); } }
        public static Color LogBackground { get { return Dark ? Rgb(16, 20, 27) : Rgb(250, 250, 251); } }

        // Seitenleiste
        public static Color Sidebar { get { return Dark ? Rgb(20, 25, 35) : Rgb(233, 236, 242); } }
        public static Color SidebarHover { get { return Dark ? Rgb(38, 46, 62) : Rgb(217, 223, 235); } }
        public static Color SidebarText { get { return Dark ? Rgb(166, 176, 192) : Rgb(74, 81, 98); } }
        public static Color SidebarStrong { get { return Dark ? Color.White : Rgb(31, 36, 48); } }
        public static Color SidebarFaint { get { return Dark ? Rgb(138, 149, 166) : Rgb(130, 138, 155); } }

        public static Color Accent { get { return Dark ? Rgb(88, 140, 255) : Rgb(47, 111, 237); } }
        public static Color AccentHover { get { return Dark ? Rgb(70, 122, 240) : Rgb(37, 96, 212); } }

        public static readonly Font Body = new Font("Segoe UI", 9.75f);
        public static readonly Font Title = new Font("Segoe UI Semibold", 10.5f);
        public static readonly Font Brand = new Font("Segoe UI Semibold", 13f);
        public static readonly Font Nav = new Font("Segoe UI", 10f);
        public static readonly Font HintFont = new Font("Segoe UI", 8.25f);
        public static readonly Font Mono = new Font("Consolas", 9f);

        private static Color Rgb(int r, int g, int b)
        {
            return Color.FromArgb(r, g, b);
        }


        // --- Teile, die Windows selbst zeichnet ---

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y,
            int cx, int cy, uint flags);

        private const int DwmUseImmersiveDarkMode = 20;
        private const int DwmUseImmersiveDarkModeOld = 19;
        private const uint SwpFrameChanged = 0x0020 | 0x0002 | 0x0001 | 0x0004;

        /// <summary>Faerbt die Fensterleiste mit (Windows 10 ab 2004 und Windows 11).</summary>
        public static void ApplyTitleBar(Form form)
        {
            if (form == null || !form.IsHandleCreated)
            {
                return;
            }

            try
            {
                int value = Dark ? 1 : 0;
                if (DwmSetWindowAttribute(form.Handle, DwmUseImmersiveDarkMode, ref value, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(form.Handle, DwmUseImmersiveDarkModeOld, ref value, sizeof(int));
                }

                // Rahmen neu zeichnen lassen, sonst wechselt die Farbe erst beim naechsten Mal.
                SetWindowPos(form.Handle, IntPtr.Zero, 0, 0, 0, 0, SwpFrameChanged);
            }
            catch (Exception)
            {
                // Aeltere Windows-Versionen koennen das nicht - dann bleibt die Leiste hell.
            }
        }

        /// <summary>Schaltet die Bildlaufleiste eines Steuerelements auf dunkel.</summary>
        public static void ApplyScrollBars(Control control)
        {
            if (control == null || !control.IsHandleCreated)
            {
                return;
            }

            try
            {
                SetWindowTheme(control.Handle, Dark ? "DarkMode_Explorer" : "Explorer", null);
            }
            catch (Exception)
            {
                // Nicht kritisch.
            }
        }

        /// <summary>Holt das zum Modus passende Programmsymbol aus der EXE.</summary>
        public static Icon AppIcon()
        {
            string name = Dark ? "icon.dark.ico" : "icon.light.ico";
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                {
                    if (stream != null)
                    {
                        return new Icon(stream);
                    }
                }
            }
            catch (Exception)
            {
                // Ohne Symbol laeuft es auch.
            }
            return null;
        }

        // --- Bausteine ---

        public static Label Label(string text, int x, int y, int width)
        {
            Label label = new Label();
            label.Text = text;
            label.Location = new Point(x, y);
            label.Size = new Size(width, 22);
            label.ForeColor = Text;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        public static Label Hint(string text, int x, int y, int width, int height)
        {
            Label hint = new Label();
            hint.Text = text;
            hint.Location = new Point(x, y);
            hint.Size = new Size(width, height);
            hint.Font = HintFont;
            hint.ForeColor = Muted;
            return hint;
        }

        public static TextBox TextField(TextBox box, int x, int y, int width)
        {
            box.Location = new Point(x, y);
            box.Size = new Size(width, 24);
            box.BorderStyle = BorderStyle.FixedSingle;
            box.BackColor = Field;
            box.ForeColor = Text;
            return box;
        }

        public static NumericUpDown Number(NumericUpDown box, int x, int y, int width,
            int min, int max, int value)
        {
            box.Location = new Point(x, y);
            box.Size = new Size(width, 24);
            box.BorderStyle = BorderStyle.FixedSingle;
            box.BackColor = Field;
            box.ForeColor = Text;
            box.Minimum = min;
            box.Maximum = max;
            box.Value = value;
            PaintSpinButtons(box);
            return box;
        }

        /// <summary>Faerbt die Pfeiltasten eines Zahlenfeldes mit ein.</summary>
        public static void PaintSpinButtons(NumericUpDown box)
        {
            if (box.Controls.Count > 0)
            {
                box.Controls[0].BackColor = Field;
                box.Controls[0].ForeColor = Text;
            }
        }

        public static ComboBox Combo(ComboBox box, int x, int y, int width)
        {
            box.Location = new Point(x, y);
            box.Size = new Size(width, 24);
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.FlatStyle = FlatStyle.Flat;
            box.BackColor = Field;
            box.ForeColor = Text;
            return box;
        }

        public static CheckBox Check(CheckBox box, string text, int x, int y, int width)
        {
            box.Text = text;
            box.Location = new Point(x, y);
            box.Size = new Size(width, 22);
            box.ForeColor = Text;
            box.BackColor = Surface;
            box.FlatStyle = FlatStyle.Flat;
            box.FlatAppearance.BorderColor = QuietBorder;
            box.FlatAppearance.CheckedBackColor = Accent;
            return box;
        }

        /// <summary>Unauffälliger Knopf mit Rahmen - für Nebenaktionen.</summary>
        public static Button Quiet(Button button, string text, int x, int y, int width, int height,
            EventHandler onClick)
        {
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(width, height);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = QuietBorder;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = QuietHover;
            button.BackColor = QuietButton;
            button.ForeColor = Text;
            button.UseVisualStyleBackColor = false;
            button.Cursor = Cursors.Hand;
            button.Click += onClick;
            return button;
        }

        /// <summary>Hervorgehobener Knopf für die Hauptaktion.</summary>
        public static Button Primary(Button button, string text, int x, int y, int width, int height,
            EventHandler onClick)
        {
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(width, height);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = AccentHover;
            button.BackColor = Accent;
            button.ForeColor = Color.White;
            button.Font = Title;
            button.UseVisualStyleBackColor = false;
            button.Cursor = Cursors.Hand;
            button.Tag = "primary";
            button.Click += onClick;
            return button;
        }

        /// <summary>
        /// Färbt einen Bereich nach dem aktuellen Modus neu ein. Die Seitenleiste wird
        /// übersprungen, sie bleibt in beiden Modi dunkel.
        /// </summary>
        public static void Repaint(Control root)
        {
            foreach (Control control in root.Controls)
            {
                Card card = control as Card;
                if (card != null)
                {
                    card.BackColor = Surface;
                    Repaint(card);
                    card.Invalidate();
                    continue;
                }

                TextBox textBox = control as TextBox;
                if (textBox != null)
                {
                    textBox.BackColor = textBox.ReadOnly ? LogBackground : Field;
                    textBox.ForeColor = Text;
                    continue;
                }

                NumericUpDown number = control as NumericUpDown;
                if (number != null)
                {
                    number.BackColor = Field;
                    number.ForeColor = Text;
                    PaintSpinButtons(number);
                    DarkNumeric dark = number as DarkNumeric;
                    if (dark != null)
                    {
                        dark.RepaintButtons();
                    }
                    continue;
                }

                ComboBox combo = control as ComboBox;
                if (combo != null)
                {
                    combo.BackColor = Field;
                    combo.ForeColor = Text;
                    continue;
                }

                CheckBox check = control as CheckBox;
                if (check != null)
                {
                    check.ForeColor = Text;
                    check.BackColor = Surface;
                    check.FlatAppearance.BorderColor = QuietBorder;
                    check.FlatAppearance.CheckedBackColor = Accent;
                    continue;
                }

                Button button = control as Button;
                if (button != null)
                {
                    if ("primary".Equals(button.Tag))
                    {
                        button.BackColor = Accent;
                        button.ForeColor = Color.White;
                    }
                    else
                    {
                        button.BackColor = QuietButton;
                        button.ForeColor = Text;
                        button.FlatAppearance.BorderColor = QuietBorder;
                        button.FlatAppearance.MouseOverBackColor = QuietHover;
                    }
                    continue;
                }

                Label label = control as Label;
                if (label != null)
                {
                    if ("accent".Equals(label.Tag))
                    {
                        label.ForeColor = Accent;
                    }
                    else if (label.Font == HintFont)
                    {
                        label.ForeColor = Muted;
                    }
                    else
                    {
                        label.ForeColor = Text;
                    }
                    continue;
                }

                Panel panel = control as Panel;
                if (panel != null)
                {
                    panel.BackColor = Background;
                    Repaint(panel);
                }
            }
        }
    }


    /// <summary>
    /// Auswahlfeld, das Rahmen und Pfeil selbst zeichnet - Windows malt sie sonst hell,
    /// was im Dunkelmodus auffaellt.
    /// </summary>
    internal sealed class FlatCombo : ComboBox
    {
        private const int WmPaint = 0x000F;

        public FlatCombo()
        {
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg != WmPaint)
            {
                return;
            }

            using (Graphics g = CreateGraphics())
            {
                int buttonWidth = 20;
                Rectangle button = new Rectangle(Width - buttonWidth - 1, 1, buttonWidth, Height - 2);

                using (SolidBrush brush = new SolidBrush(Ui.Field))
                {
                    g.FillRectangle(brush, button);
                }

                g.SmoothingMode = SmoothingMode.AntiAlias;
                int cx = button.Left + button.Width / 2;
                int cy = button.Top + button.Height / 2;
                Point[] arrow =
                {
                    new Point(cx - 4, cy - 2),
                    new Point(cx + 4, cy - 2),
                    new Point(cx, cy + 3)
                };
                using (SolidBrush brush = new SolidBrush(Ui.Muted))
                {
                    g.FillPolygon(brush, arrow);
                }

                g.SmoothingMode = SmoothingMode.Default;
                using (Pen pen = new Pen(Ui.QuietBorder))
                {
                    g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            }
        }
    }



    /// <summary>
    /// Merkt sich den Anzeigemodus zwischen zwei Starts. Liegt im Benutzerprofil, damit
    /// es auch funktioniert, wenn das Programm in einem schreibgeschuetzten Ordner liegt.
    /// Beim ersten Start - also nach der Installation - ist dunkel voreingestellt.
    /// </summary>
    internal static class UiSettings
    {
        private static string FilePath()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CrewRestCompanion");
            return Path.Combine(folder, "ui.txt");
        }

        public static bool LoadDark()
        {
            try
            {
                string path = FilePath();
                if (File.Exists(path))
                {
                    string content = File.ReadAllText(path).Trim().ToLowerInvariant();
                    if (content.Contains("light"))
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                // Im Zweifel dunkel.
            }
            return true;
        }

        public static void SaveDark(bool dark)
        {
            try
            {
                string path = FilePath();
                string folder = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                File.WriteAllText(path, "theme=" + (dark ? "dark" : "light") + Environment.NewLine);
            }
            catch (Exception)
            {
                // Nicht speichern zu koennen ist kein Grund zum Absturz.
            }
        }
    }

    /// <summary>
    /// Zahlenfeld, dessen Pfeiltasten mitgefaerbt werden. Windows zeichnet sie sonst hell.
    /// </summary>
    internal sealed class DarkNumeric : NumericUpDown
    {
        private ButtonPainter _painter;

        public DarkNumeric()
        {
            BorderStyle = BorderStyle.FixedSingle;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_painter == null && Controls.Count > 0)
            {
                _painter = new ButtonPainter(Controls[0]);
            }
        }

        public void RepaintButtons()
        {
            if (Controls.Count > 0)
            {
                Controls[0].Invalidate();
            }
        }

        private sealed class ButtonPainter : NativeWindow
        {
            private const int WmPaint = 0x000F;
            private readonly Control _buttons;

            public ButtonPainter(Control buttons)
            {
                _buttons = buttons;
                AssignHandle(buttons.Handle);
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                if (m.Msg == WmPaint)
                {
                    Draw();
                }
            }

            private void Draw()
            {
                try
                {
                    using (Graphics g = Graphics.FromHwnd(_buttons.Handle))
                    {
                        Rectangle area = new Rectangle(0, 0, _buttons.Width, _buttons.Height);
                        using (SolidBrush brush = new SolidBrush(Ui.Field))
                        {
                            g.FillRectangle(brush, area);
                        }

                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        int cx = area.Width / 2;
                        int upY = area.Height / 4;
                        int downY = area.Height - area.Height / 4;

                        using (SolidBrush brush = new SolidBrush(Ui.Muted))
                        {
                            g.FillPolygon(brush, new[]
                            {
                                new Point(cx - 4, upY + 2), new Point(cx + 4, upY + 2), new Point(cx, upY - 3)
                            });
                            g.FillPolygon(brush, new[]
                            {
                                new Point(cx - 4, downY - 2), new Point(cx + 4, downY - 2), new Point(cx, downY + 3)
                            });
                        }

                        g.SmoothingMode = SmoothingMode.Default;
                        using (Pen pen = new Pen(Ui.QuietBorder))
                        {
                            g.DrawRectangle(pen, 0, 0, area.Width - 1, area.Height - 1);
                        }
                    }
                }
                catch (Exception)
                {
                    // Zeichnen darf nie die Anwendung stoppen.
                }
            }
        }
    }

    /// <summary>Fläche mit abgerundeten Ecken, dünnem Rahmen und Überschrift.</summary>
    internal sealed class Card : Panel
    {
        private const int Radius = 10;

        public Card(string title, int x, int y, int width, int height)
        {
            Location = new Point(x, y);
            Size = new Size(width, height);
            BackColor = Ui.Surface;

            if (!string.IsNullOrEmpty(title))
            {
                Label caption = new Label();
                caption.Text = title;
                caption.Font = Ui.Title;
                caption.ForeColor = Ui.Text;
                caption.Location = new Point(16, 14);
                caption.Size = new Size(width - 32, 22);
                Controls.Add(caption);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            using (GraphicsPath path = RoundedPath(new Rectangle(0, 0, Width, Height), Radius))
            {
                Region = new Region(path);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), Radius))
            using (Pen pen = new Pen(Ui.CardBorder))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
