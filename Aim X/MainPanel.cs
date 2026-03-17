using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;

namespace Aim_X
{
    public partial class MainPanel : Form
    {
        public MainPanel()
        {
            InitializeComponent();

            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(10, 10, 10);

            // --- LOCK SIZE PREVENT FULLSCREEN ---
            this.Size = new Size(800, 450); 
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;
            this.MaximizeBox = false;

            lblStatus.Text = "STATUS: IDLE (READY)";
            lblStatus.ForeColor = Color.White;

            // Restore user settings when the app is completely closed
            Application.ApplicationExit += (s, e) => {
                AimXEngine.RevertAllSettings();
            };

            Program.trayIcon.MouseDoubleClick += (s, e) => { ShowForm(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            int borderRadius = 25;

            using (GraphicsPath path = GetRoundedPath(this.ClientRectangle, borderRadius))
            {
                this.Region = new Region(path);
                using (LinearGradientBrush lgb = new LinearGradientBrush(this.ClientRectangle,
                    Color.FromArgb(35, 0, 0), Color.FromArgb(10, 10, 10), LinearGradientMode.Vertical))
                {
                    g.FillPath(lgb, path);
                }
            }

            Rectangle borderRect = this.ClientRectangle;
            borderRect.Inflate(-1, -1);

            using (GraphicsPath borderPath = GetRoundedPath(borderRect, borderRadius))
            {
                using (Pen perfectionPen = new Pen(Color.Red, 1.5f))
                {
                    perfectionPen.Alignment = PenAlignment.Center;
                    g.DrawPath(perfectionPen, borderPath);
                }
            }
            base.OnPaint(e);
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            if (d <= 0) d = 1;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                Program.trayIcon.ShowBalloonTip(2000, "Aim X v21", "Running in Background!", ToolTipIcon.Info);
            }
            else
            {
                // If the app is actually quitting (e.g., from Tray Exit)
                AimXEngine.RevertAllSettings();
            }
            base.OnFormClosing(e);
        }

        private void ShowForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
        }

        // --- BUTTON EVENTS ---
        private void btnOptimize_Click(object sender, EventArgs e)
        {
            UpdateStatus("OPTIMIZING HID & MOUSE...", Color.Lime);
            AimXEngine.OptimizeMouse();
            UpdateStatus("MOUSE OPTIMIZED!", Color.Lime);
        }

        private void btnFPS_Click(object sender, EventArgs e)
        {
            UpdateStatus("UNPARKING CORES & BOOSTING FPS...", Color.Red);
            AimXEngine.StabilizeFPS();
            UpdateStatus("FPS BOOST ACTIVE!", Color.Red);
        }

        private void UpdateStatus(string message, Color statusColor)
        {
            lblStatus.Text = "STATUS: " + message;
            lblStatus.ForeColor = statusColor;
        }

        private void btnInject_Click_1(object sender, EventArgs e)
        {
            UpdateStatus("INJECTING SENSITIVITY...", Color.Gold);
            AimXEngine.InjectEmulatorTweaks();
            UpdateStatus("CONFIGS INJECTED!", Color.Gold);
        }

        private void btnClean_Click_1(object sender, EventArgs e)
        {
            UpdateStatus("DELETING TRASH FILES...", Color.Cyan);
            AimXEngine.CleanSystem();
            UpdateStatus("SYSTEM CLEANED!", Color.Cyan);
        }

        private void btnEngine_Click_1(object sender, EventArgs e)
        {
            UpdateStatus("APPLYING REGISTRY TWEAKS...", Color.Magenta);
            AimXEngine.ApplyEngineTweaks();
            UpdateStatus("ENGINE TWEAKED!", Color.Magenta);
        }

        // --- THE MASTER BOOSTER (Everything in One Click) ---
        private void guna2Button2_Click(object sender, EventArgs e)
        {
            try
            {
                UpdateStatus("OPTIMIZING MOUSE & HID...", Color.Lime);
                AimXEngine.OptimizeMouse();
                Application.DoEvents();
                Thread.Sleep(300);

                UpdateStatus("TWEAKING ENGINE...", Color.Magenta);
                AimXEngine.ApplyEngineTweaks();
                Application.DoEvents();
                Thread.Sleep(300);

                UpdateStatus("UNPARKING CPU CORES...", Color.Red);
                AimXEngine.StabilizeFPS(); // This now includes Core Unparking
                Application.DoEvents();
                Thread.Sleep(300);

                UpdateStatus("CLEANING JUNK FILES...", Color.Cyan);
                AimXEngine.CleanSystem();
                Application.DoEvents();
                Thread.Sleep(300);

                UpdateStatus("INJECTING CONFIGS...", Color.Gold);
                AimXEngine.InjectEmulatorTweaks();
                Application.DoEvents();
                Thread.Sleep(500);

                UpdateStatus("ULTIMATE BOOST COMPLETE!", Color.Lime);
            }
            catch
            {
                UpdateStatus("ERROR DURING BOOST!", Color.OrangeRed);
            }
        }

        private void guna2ImageButton1_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = "http://www.youtube.com/@MR.PC_GAMER_YT", UseShellExecute = true });
        }

        private void guna2ImageButton2_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = "https://discord.gg/XbqcMzwfQQ", UseShellExecute = true });
        }

        // --- WINDOWS DRAGGING & SNAP LOGIC ---
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            const int HTCAPTION = 0x02;
            const int HTCLIENT = 0x01;
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MAXIMIZE = 0xF030;
            const int WM_NCLBUTTONDBLCLK = 0x00A3; 

            if (m.Msg == WM_SYSCOMMAND && (int)m.WParam == SC_MAXIMIZE) return;
            if (m.Msg == WM_NCLBUTTONDBLCLK) return;

            base.WndProc(ref m);

            if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT)
            {
                short x = (short)(m.LParam.ToInt32() & 0xFFFF);
                short y = (short)((m.LParam.ToInt32() >> 16) & 0xFFFF);
                
                Point clientPoint = this.PointToClient(new Point(x, y));

                if (clientPoint.Y <= 50)
                {
                    m.Result = (IntPtr)HTCAPTION;
                }
            }
        }
    }
}
