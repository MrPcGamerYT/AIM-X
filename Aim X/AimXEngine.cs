using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Aim_X
{
    public static class AimXEngine
    {
        // --- Kernel-Level Imports ---
        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        public static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, out uint CurrentResolution);

        [DllImport("kernel32.dll")]
        static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

        private static byte[] origXCurve, origYCurve;
        private static bool hasBackup = false;

        // --- 1. BACKUP SYSTEM ---
        private static void BackupUserSettings()
        {
            if (hasBackup) return; 
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", false))
                {
                    if (key != null)
                    {
                        origXCurve = (byte[])key.GetValue("SmoothMouseXCurve");
                        origYCurve = (byte[])key.GetValue("SmoothMouseYCurve");
                        hasBackup = true;
                    }
                }
            }
            catch { }
        }

        // --- 2. MOUSE OPTIMIZATION (THE MAGNET) ---
        public static void OptimizeMouse()
        {
            string[] emuNames = { "HD-Player", "LdVBoxHeadless", "dnplayer", "SmartGaGa", "aow_exe", "AndroidProcess" };
            if (!emuNames.Any(t => Process.GetProcessesByName(t).Length > 0)) return;

            BackupUserSettings();
            try
            {
                uint curRes;
                NtSetTimerResolution(5000, true, out curRes); // 0.5ms Delay

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))
                {
                    if (key != null)
                    {
                        byte[] magnetCurve = { 
                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00 
                        };
                        key.SetValue("SmoothMouseXCurve", magnetCurve, RegistryValueKind.Binary);
                        key.SetValue("SmoothMouseYCurve", magnetCurve, RegistryValueKind.Binary);
                        key.SetValue("MouseSpeed", "0", RegistryValueKind.String);
                    }
                }

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Mouclass\Parameters"))
                {
                    if (key != null) 
                    {
                        key.SetValue("MouseDataQueueSize", 20, RegistryValueKind.DWord);
                        key.SetValue("ThreadPriority", 31, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        // --- 3. STABILIZE FPS (Now Includes RAM Lock & Affinity) ---
        public static void StabilizeFPS()
        {
            try
            {
                // Core Parking & Power
                RunHiddenCommand("powercfg", "-setacvalueindex scheme_current sub_processor cppm_set_all_cores_parking 100");
                RunHiddenCommand("powercfg", "-setactive scheme_current");

                // BCD Latency Fix
                RunHiddenCommand("bcdedit", "/set disabledynamictick yes");
                RunHiddenCommand("bcdedit", "/set useplatformclock no");

                // Multimedia Registry Tweaks
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"))
                {
                    if (key != null)
                    {
                        key.SetValue("SystemResponsiveness", 0, RegistryValueKind.DWord);
                        key.SetValue("NetworkThrottlingIndex", 0xFFFFFFFF, RegistryValueKind.DWord);
                    }
                }

                // Process Steering: Core Affinity & RAM Lock
                string[] targets = { "HD-Player", "LdVBoxHeadless", "dnplayer", "SmartGaGa", "aow_exe", "AndroidProcess" };
                foreach (var name in targets)
                {
                    foreach (var p in Process.GetProcessesByName(name))
                    {
                        try {
                            p.PriorityClass = ProcessPriorityClass.High;
                            SetProcessWorkingSetSize(p.Handle, -1, -1); // RAM LOCK

                            int coreCount = Environment.ProcessorCount;
                            if (coreCount > 1)
                            {
                                long affinityMask = 0;
                                for (int i = 1; i < coreCount; i++) affinityMask |= (1L << i);
                                p.ProcessorAffinity = (IntPtr)affinityMask; // Skip Core 0
                            }
                        } catch { }
                    }
                }
            }
            catch { }
        }

        // --- 4. APPLY ENGINE TWEAKS (GPU & Interrupts) ---
        public static void ApplyEngineTweaks()
        {
            try
            {
                // GPU Scheduling
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers"))
                {
                    if (key != null) key.SetValue("HwSchMode", 2, RegistryValueKind.DWord);
                }

                Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore").SetValue("GameDVR_Enabled", 0, RegistryValueKind.DWord);
                RunHiddenCommand("sc", "stop WSearch");
                
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"))
                {
                    if (key != null) key.SetValue("InterruptModeration", 0, RegistryValueKind.String);
                }
            }
            catch { }
        }

        // --- 5. CLEAN SYSTEM ---
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
                }
                RunHiddenCommand("cmd.exe", "/c ipconfig /flushdns");
            }
            catch { }
        }

        // --- 6. EMULATOR INJECTION ---
        public static void InjectEmulatorTweaks()
        {
            try
            {
                string[] targets = { "BlueStacks_nxt", "MSI_AppPlayer", "LDPlayer", "SmartGaGa" };
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                {
                    foreach (var target in targets)
                    {
                        string path = Path.Combine(drive.Name, "ProgramData", target, "bluestacks.conf");
                        if (File.Exists(path))
                        {
                            var lines = File.ReadAllLines(path).ToList();
                            bool changed = false;
                            for (int i = 0; i < lines.Count; i++)
                            {
                                if (lines[i].Contains("cfg_x_sensitivity")) { lines[i] = "bst.cfg_x_sensitivity=1.20"; changed = true; }
                                if (lines[i].Contains("cfg_y_sensitivity")) { lines[i] = "bst.cfg_y_sensitivity=6.80"; changed = true; }
                                if (lines[i].Contains("force_dedicated_gpu")) { lines[i] = "bst.force_dedicated_gpu=1"; changed = true; }
                            }
                            if (changed) File.WriteAllLines(path, lines);
                        }
                    }
                }
            }
            catch { }
        }

        // --- MASTER TRIGGER ---
        public static void RunAllOptimizations()
        {
            StabilizeFPS();
            ApplyEngineTweaks();
            OptimizeMouse();
            InjectEmulatorTweaks();
            CleanSystem();
        }

        // --- REVERT ALL SETTINGS ---
        public static void RevertAllSettings()
        {
            if (!hasBackup) return;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))
                {
                    if (key != null)
                    {
                        if (origXCurve != null) key.SetValue("SmoothMouseXCurve", origXCurve, RegistryValueKind.Binary);
                        if (origYCurve != null) key.SetValue("SmoothMouseYCurve", origYCurve, RegistryValueKind.Binary);
                    }
                }
                RunHiddenCommand("sc", "start WSearch");
            }
            catch { }
        }

        private static void RunHiddenCommand(string file, string args)
        {
            try { Process.Start(new ProcessStartInfo(file, args) { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = false }); } catch { }
        }
    }
}
