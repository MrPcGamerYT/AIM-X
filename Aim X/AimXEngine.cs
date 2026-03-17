using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Aim_X
{
    public static class AimXEngine
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        // --- 1. FPS STABILIZER ---
        public static void StabilizeFPS()
        {
            try
            {
                RunHiddenCommand("powercfg", "-setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                RunHiddenCommand("bcdedit", "/set disabledynamictick yes");
                RunHiddenCommand("bcdedit", "/set useplatformclock no");

                string[] emuProcesses = { "HD-Player", "LdVBoxHeadless", "dnplayer", "SmartGaGa", "aow_exe", "AndroidProcess" };
                foreach (var name in emuProcesses)
                {
                    foreach (var p in Process.GetProcessesByName(name))
                    {
                        try { p.PriorityClass = ProcessPriorityClass.High; } catch { }
                    }
                }
                RunHiddenCommand("cmd.exe", "/c ipconfig /flushdns");
            }
            catch { }
        }

        // --- 2. ENGINE OPTIMIZER (HAGS) ---
        public static void ApplyEngineTweaks()
        {
            try
            {
                Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore").SetValue("GameDVR_Enabled", 0, RegistryValueKind.DWord);
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers"))
                {
                    if (key != null) key.SetValue("HwSchMode", 2, RegistryValueKind.DWord);
                }
            }
            catch { }
        }

        // --- 3. MOUSE OPTIMIZATION (v21 RAW) ---
        public static void OptimizeMouse()
        {
            try
            {
                SystemParametersInfo(0x0071, 0, (IntPtr)10, 0x01 | 0x02);
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))
                {
                    if (key != null)
                    {
                        key.SetValue("MouseSensitivity", "10", RegistryValueKind.String);
                        key.SetValue("MouseSpeed", "0", RegistryValueKind.String);
                        key.SetValue("MouseThreshold1", "0", RegistryValueKind.String);
                        key.SetValue("MouseThreshold2", "0", RegistryValueKind.String);

                        byte[] v21Curve = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00 };
                        key.SetValue("SmoothMouseXCurve", v21Curve, RegistryValueKind.Binary);
                        key.SetValue("SmoothMouseYCurve", v21Curve, RegistryValueKind.Binary);
                    }
                }
            }
            catch { }
        }

        // --- 4. CLEANER (FIXED: NO MORE BLACK SCREEN) ---
        public static void CleanSystem()
        {
            try
            {
                // REMOVED taskkill explorer to prevent the black screen crash.
                // Instead, we just clear the cache files.
                
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

        // --- 5. AUTOMATIC REVERT (SAVES MOUSE DEFAULTS) ---
        public static void RevertAllSettings()
        {
            try
            {
                // Windows Default Mouse Speed (10) and Precision (Off/Default)
                SystemParametersInfo(0x0071, 0, (IntPtr)10, 0x01 | 0x02);
                
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))
                {
                    if (key != null)
                    {
                        key.SetValue("MouseSpeed", "1", RegistryValueKind.String);
                        key.SetValue("MouseThreshold1", "6", RegistryValueKind.String);
                        key.SetValue("MouseThreshold2", "10", RegistryValueKind.String);
                        // Delete curves to return to default Windows movement
                        key.DeleteValue("SmoothMouseXCurve", false);
                        key.DeleteValue("SmoothMouseYCurve", false);
                    }
                }
            }
            catch { }
        }

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
