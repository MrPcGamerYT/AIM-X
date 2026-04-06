using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using Newtonsoft.Json;

namespace Aim_X
{
    public partial class Login : Form
    {
        private const string CryptKey = "9X_Aim_Secure_77_Alpha";

        // 🔥 FIREBASE CONFIG
        private static string firebaseApiKey = "AIzaSyA1ynP33ebHHyoPj8Qip_TigG5tYP-NcrI";
        private static string projectId = "aimx-38fd4";

        private bool isLoggingIn = false; // جلوگیری از دوبار اجرا

        [DllImport("kernel32.dll")] static extern bool IsDebuggerPresent();
        [DllImport("kernel32.dll")] static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);

        public Login()
        {
            ApplyExtremeSecurity();

            InitializeComponent();

            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;

            LoadSavedCredentials();
            InitSystem();

            this.Shown += async (s, e) => await AutoLogin(); // 🔥 AUTO LOGIN
        }

        // ================= AUTO LOGIN =================
        private async Task AutoLogin()
        {
            try
            {
                string savedUser = Decrypt(Properties.Settings.Default.Username);
                string savedPass = Decrypt(Properties.Settings.Default.Password);

                if (!string.IsNullOrEmpty(savedUser) && !string.IsNullOrEmpty(savedPass))
                {
                    user.Text = savedUser;
                    pass.Text = savedPass;
                    checkBox1.Checked = true;

                    status.Text = "Auto logging in...";

                    await AttemptLogin();
                }
            }
            catch
            {
                status.Text = "Auto login failed";
            }
        }

        // ================= SECURITY =================
        private void ApplyExtremeSecurity()
        {
            if (IsDebuggerPresent()) Environment.Exit(0);

            bool dbg = false;
            CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref dbg);
            if (dbg) Environment.Exit(0);

            if (IsVirtualMachine()) Environment.Exit(0);
        }

        private bool IsVirtualMachine()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
                {
                    foreach (var item in searcher.Get())
                    {
                        string m = item["Manufacturer"].ToString().ToLower();
                        string model = item["Model"].ToString().ToUpper();

                        if (m.Contains("vmware") || m.Contains("virtualbox") || model.Contains("VIRTUAL"))
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }

        // ================= HWID =================
        private string GetHWID()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("Select * from Win32_Processor"))
                {
                    foreach (var item in searcher.Get())
                        return item["ProcessorId"].ToString();
                }
            }
            catch { }
            return "UNKNOWN_HWID";
        }

        // ================= FIRESTORE =================
        private async Task<bool> CheckHWID(string email)
        {
            string hwid = GetHWID();

            using (var client = new HttpClient())
            {
                string url = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/users/{email}";

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var data = new
                    {
                        fields = new
                        {
                            hwid = new { stringValue = hwid }
                        }
                    };

                    var json = JsonConvert.SerializeObject(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    await PatchAsync(client, url, content);
                    return true;
                }

                var res = await response.Content.ReadAsStringAsync();
                dynamic obj = JsonConvert.DeserializeObject(res);

                string saved = obj.fields.hwid.stringValue;

                return saved == hwid;
            }
        }

        private async Task<HttpResponseMessage> PatchAsync(HttpClient client, string url, HttpContent content)
        {
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
            {
                Content = content
            };
            return await client.SendAsync(request);
        }

        // ================= ENCRYPT =================
        private string Encrypt(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
                sb.Append((char)(text[i] ^ CryptKey[i % CryptKey.Length]));

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        private string Decrypt(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            try
            {
                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(text));
                var sb = new StringBuilder();

                for (int i = 0; i < decoded.Length; i++)
                    sb.Append((char)(decoded[i] ^ CryptKey[i % CryptKey.Length]));

                return sb.ToString();
            }
            catch { return ""; }
        }

        private void LoadSavedCredentials()
        {
            try
            {
                string u = Decrypt(Properties.Settings.Default.Username);
                string p = Decrypt(Properties.Settings.Default.Password);

                if (!string.IsNullOrEmpty(u))
                {
                    user.Text = u;
                    pass.Text = p;
                    checkBox1.Checked = true;
                }
            }
            catch { }
        }

        private void InitSystem()
        {
            status.Text = "System: Ready";
        }

        private async void guna2TileButton1_Click(object sender, EventArgs e)
        {
            await AttemptLogin();
        }

        // ================= FIREBASE LOGIN =================
        private async Task<bool> FirebaseLogin(string email, string password)
        {
            using (var client = new HttpClient())
            {
                var requestData = new
                {
                    email = email,
                    password = password,
                    returnSecureToken = true
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={firebaseApiKey}",
                    content
                );

                return response.IsSuccessStatusCode;
            }
        }

        // ================= LOGIN =================
        private async Task AttemptLogin()
        {
            if (isLoggingIn) return;
            isLoggingIn = true;

            if (string.IsNullOrWhiteSpace(user.Text) || string.IsNullOrWhiteSpace(pass.Text))
            {
                isLoggingIn = false;
                return;
            }

            status.Text = "Authenticating...";
            ApplyExtremeSecurity();

            bool success = await FirebaseLogin(user.Text, pass.Text);

            if (!success)
            {
                status.Text = "Invalid login";
                isLoggingIn = false;
                return;
            }

            status.Text = "Checking device...";

            bool hwidOk = await CheckHWID(user.Text);

            if (!hwidOk)
            {
                status.Text = "Account already used on another PC!";
                isLoggingIn = false;
                return;
            }

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

            status.Text = "Login Success";
            DialogResult = DialogResult.OK;
            Close();

            isLoggingIn = false;
        }

        // ================= UI =================
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;

            using (GraphicsPath path = GetRoundedPath(ClientRectangle, 25))
            {
                Region = new Region(path);

                using (var brush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(45, 0, 0), Color.Black, 90f))
                    g.FillPath(brush, path);

                using (var pen = new Pen(Color.Red, 2))
                    g.DrawPath(pen, path);
            }
        }

        private GraphicsPath GetRoundedPath(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;

            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}