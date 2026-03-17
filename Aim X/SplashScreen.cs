using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Aim_X
{
    public partial class SplashScreen : Form
    {
        private float progressValue = 0;
        private System.Windows.Forms.Timer autoTimer = new System.Windows.Forms.Timer();

        private string[] subLogs = {
            "AIM-X: Locating Emulator...",
            "YT: MR.PC GAMER Config v21...",
            "BYPASS: Bypassing Registry...",
            "SENS: Loading 6.80 Y-Axis...",
            "READY: 100% HS READY!"
        };

        public SplashScreen()
        {
            InitializeComponent();
            Updater.CheckAndUpdate();
            this.DoubleBuffered = true;
            this.Size = new Size(620, 380);

            // --- LOCK SIZE PREVENT FULLSCREEN ---
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;
            this.MaximizeBox = false;

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(10, 10, 10);

            autoTimer.Interval = 15;
            autoTimer.Tick += (s, e) => {
                if (progressValue < 100)
                {
                    progressValue += 0.7f;
                    this.Invalidate();
                }
                else
                {
                    autoTimer.Stop();
                    this.Hide();
                    new MainPanel().Show();
                }
            };
            autoTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int borderRadius = 25;
            int width = this.ClientRectangle.Width;
            int height = this.ClientRectangle.Height;
            int centerX = width / 2;

            using (GraphicsPath path = GetRoundedPath(this.ClientRectangle, borderRadius))
            {
                this.Region = new Region(path);
                using (LinearGradientBrush bgBrush = new LinearGradientBrush(this.ClientRectangle,
                    Color.FromArgb(45, 0, 0), Color.FromArgb(10, 10, 10), 90f))
                {
                    g.FillPath(bgBrush, path);
                }
            }

            Rectangle borderRect = this.ClientRectangle;
            borderRect.Inflate(-1, -1);
            using (GraphicsPath borderPath = GetRoundedPath(borderRect, borderRadius))
            using (Pen perfectionPen = new Pen(Color.Red, 1.5f))
            {
                perfectionPen.Alignment = PenAlignment.Center;
                g.DrawPath(perfectionPen, borderPath);
            }

            using (Font logoFont = new Font("Corpta DEMO", 42, FontStyle.Bold | FontStyle.Italic))
            {
                string text = "AIM X";
                SizeF textSize = g.MeasureString(text, logoFont);
                float tx = centerX - (textSize.Width / 2);
                float ty = 110;
                g.DrawString(text, logoFont, Brushes.Black, tx + 3, ty + 3);
                using (SolidBrush glowBrush = new SolidBrush(Color.FromArgb(100, 255, 0, 0)))
                    g.DrawString(text, logoFont, glowBrush, tx + 1, ty + 1);
                g.DrawString(text, logoFont, Brushes.White, tx, ty);
            }

            using (Font subTagFont = new Font("Corpta DEMO", 9, FontStyle.Bold | FontStyle.Italic))
            {
                string tag = "SUBSCRIBER EDITION";
                SizeF tagSize = g.MeasureString(tag, subTagFont);
                g.DrawString(tag, subTagFont, Brushes.Gold, centerX - (tagSize.Width / 2), 185);
            }

            int barWidth = 500;
            int barX = centerX - (barWidth / 2);
            int barY = 280;
            g.FillRectangle(new SolidBrush(Color.FromArgb(25, 25, 25)), new Rectangle(barX, barY, barWidth, 6));

            int currentWidth = (int)((barWidth * progressValue) / 100);
            if (currentWidth > 5)
            {
                Rectangle progressRect = new Rectangle(barX, barY, currentWidth, 6);
                using (LinearGradientBrush pBrush = new LinearGradientBrush(progressRect,
                    Color.DarkRed, Color.Red, LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(pBrush, progressRect);
                }
                g.FillRectangle(Brushes.White, progressRect.Right - 3, barY, 3, 6);
            }

            using (Font consoleFont = new Font("Segoe UI", 8, FontStyle.Bold | FontStyle.Italic))
            {
                int logIndex = Math.Min((int)(progressValue / 21), subLogs.Length - 1);
                g.DrawString("> " + subLogs[logIndex], consoleFont, Brushes.Lime, barX, barY - 22);
                string pct = (int)progressValue + "%";
                SizeF pctSize = g.MeasureString(pct, consoleFont);
                g.DrawString(pct, consoleFont, Brushes.Red, (barX + barWidth) - pctSize.Width, barY - 22);
            }

            using (Font channelFont = new Font("Arial", 7, FontStyle.Bold))
            {
                string watermark = "MR.PC GAMER // 2026 STABLE BUILD";
                SizeF wmSize = g.MeasureString(watermark, channelFont);
                g.DrawString(watermark, channelFont, Brushes.DimGray, centerX - (wmSize.Width / 2), 310);
            }
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // --- FULLSCREEN BLOCKER & DRAGGER ---
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            const int HTCAPTION = 0x02;
            const int HTCLIENT = 0x01;
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MAXIMIZE = 0xF030;
            const int WM_NCLBUTTONDBLCLK = 0x00A3; // Double click on caption

            // Block Maximize via Snap/Shortcut
            if (m.Msg == WM_SYSCOMMAND && (int)m.WParam == SC_MAXIMIZE) return;

            // Block Double Click to Maximize
            if (m.Msg == WM_NCLBUTTONDBLCLK) return;

            base.WndProc(ref m);

            if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT)
            {
                Point screenPoint = new Point(m.LParam.ToInt32());
                Point clientPoint = this.PointToClient(screenPoint);
                if (clientPoint.Y <= 60) m.Result = (IntPtr)HTCAPTION;
            }
        }
    }
}