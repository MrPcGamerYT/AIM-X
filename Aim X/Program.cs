using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Security.Principal;
using System.Threading;
using System.Linq;
using Microsoft.Win32;

namespace Aim_X
{
    internal static class Program
    {
        public static NotifyIcon trayIcon = new NotifyIcon();

        [STAThread]
        static void Main()
        {
            try { AimXEngine.RevertAllSettings(); } catch { }

            // Admin Check
            if (!IsRunningAsAdmin())
            {
                ProcessStartInfo proc = new ProcessStartInfo();
                proc.UseShellExecute = true;
                proc.WorkingDirectory = Environment.CurrentDirectory;
                proc.FileName = Application.ExecutablePath;
                proc.Verb = "runas";
                try { Process.Start(proc); } catch { }
                return;
            }

            // Prevent Multiple Instances
            string currentName = Process.GetCurrentProcess().ProcessName;
            Process[] duplicates = Process.GetProcessesByName(currentName);
            if (duplicates.Length > 1)
            {
                foreach (var p in duplicates)
                {
                    if (p.Id != Process.GetCurrentProcess().Id)
                    {
                        try { p.Kill(); p.WaitForExit(1000); } catch { }
                    }
                }
            }

            // Global handlers
            Application.ThreadException += (s, e) => { AimXEngine.RevertAllSettings(); };
            AppDomain.CurrentDomain.ProcessExit += (s, e) => { AimXEngine.RevertAllSettings(); };

            SystemEvents.SessionEnding += (s, e) => {
                AimXEngine.RevertAllSettings();
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            SetupTray();

            // 🔐 STEP 1: LOGIN
            Login login = new Login();

            if (login.ShowDialog() != DialogResult.OK)
                return; // exit if failed

            // 🚀 STEP 2: SPLASH (blocking)
            using (SplashScreen splash = new SplashScreen())
            {
                splash.ShowDialog();
            }

            // 🎯 STEP 3: MAIN PANEL (main loop)
            Application.Run(new MainPanel());
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
                // Note: Ensure your icon is named 'icon' in Resources
                trayIcon.Icon = Properties.Resources.icon;
                trayIcon.Visible = true;
                trayIcon.Text = "AIM X - SUBSCRIBER EDITION";

                ContextMenuStrip menu = new ContextMenuStrip();

                menu.Items.Add("Open Aim X", null, (s, e) => {
                    ShowMainPanel();
                });

                // --- THE ULTIMATE MASTER OPTIMIZER BUTTON ---
                var masterOptimizeBtn = new ToolStripMenuItem("Optimize Now (Ultimate Boost)");
                masterOptimizeBtn.Font = new Font(masterOptimizeBtn.Font, FontStyle.Bold);
                masterOptimizeBtn.Click += (s, e) => {
                    TriggerMasterBoost();
                };
                menu.Items.Add(masterOptimizeBtn);

                menu.Items.Add("YouTube: MR.PC GAMER", null, (s, e) => {
                    Process.Start(new ProcessStartInfo("https://youtube.com/@MR.PC_GAMER_YT") { UseShellExecute = true });
                });

                menu.Items.Add("-");

                menu.Items.Add("Exit & Revert", null, (s, e) => {
                    AimXEngine.RevertAllSettings();
                    trayIcon.Visible = false;
                    Application.Exit();
                    Environment.Exit(0);
                });

                trayIcon.ContextMenuStrip = menu;
            }
            catch { trayIcon.Icon = SystemIcons.Shield; }
        }

        // Helper to find the MainPanel and run its logic safely
        private static void TriggerMasterBoost()
        {
            MainPanel main = Application.OpenForms.OfType<MainPanel>().FirstOrDefault();
            
            if (main != null)
            {
                // UI Sync: invoke the boost logic on the MainPanel thread
                main.Invoke(new Action(() => main.RunUltimateBoost()));
                trayIcon.ShowBalloonTip(3000, "Aim X Engine", "Ultimate Boost Applied Successfully!", ToolTipIcon.Info);
            }
            else
            {
                // Background Mode: If UI isn't open, run engine-only optimization
                AimXEngine.OptimizeMouse();
                AimXEngine.ApplyEngineTweaks();
                AimXEngine.StabilizeFPS();
                AimXEngine.CleanSystem();
                trayIcon.ShowBalloonTip(3000, "Aim X Engine", "System Optimized (Background Mode)", ToolTipIcon.Info);
            }
        }

        private static void ShowMainPanel()
        {
            MainPanel main = Application.OpenForms.OfType<MainPanel>().FirstOrDefault();
            if (main != null)
            {
                main.Show();
                main.WindowState = FormWindowState.Normal;
                main.BringToFront();
            }
        }
    }
}
