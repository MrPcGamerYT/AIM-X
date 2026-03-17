using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Security.Principal;

namespace Aim_X
{
    internal static class Program
    {
        public static NotifyIcon trayIcon = new NotifyIcon();

        [STAThread]
        static void Main()
        {
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

            // Process Guard
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

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            SetupTray();
            AimXEngine.OptimizeMouse();

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
                // 'icon' is the name in your Resources tab
                trayIcon.Icon = Properties.Resources.icon;
                trayIcon.Visible = true;
                trayIcon.Text = "AIM X - SUBSCRIBER EDITION";

                ContextMenuStrip menu = new ContextMenuStrip();

                menu.Items.Add("Open Aim X", null, (s, e) => {
                    foreach (Form f in Application.OpenForms)
                    {
                        if (f is MainPanel) { f.Show(); f.WindowState = FormWindowState.Normal; f.BringToFront(); }
                    }
                });
                menu.Items.Add("Optimize Now", null, (s, e) => AimXEngine.OptimizeMouse());
                menu.Items.Add("YouTube: MR.PC GAMER", null, (s, e) => Process.Start(new ProcessStartInfo("https://youtube.com/@MR.PC_GAMER_YT") { UseShellExecute = true }));
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
    }
}