using KeyAuth;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text;
using System.Runtime.InteropServices;
using System.Linq;

namespace AppSystem_Utility
{
    public partial class Login : Form
    {
        private const string CryptKey = "9X_Aim_Secure_77_Alpha"; 

        private static api ServiceProvider = new api(
            name: "Aim X",
            ownerid: "P7SA5qAcgj",
            version: "1.0"
        );

        // --- ANTI-DEBUG NATIVE IMPORTS ---
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool IsDebuggerPresent();

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);

        public Login()
        {
            // Execute Security Check BEFORE anything else
            RunSecurityShield();

            InitializeComponent();
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            LoadEncryptedState();
            StartValidation();
        }

        // --- 1. THE SECURITY SHIELD (Anti-Crack) ---
        private void RunSecurityShield()
        {
            // Check for basic Debuggers
            if (IsDebuggerPresent()) Environment.Exit(0);

            bool isDebuggerPresent = false;
            CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref isDebuggerPresent);
            if (isDebuggerPresent) Environment.Exit(0);

            // Blacklisted Programs (Crack tools)
            string[] blacklist = { "dnspy", "x64dbg", "ollydbg", "wireshark", "fiddler", "httpdebugger", "de4dot", "processhacker" };
            foreach (var process in Process.GetProcesses())
            {
                if (blacklist.Any(b => process.ProcessName.ToLower().Contains(b)))
                {
                    process.Kill(); // Kill the crack tool
                    Environment.Exit(0); // Close our app
                }
            }
        }

        // --- 2. ENCRYPTION ENGINE ---
        private string Encrypt(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var result = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
                result.Append((char)(text[i] ^ CryptKey[i % CryptKey.Length]));
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(result.ToString()));
        }

        private string Decrypt(string base64Text)
        {
            if (string.IsNullOrEmpty(base64Text)) return string.Empty;
            try
            {
                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64Text));
                var result = new StringBuilder();
                for (int i = 0; i < decoded.Length; i++)
                    result.Append((char)(decoded[i] ^ CryptKey[i % CryptKey.Length]));
                return result.ToString();
            }
            catch { return string.Empty; }
        }

        private void LoadEncryptedState()
        {
            string savedUser = Decrypt(Properties.Settings.Default.Username);
            string savedPass = Decrypt(Properties.Settings.Default.Password);

            if (!string.IsNullOrEmpty(savedUser) && !string.IsNullOrEmpty(savedPass))
            {
                user.Text = savedUser;
                pass.Text = savedPass;
                checkBox1.Checked = true;
            }
        }

        private async void StartValidation()
        {
            status.Text = "System: Protected Mode";
            await Task.Run(() => ServiceProvider.init());

            if (!ServiceProvider.response.success)
            {
                status.Text = "Auth Error: Contact Support";
                return;
            }

            status.Text = "System Secure.";

            if (checkBox1.Checked && !string.IsNullOrWhiteSpace(user.Text))
                await ExecuteAuth();
        }

        private async void guna2TileButton1_Click(object sender, EventArgs e)
        {
            await ExecuteAuth();
        }

        private async Task ExecuteAuth()
        {
            if (string.IsNullOrWhiteSpace(user.Text) || string.IsNullOrWhiteSpace(pass.Text))
            {
                status.Text = "Input Required.";
                return;
            }

            status.Text = "Authenticating...";
            
            // Re-check security during login attempt
            RunSecurityShield();

            await Task.Run(() => ServiceProvider.login(user.Text, pass.Text));

            if (ServiceProvider.response.success)
            {
                status.Text = "Success!";
                SaveEncryptedData(checkBox1.Checked);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                status.Text = "Status: " + ServiceProvider.response.message;
            }
        }

        private void SaveEncryptedData(bool remember)
        {
            if (remember)
            {
                Properties.Settings.Default.Username = Encrypt(user.Text);
                Properties.Settings.Default.Password = Encrypt(pass.Text);
            }
            else
            {
                Properties.Settings.Default.Username = string.Empty;
                Properties.Settings.Default.Password = string.Empty;
            }
            Properties.Settings.Default.Save();
        }

        // --- UI & RENDERING ---
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int radius = 25;
            using (GraphicsPath path = GetRoundedPath(this.ClientRectangle, radius))
            {
                this.Region = new Region(path);
                using (LinearGradientBrush b = new LinearGradientBrush(this.ClientRectangle, Color.FromArgb(45, 0, 0), Color.FromArgb(15, 15, 15), 90f))
                {
                    g.FillPath(b, path);
                }
                using (Pen p = new Pen(Color.Red, 2f))
                    g.DrawPath(p, path);
            }
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int r)
        {
            GraphicsPath path = new GraphicsPath();
            float d = r * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void LaunchPortal(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
        }

        private void guna2ImageButton1_Click(object sender, EventArgs e) => LaunchPortal("http://www.youtube.com/@MR.PC_GAMER_YT");
        private void guna2ImageButton2_Click(object sender, EventArgs e) => LaunchPortal("https://discord.gg/5qkKPRZkWa");
    }
}
