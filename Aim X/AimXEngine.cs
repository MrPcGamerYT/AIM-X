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

        // --- 0. PRIVATE STORAGE (Restores USER settings, not defaults) ---
        private static string origSens, origSpeed, origThresh1, origThresh2;
        private static byte[] origXCurve, origYCurve;
        private static bool hasBackup = false;

        private static void BackupUserSettings()
        {
            if (hasBackup) return; 
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", false))
                {
                    if (key != null)
                    {
                        origSens = key.GetValue("MouseSensitivity", "10").ToString();
                        origSpeed = key.GetValue("MouseSpeed", "1").ToString();
                        origThresh1 = key.GetValue("MouseThreshold1", "6").ToString();
                        origThresh2 = key.GetValue("MouseThreshold2", "10").ToString();
                        origXCurve = (byte[])key.GetValue("SmoothMouseXCurve");
                        origYCurve = (byte[])key.GetValue("SmoothMouseYCurve");
                        hasBackup = true;
                    }
                }
            }
            catch { }
        }

        // --- 1. FPS STABILIZER ---
        public static void StabilizeFPS()
        {
            try
            {
                RunHiddenCommand("powercfg", "-setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"))
                {
                    if (key != null)
                    {
                        key.SetValue("SystemResponsiveness", 0, RegistryValueKind.DWord);
                        key.SetValue("NetworkThrottlingIndex", 0xFFFFFFFF, RegistryValueKind.DWord);
                    }
                }
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

        // --- 2. ENGINE OPTIMIZER ---
        public static void ApplyEngineTweaks()
        {
            try
            {
                Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore").SetValue("GameDVR_Enabled", 0, RegistryValueKind.DWord);
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers"))
                {
                    if (key != null) key.SetValue("HwSchMode", 2, RegistryValueKind.DWord);
                }
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"))
                {
                    if (key != null) key.SetValue("InterruptModeration", 0, RegistryValueKind.String);
                }
            }
            catch { }
        }

        // --- 3. MOUSE OPTIMIZATION (Now with Auto-Backup) ---
        public static void OptimizeMouse()
        {
            BackupUserSettings(); // Save user's current settings before changing anything
            try
            {
                SystemParametersInfo(0x0071, 0, (IntPtr)10, 0x01 | 0x02);
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Mouclass\Parameters"))
                {
                    if (key != null) key.SetValue("MouseDataQueueSize", 20, RegistryValueKind.DWord);
                }
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))
                {
                    if (key != null)
                    {
                        key.SetValue("MouseSensitivity", "10", RegistryValueKind.String);
                        key.SetValue("MouseSpeed", "0", RegistryValueKind.String);
                        key.SetValue("MouseThreshold1", "0", RegistryValueKind.String);
                        key.SetValue("MouseThreshold2", "0", RegistryValueKind.String);
                        key.SetValue("MouseHoverTime", "8", RegistryValueKind.String);

                        byte[] killerCurve = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00 };
                        key.SetValue("SmoothMouseXCurve", killerCurve, RegistryValueKind.Binary);
                        key.SetValue("SmoothMouseYCurve", killerCurve, RegistryValueKind.Binary);
                    }
                }
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                {
                    if (key != null) key.SetValue("ForegroundLockTimeout", 0, RegistryValueKind.DWord);
                }
            }
            catch { }
        }

        // --- 4. CONFIG INJECTION ---
        public static void InjectEmulatorTweaks()
        {
            try
            {
                string[] targets = { "BlueStacks_nxt", "MSI_AppPlayer", "LDPlayer", "SmartGaGa" };
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                {
                    foreach (var target in targets)
                    {
                        string[] paths = { Path.Combine(drive.Name, "ProgramData", target, "bluestacks.conf"), Path.Combine(drive.Name, "Program Files", target, "bluestacks.conf") };
                        foreach (string path in paths)
                        {
                            if (File.Exists(path))
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
                        }
                    }
                }
            }
            catch { }
        }

        // --- 5. CLEANER ---
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

        // --- 6. MASTER RUN ---
        public static void RunAllOptimizations()
        {
            OptimizeMouse();
            StabilizeFPS();
            ApplyEngineTweaks();
            InjectEmulatorTweaks();
            CleanSystem();
        }

        // --- 7. REVERT (Restores User's Personal Settings) ---
        public static void RevertAllSettings()
        {
            if (!hasBackup) return;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))
                {
                    if (key != null)
                    {
                        key.SetValue("MouseSensitivity", origSens, RegistryValueKind.String);
                        key.SetValue("MouseSpeed", origSpeed, RegistryValueKind.String);
                        key.SetValue("MouseThreshold1", origThresh1, RegistryValueKind.String);
                        key.SetValue("MouseThreshold2", origThresh2, RegistryValueKind.String);
                        
                        if (origXCurve != null) key.SetValue("SmoothMouseXCurve", origXCurve, RegistryValueKind.Binary);
                        else key.DeleteValue("SmoothMouseXCurve", false);

                        if (origYCurve != null) key.SetValue("SmoothMouseYCurve", origYCurve, RegistryValueKind.Binary);
                        else key.DeleteValue("SmoothMouseYCurve", false);
                    }
                }
                // Push the restored sensitivity to the system
                SystemParametersInfo(0x0071, 0, (IntPtr)int.Parse(origSens), 0x01 | 0x02);
            }
            catch { }
        }

        private static void RunHiddenCommand(string file, string args)
        {
            try { Process.Start(new ProcessStartInfo(file, args) { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = false }); } catch { }
        }
    }
}
