using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Security.Principal;
using System.Threading;
using System.Linq;
using Microsoft.Win32; // REQUIRED for Shutdown detection

namespace Aim_X
{
    internal static class Program
    {
        public static NotifyIcon trayIcon = new NotifyIcon();

        [STAThread]
        static void Main()
        {
            // --- EMERGENCY REVERT ON STARTUP ---
            // If the PC crashed last time, this cleans the registry immediately
            try { AimXEngine.RevertAllSettings(); } catch { }

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

            // Kill duplicates
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

            // --- SHUTDOWN & CRASH HANDLERS ---
            Application.ThreadException += (s, e) => { AimXEngine.RevertAllSettings(); };
            AppDomain.CurrentDomain.ProcessExit += (s, e) => { AimXEngine.RevertAllSettings(); };
            
            // This catches when the user clicks "Shut Down" or "Restart" in Windows
            SystemEvents.SessionEnding += (s, e) => { 
                AimXEngine.RevertAllSettings(); 
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            SetupTray();
            
            Application.Run(new SplashScreen());
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

                menu.Items.Add("Open Aim X", null, (s, e) => { ShowMainPanel(); });

                var masterOptimizeBtn = new ToolStripMenuItem("Optimize Now (Ultimate Boost)");
                masterOptimizeBtn.Font = new Font(masterOptimizeBtn.Font, FontStyle.Bold);
                masterOptimizeBtn.Click += (s, e) => { TriggerMasterBoost(); };
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

        private static void TriggerMasterBoost()
        {
            MainPanel main = Application.OpenForms.OfType<MainPanel>().FirstOrDefault();
            if (main != null)
            {
                main.Invoke(new Action(() => main.RunUltimateBoost()));
                trayIcon.ShowBalloonTip(3000, "Aim X Engine", "Ultimate Boost Applied Successfully!", ToolTipIcon.Info);
            }
            else
            {
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
