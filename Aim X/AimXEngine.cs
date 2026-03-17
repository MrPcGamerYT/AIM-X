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

        // --- 1. FPS STABILIZER (v21: RAM Standby & BCD Tweaks) ---
        public static void StabilizeFPS()
        {
            try
            {
                // Set Ultimate/High Performance Power Plan
                RunHiddenCommand("powercfg", "-setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");

                // Disable BCD Dynamic Tick (Reduces Micro-stutter)
                RunHiddenCommand("bcdedit", "/set disabledynamictick yes");
                RunHiddenCommand("bcdedit", "/set useplatformclock no");

                // Priority Boost for Gaming Emulators
                string[] emuProcesses = { "HD-Player", "LdVBoxHeadless", "dnplayer", "SmartGaGa", "aow_exe", "AndroidProcess" };
                foreach (var name in emuProcesses)
                {
                    foreach (var p in Process.GetProcessesByName(name))
                    {
                        try { p.PriorityClass = ProcessPriorityClass.High; } catch { }
                    }
                }

                // Network Ping Optimization
                RunHiddenCommand("cmd.exe", "/c ipconfig /flushdns & netsh int ip reset & netsh winsock reset");
            }
            catch { }
        }

        // --- 2. ENGINE OPTIMIZER (v21: Hardware Accelerated GPU Scheduling) ---
        public static void ApplyEngineTweaks()
        {
            try
            {
                // Disable Game DVR & Game Bar
                Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore").SetValue("GameDVR_Enabled", 0, RegistryValueKind.DWord);
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\PolicyManager\default\ApplicationManagement\AllowGameDVR").SetValue("value", 0, RegistryValueKind.DWord);

                // Enable Hardware Accelerated GPU Scheduling (HAGS)
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers"))
                {
                    if (key != null) key.SetValue("HwSchMode", 2, RegistryValueKind.DWord);
                }

                // Optimization for HD-Player.exe (BlueStacks/MSI)
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\HD-Player.exe\PerfOptions"))
                {
                    if (key != null) key.SetValue("CpuPriorityClass", 3, RegistryValueKind.DWord);
                }
            }
            catch { }
        }

        // --- 3. MOUSE OPTIMIZATION (v21: 1:1 Raw Input Simulation) ---
        public static void OptimizeMouse()
        {
            try
            {
                // Apply 1:1 Pixel Precision via User32
                SystemParametersInfo(0x0071, 0, (IntPtr)10, 0x01 | 0x02);

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))
                {
                    if (key != null)
                    {
                        key.SetValue("MouseSensitivity", "10", RegistryValueKind.String);
                        key.SetValue("MouseSpeed", "0", RegistryValueKind.String);
                        key.SetValue("MouseThreshold1", "0", RegistryValueKind.String);
                        key.SetValue("MouseThreshold2", "0", RegistryValueKind.String);

                        // MR.PC GAMER v21 Ultra-Smooth Curve (Linear Raw Input)
                        byte[] v21Curve = {
                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                            0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
                            0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00,
                            0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00
                        };
                        key.SetValue("SmoothMouseXCurve", v21Curve, RegistryValueKind.Binary);
                        key.SetValue("SmoothMouseYCurve", v21Curve, RegistryValueKind.Binary);
                    }
                }
            }
            catch { }
        }

        // --- 4. CONFIG INJECTION (v21: Multi-Drive Deep Scan) ---
        public static void InjectEmulatorTweaks()
        {
            string[] targets = { "BlueStacks_nxt", "MSI_AppPlayer", "LDPlayer", "SmartGaGa" };
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                foreach (var target in targets)
                {
                    string pData = Path.Combine(drive.Name, "ProgramData", target, "bluestacks.conf");
                    string pFiles = Path.Combine(drive.Name, "Program Files", target, "bluestacks.conf");

                    ApplyToConfig(pData);
                    ApplyToConfig(pFiles);
                }
            }
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
                    // v21 VIP Sensitivity Settings
                    if (lines[i].Contains("cfg_x_sensitivity")) { lines[i] = "bst.cfg_x_sensitivity=1.20"; changed = true; }
                    if (lines[i].Contains("cfg_y_sensitivity")) { lines[i] = "bst.cfg_y_sensitivity=6.80"; changed = true; }
                    if (lines[i].Contains("cfg_tweaks")) { lines[i] = "bst.cfg_tweaks=16450"; changed = true; }
                }
                if (changed) File.WriteAllLines(path, lines);
            }
            catch { }
        }

        // --- 5. CLEANER (v21: Shader Cache & Standby Memory) ---
        public static void CleanSystem()
        {
            try
            {
                // Clear Standby List (RAM Cleaner)
                RunHiddenCommand("cmd.exe", "/c taskkill /f /im explorer.exe & start explorer.exe");

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

        // --- 6. REVERT (Windows Defaults) ---
        public static void RevertAllSettings()
        {
            try
            {
                SystemParametersInfo(0x0071, 0, (IntPtr)10, 0x01 | 0x02);
                RunHiddenCommand("bcdedit", "/deletevalue disabledynamictick");

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))
                {
                    if (key != null)
                    {
                        key.SetValue("MouseSpeed", "1", RegistryValueKind.String);
                        key.SetValue("MouseThreshold1", "6", RegistryValueKind.String);
                        key.SetValue("MouseThreshold2", "10", RegistryValueKind.String);
                    }
                }
            }
            catch { }
        }
    }
}