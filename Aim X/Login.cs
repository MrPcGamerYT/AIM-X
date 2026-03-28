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
using System.Management;

namespace Aim_X
{
    public partial class Login : Form
    {
        // Secret key for internal encryption - you can change this!
        private const string CryptKey = "9X_Aim_Secure_77_Alpha";

        private static api KeyAuthApp = new api(
            name: "Aim X",
            ownerid: "P7SA5qAcgj",
            version: "1.0"
        );

        // --- NATIVE SECURITY IMPORTS ---
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool IsDebuggerPresent();

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);

        public Login()
        {
            // 🔥 Check for crackers before the window even opens
            ApplyExtremeSecurity();

            InitializeComponent();

            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;

            LoadSavedCredentials();
            InitKeyAuth();
        }

        private void ApplyExtremeSecurity()
        {
            // 1. Basic Debugger Check
            if (IsDebuggerPresent()) Environment.Exit(0);

            // 2. Remote Debugger Check (Advanced)
            bool isDebuggerPresent = false;
            CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref isDebuggerPresent);
            if (isDebuggerPresent) Environment.Exit(0);

            // 3. Virtual Machine Protection
            if (IsVirtualMachine()) Environment.Exit(0);

            // 4. Blacklist Scanner
            string[] blacklist = { "dnspy", "x64dbg", "ollydbg", "wireshark", "fiddler", "httpdebugger", "processhacker", "de4dot", "detectiteasy" };
            foreach (var process in Process.GetProcesses())
            {
                if (blacklist.Any(b => process.ProcessName.ToLower().Contains(b)))
                {
                    try { process.Kill(); } catch { }
                    Environment.Exit(0);
                }
            }
        }

        private bool IsVirtualMachine()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
                using (var items = searcher.Get())
                {
                    foreach (var item in items)
                    {
                        string manufacturer = item["Manufacturer"].ToString().ToLower();
                        if (manufacturer.Contains("vmware") || manufacturer.Contains("virtualbox") || item["Model"].ToString().ToUpperInvariant().Contains("VIRTUAL"))
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }

        // --- DATA ENCRYPTION ---
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

        private void LoadSavedCredentials()
        {
            try
            {
                string savedUser = Decrypt(Properties.Settings.Default.Username);
                string savedPass = Decrypt(Properties.Settings.Default.Password);
                if (!string.IsNullOrEmpty(savedUser))
                {
                    user.Text = savedUser;
                    pass.Text = savedPass;
                    checkBox1.Checked = true;
                }
            }
            catch { }
        }

        private async void InitKeyAuth()
        {
            status.Text = "System: Syncing...";
            await Task.Run(() => KeyAuthApp.init());
            if (!KeyAuthApp.response.success)
            {
                status.Text = "Connection Error.";
                return;
            }
            status.Text = "System: Protected.";
            if (checkBox1.Checked && !string.IsNullOrEmpty(user.Text)) await AttemptLogin();
        }

        private async void guna2TileButton1_Click(object sender, EventArgs e) => await AttemptLogin();

        private async Task AttemptLogin()
        {
            if (string.IsNullOrWhiteSpace(user.Text) || string.IsNullOrWhiteSpace(pass.Text)) return;
            status.Text = "Authenticating...";
            ApplyExtremeSecurity();

            await Task.Run(() => KeyAuthApp.login(user.Text, pass.Text));

            if (KeyAuthApp.response.success)
            {
                if (checkBox1.Checked)
                {
                    Properties.Settings.Default.Username = Encrypt(user.Text);
                    Properties.Settings.Default.Password = Encrypt(pass.Text);
                }
                else
                {
                    Properties.Settings.Default.Username = "";
                    Properties.Settings.Default.Password = "";
                }
                Properties.Settings.Default.Save();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else status.Text = "Error: " + KeyAuthApp.response.message;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked)
            {
                user.Clear();
                pass.Clear();
                Properties.Settings.Default.Username = "";
                Properties.Settings.Default.Password = "";
                Properties.Settings.Default.Save();
            }
        }

        // --- CUSTOM UI RENDERING ---
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            using (GraphicsPath path = GetRoundedPath(this.ClientRectangle, 25))
            {
                this.Region = new Region(path);
                using (LinearGradientBrush brush = new LinearGradientBrush(this.ClientRectangle, Color.FromArgb(45, 0, 0), Color.FromArgb(10, 10, 10), 90f))
                    g.FillPath(brush, path);
                using (Pen pen = new Pen(Color.Red, 2f)) g.DrawPath(pen, path);
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

        private void LaunchPortal(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
        }

        private void guna2ImageButton1_Click(object sender, EventArgs e) => LaunchPortal("http://www.youtube.com/@MR.PC_GAMER_YT");
        private void guna2ImageButton2_Click(object sender, EventArgs e) => LaunchPortal("https://discord.gg/5qkKPRZkWa");
    }
}