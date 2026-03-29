using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Security.Principal;
using System.Linq;
using Microsoft.Win32;
using System.IO;

namespace Aim_X
{
    internal static class Program
    {
        public static NotifyIcon trayIcon;

        [STAThread]
        static void Main()
        {
            MessageBox.Show("App starting..."); // 🔥 DEBUG (remove later)

            try { AimXEngine.RevertAllSettings(); }
            catch (Exception ex) { LogError("RevertAllSettings failed", ex); }

            // ✅ ADMIN CHECK
            if (!IsRunningAsAdmin())
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        FileName = Application.ExecutablePath,
                        Verb = "runas"
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Admin required!\n" + ex.Message);
                }
                return;
            }

            // ✅ SINGLE INSTANCE (SAFE)
            try
            {
                var current = Process.GetCurrentProcess();
                var others = Process.GetProcessesByName(current.ProcessName);

                foreach (var p in others)
                {
                    if (p.Id != current.Id)
                    {
                        try { p.Kill(); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Instance check failed", ex);
            }

            // ✅ GLOBAL ERRORS
            Application.ThreadException += (s, e) =>
            {
                LogError("ThreadException", e.Exception);
                MessageBox.Show(e.Exception.ToString());
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                LogError("UnhandledException", (Exception)e.ExceptionObject);
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 🔐 LOGIN
            try
            {
                Login login = new Login();
                if (login.ShowDialog() != DialogResult.OK)
                    return;
            }
            catch (Exception ex)
            {
                LogError("Login failed", ex);
                MessageBox.Show(ex.ToString());
                return;
            }

            // 🚀 SPLASH
            try
            {
                using (SplashScreen splash = new SplashScreen())
                    splash.ShowDialog();
            }
            catch (Exception ex)
            {
                LogError("Splash failed", ex);
            }

            // ✅ CREATE TRAY AFTER UI READY
            SetupTray();

            // 🎯 MAIN LOOP
            Application.Run(new MainPanel());
        }

        private static bool IsRunningAsAdmin()
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void SetupTray()
        {
            try
            {
                trayIcon = new NotifyIcon();

                // ✅ SAFE ICON LOAD
                try
                {
                    trayIcon.Icon = SystemIcons.Application; // fallback safe
                }
                catch { }

                trayIcon.Visible = true;
                trayIcon.Text = "AIM X";

                ContextMenuStrip menu = new ContextMenuStrip();

                menu.Items.Add("Open", null, (s, e) => ShowMainPanel());

                menu.Items.Add("Optimize", null, (s, e) => TriggerMasterBoost());

                menu.Items.Add("Exit", null, (s, e) =>
                {
                    try { AimXEngine.RevertAllSettings(); } catch { }
                    trayIcon.Visible = false;
                    Application.Exit();
                });

                trayIcon.ContextMenuStrip = menu;
            }
            catch (Exception ex)
            {
                LogError("Tray failed", ex);
            }
        }

        private static void TriggerMasterBoost()
        {
            try
            {
                var main = Application.OpenForms.OfType<MainPanel>().FirstOrDefault();

                if (main != null)
                    main.Invoke(new Action(() => main.RunUltimateBoost()));
                else
                {
                    AimXEngine.OptimizeMouse();
                    AimXEngine.ApplyEngineTweaks();
                    AimXEngine.StabilizeFPS();
                    AimXEngine.CleanSystem();
                }
            }
            catch (Exception ex)
            {
                LogError("Boost failed", ex);
            }
        }

        private static void ShowMainPanel()
        {
            var main = Application.OpenForms.OfType<MainPanel>().FirstOrDefault();

            if (main != null)
            {
                main.Show();
                main.WindowState = FormWindowState.Normal;
                main.BringToFront();
            }
        }

        // ✅ SAFE LOG PATH
        private static void LogError(string title, Exception ex)
        {
            try
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AIM-X");

                Directory.CreateDirectory(path);

                File.AppendAllText(Path.Combine(path, "crash_log.txt"),
                    $"[{DateTime.Now}] {title}\n{ex}\n\n");
            }
            catch { }
        }
    }
}
