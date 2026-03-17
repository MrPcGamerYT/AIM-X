using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;

namespace Aim_X
{
    public static class AimXEngine
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        // --- 1. UNIVERSAL MOUSE OPTIMIZATION (v20 STABLE) ---
        public static void OptimizeMouse()
        {
            try
            {
                // Apply 1:1 Pixel Precision
                SystemParametersInfo(0x0071, 0, (IntPtr)10, 0x01 | 0x02);

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))
                {
                    if (key != null)
                    {
                        key.SetValue("MouseSensitivity", "10", RegistryValueKind.String);
                        key.SetValue("MouseSpeed", "0", RegistryValueKind.String);
                        
                        // MR.PC GAMER v20 High-Speed Pull Curve
                        byte[] v20Curve = { 
                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00 
                        };
                        key.SetValue("SmoothMouseXCurve", v20Curve, RegistryValueKind.Binary);
                        key.SetValue("SmoothMouseYCurve", v20Curve, RegistryValueKind.Binary);
                    }
                }
            }
            catch { }
        }

        // --- 2. CONFIG INJECTION (v20 DEEP SCAN) ---
        public static void InjectEmulatorTweaks()
        {
            try
            {
                string[] targets = { "BlueStacks_nxt", "MSI_AppPlayer", "LDPlayer", "SmartGaGa" };
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                {
                    foreach (var target in targets)
                    {
                        // v20 Scan Logic
                        ApplyToConfig(Path.Combine(drive.Name, "ProgramData", target, "bluestacks.conf"));
                        ApplyToConfig(Path.Combine(drive.Name, target, "vms", "config.ini"));
                    }
                }
            }
            catch { }
        }

        private static void ApplyToConfig(string path)
        {
            if (!File.Exists(path)) return;
            try
            {
                var lines = File.ReadAllLines(path).ToList();
                bool changed = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Contains("cfg_x_sensitivity")) { lines[i] = "bst.cfg_x_sensitivity=1.20"; changed = true; }
                    if (lines[i].Contains("cfg_y_sensitivity")) { lines[i] = "bst.cfg_y_sensitivity=6.80"; changed = true; }
                    if (lines[i].Contains("cfg_tweaks")) { lines[i] = "bst.cfg_tweaks=16450"; changed = true; }
                }
                if (changed) File.WriteAllLines(path, lines);
            }
            catch { }
        }

        // --- 3. SYSTEM CLEANER (v20 STABLE) ---
        public static void CleanSystem()
        {
            try
            {
                string[] folders = { Path.GetTempPath(), "C:\\Windows\\Temp", "C:\\Windows\\Prefetch" };
                foreach (var folder in folders)
                {
                    if (!Directory.Exists(folder)) continue;
                    DirectoryInfo di = new DirectoryInfo(folder);
                    foreach (FileInfo file in di.GetFiles()) { try { file.Delete(); } catch { } }
                    foreach (DirectoryInfo dir in di.GetDirectories()) { try { dir.Delete(true); } catch { } }
                }
            }
            catch { }
        }

        // --- 4. PROCESS GUARD (v20 STABLE) ---
        public static void RunProcessGuard()
        {
            try
            {
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
            }
            catch { }
        }

        // --- HELPER: Run Commands Silently ---
        private static void RunHiddenCommand(string file, string args)
        {
            try
            {
                Process.Start(new ProcessStartInfo(file, args)
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false
                });
            }
            catch { }
        }
    }
}
