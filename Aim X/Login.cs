using KeyAuth;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Aim_X
{
    public partial class Login : Form
    {
        private static api KeyAuthApp = new api(
            name: "Aim X",
            ownerid: "P7SA5qAcgj",
            version: "1.0"
        );

        public Login()
        {
            InitializeComponent();

            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(10, 10, 10);

            // Load saved credentials automatically
            LoadSavedCredentials();

            InitKeyAuth();
        }

        private void LoadSavedCredentials()
        {
            // Load saved username/password if remember me was checked
            if (!string.IsNullOrEmpty(Properties.Settings.Default.Username) &&
                !string.IsNullOrEmpty(Properties.Settings.Default.Password))
            {
                user.Text = Properties.Settings.Default.Username;
                pass.Text = Properties.Settings.Default.Password;
                checkBox1.Checked = true; // automatically check
            }
        }

        private async void InitKeyAuth()
        {
            status.Text = "Connecting...";
            await Task.Run(() => KeyAuthApp.init());

            if (!KeyAuthApp.response.success)
            {
                status.Text = "Auth failed: " + KeyAuthApp.response.message;
                return;
            }

            status.Text = "Connected.";

            // 🔥 AUTO LOGIN if Remember Me is checked
            if (checkBox1.Checked && !string.IsNullOrEmpty(user.Text) && !string.IsNullOrEmpty(pass.Text))
            {
                await AttemptLogin();
            }
        }

        private async void guna2TileButton1_Click(object sender, EventArgs e)
        {
            await AttemptLogin();
        }

        private async Task AttemptLogin()
        {
            if (string.IsNullOrWhiteSpace(user.Text) || string.IsNullOrWhiteSpace(pass.Text))
            {
                status.Text = "Enter username and password.";
                return;
            }

            status.Text = "Logging in...";
            await Task.Run(() => KeyAuthApp.login(user.Text, pass.Text));

            if (KeyAuthApp.response.success)
            {
                status.Text = "Login successful!";

                // 🔥 Remember Me auto-save logic
                if (checkBox1.Checked)
                {
                    Properties.Settings.Default.Username = user.Text;
                    Properties.Settings.Default.Password = pass.Text;
                }
                else
                {
                    Properties.Settings.Default.Username = "";
                    Properties.Settings.Default.Password = "";
                }
                Properties.Settings.Default.Save();

                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                status.Text = "Login failed: " + KeyAuthApp.response.message;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            // Clear credentials if unchecked
            if (!checkBox1.Checked)
            {
                user.Clear();
                pass.Clear();
                Properties.Settings.Default.Username = "";
                Properties.Settings.Default.Password = "";
                Properties.Settings.Default.Save();
            }
        }

        // ================= PERFECT BORDER + ROUNDED LIKE SPLASH =================
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int borderRadius = 25; // same as SplashScreen
            int width = this.ClientRectangle.Width;
            int height = this.ClientRectangle.Height;

            // --- Rounded Background ---
            using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, width, height), borderRadius))
            {
                this.Region = new Region(path);

                using (LinearGradientBrush brush = new LinearGradientBrush(
                    this.ClientRectangle,
                    Color.FromArgb(45, 0, 0),
                    Color.FromArgb(10, 10, 10),
                    90f))
                {
                    g.FillPath(brush, path);
                }
            }

            // --- Inner Border (fully inside, not cut) ---
            float penWidth = 1.5f;
            RectangleF borderRect = new RectangleF(penWidth / 2, penWidth / 2, width - penWidth, height - penWidth);

            using (GraphicsPath borderPath = GetRoundedPath(Rectangle.Round(borderRect), borderRadius))
            using (Pen pen = new Pen(Color.Red, penWidth))
            {
                pen.Alignment = PenAlignment.Center;
                g.DrawPath(pen, borderPath);
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

        // --- External links ---
        private void guna2ImageButton1_Click(object sender, EventArgs e) =>
            Process.Start(new ProcessStartInfo { FileName = "http://www.youtube.com/@MR.PC_GAMER_YT", UseShellExecute = true });

        private void guna2ImageButton2_Click(object sender, EventArgs e) =>
            Process.Start(new ProcessStartInfo { FileName = "https://discord.gg/5qkKPRZkWa", UseShellExecute = true });
    }
}