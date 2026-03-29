using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Security.Principal;
using System.Threading;
using System.Linq;
using Microsoft.Win32;
using System.IO;

namespace Aim_X
{
    internal static class Program
    {
        public static NotifyIcon trayIcon = new NotifyIcon();

        [STAThread]
        static void Main()
        {
            try
            {
                AimXEngine.RevertAllSettings();
            }
            catch (Exception ex)
            {
                LogError("RevertAllSettings failed", ex);
            }

            // ✅ ADMIN CHECK (SAFE)
            if (!IsRunningAsAdmin())
            {
                try
                {
                    ProcessStartInfo proc = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        WorkingDirectory = Environment.CurrentDirectory,
                        FileName = Application.ExecutablePath,
                        Verb = "runas"
                    };

                    Process.Start(proc);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Admin permission required!\n\n" + ex.Message);
                }

                return;
            }

            // ✅ PREVENT MULTIPLE INSTANCES
            try
            {
                string currentName = Process.GetCurrentProcess().ProcessName;
                var duplicates = Process.GetProcessesByName(currentName);

                foreach (var p in duplicates)
                {
                    if (p.Id != Process.GetCurrentProcess().Id)
                    {
                        try { p.Kill(); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Duplicate process check failed", ex);
            }

            // ✅ GLOBAL ERROR HANDLING
            Application.ThreadException += (s, e) =>
            {
                LogError("ThreadException", e.Exception);
                MessageBox.Show(e.Exception.ToString());
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                LogError("UnhandledException", (Exception)e.ExceptionObject);
            };

            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                try { AimXEngine.RevertAllSettings(); } catch { }
            };

            SystemEvents.SessionEnding += (s, e) =>
            {
                try { AimXEngine.RevertAllSettings(); } catch { }
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            SetupTray();

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
                {
                    splash.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                LogError("Splash failed", ex);
            }

            // 🎯 MAIN
            try
            {
                Application.Run(new MainPanel());
            }
            catch (Exception ex)
            {
                LogError("MainPanel crashed", ex);
                MessageBox.Show(ex.ToString());
            }
        }

        private static bool IsRunningAsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void SetupTray()
        {
            try
            {
                trayIcon.Icon = Properties.Resources.icon;
                trayIcon.Visible = true;
                trayIcon.Text = "AIM X - SUBSCRIBER EDITION";

                ContextMenuStrip menu = new ContextMenuStrip();

                menu.Items.Add("Open Aim X", null, (s, e) => {
                    ShowMainPanel();
                });

                var masterOptimizeBtn = new ToolStripMenuItem("Optimize Now (Ultimate Boost)");
                masterOptimizeBtn.Font = new Font(masterOptimizeBtn.Font, FontStyle.Bold);
                masterOptimizeBtn.Click += (s, e) => {
                    TriggerMasterBoost();
                };

                menu.Items.Add(masterOptimizeBtn);

                menu.Items.Add("YouTube", null, (s, e) => {
                    Process.Start(new ProcessStartInfo("https://youtube.com") { UseShellExecute = true });
                });

                menu.Items.Add("-");

                menu.Items.Add("Exit & Revert", null, (s, e) => {
                    try { AimXEngine.RevertAllSettings(); } catch { }
                    trayIcon.Visible = false;
                    Application.Exit();
                });

                trayIcon.ContextMenuStrip = menu;
            }
            catch (Exception ex)
            {
                LogError("Tray setup failed", ex);
                trayIcon.Icon = SystemIcons.Application;
            }
        }

        private static void TriggerMasterBoost()
        {
            try
            {
                MainPanel main = Application.OpenForms.OfType<MainPanel>().FirstOrDefault();

                if (main != null)
                {
                    main.Invoke(new Action(() => main.RunUltimateBoost()));
                }
                else
                {
                    AimXEngine.OptimizeMouse();
                    AimXEngine.ApplyEngineTweaks();
                    AimXEngine.StabilizeFPS();
                    AimXEngine.CleanSystem();
                }

                trayIcon.ShowBalloonTip(3000, "Aim X", "Optimization Applied!", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                LogError("Boost failed", ex);
            }
        }

        private static void ShowMainPanel()
        {
            try
            {
                MainPanel main = Application.OpenForms.OfType<MainPanel>().FirstOrDefault();

                if (main != null)
                {
                    main.Show();
                    main.WindowState = FormWindowState.Normal;
                    main.BringToFront();
                }
            }
            catch (Exception ex)
            {
                LogError("ShowMainPanel failed", ex);
            }
        }

        // ✅ LOG FILE
        private static void LogError(string title, Exception ex)
        {
            try
            {
                File.AppendAllText("crash_log.txt",
                    $"[{DateTime.Now}] {title}\n{ex}\n\n");
            }
            catch { }
        }
    }
}
