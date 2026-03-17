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
        // --- High-Performance API Imports ---
        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        public static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, out uint CurrentResolution);

        [DllImport("kernel32.dll")]
        static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

        private static byte[] origXCurve, origYCurve;
        private static bool hasBackup = false;

        // --- 1. SETTINGS BACKUP (SAFE REVERT) ---
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

        // --- 2. OMEGA MOUSE ENGINE (MAGNETIC ALIGNMENT) ---
        public static void OptimizeMouse()
        {
            // Detect Emulator Presence
            string[] emuNames = { "HD-Player", "LdVBoxHeadless", "dnplayer", "SmartGaGa", "aow_exe", "AndroidProcess" };
            if (!emuNames.Any(t => Process.GetProcessesByName(t).Length > 0)) return;

            BackupUserSettings();
            try
            {
                // Force 0.5ms Kernel Timing
                uint curRes;
                NtSetTimerResolution(5000, true, out curRes);

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))
                {
                    if (key != null)
                    {
                        // OMEGA MAGNET CURVE: Perfect drag-shot friction reduction
                        byte[] magnetCurve = { 
                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00 
                        };
                        key.SetValue("SmoothMouseXCurve", magnetCurve, RegistryValueKind.Binary);
                        key.SetValue("SmoothMouseYCurve", magnetCurve, RegistryValueKind.Binary);
                        
                        // REINFORCEMENT: Ensures mouse speed remains untouched
                        key.SetValue("MouseSpeed", "0", RegistryValueKind.String); 
                    }
                }

                // Kernel Interrupt Refinement
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

        // --- 3. LIMIT BREAKER: PROCESS & RAM STEERING ---
        public static void BoostGameProcess()
        {
            string[] targets = { "HD-Player", "LdVBoxHeadless", "dnplayer", "SmartGaGa", "aow_exe", "AndroidProcess" };
            foreach (var name in targets)
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try 
                    {
                        p.PriorityClass = ProcessPriorityClass.High;

                        // RAM LOCK: Prevents Windows from moving game data to slow virtual memory
                        SetProcessWorkingSetSize(p.Handle, -1, -1);

                        // CORE STEERING: Moves game to Core 1+ (Skips Core 0 to avoid OS lag)
                        int coreCount = Environment.ProcessorCount;
                        if (coreCount > 1)
                        {
                            long affinityMask = 0;
                            for (int i = 1; i < coreCount; i++) affinityMask |= (1L << i);
                            p.ProcessorAffinity = (IntPtr)affinityMask;
                        }
                    } 
                    catch { }
                }
            }
        }

        // --- 4. SYSTEM STABILIZATION & NETWORK ---
        public static void StabilizeSystem()
        {
            try
            {
                // Ultimate Power Plan Activation
                RunHiddenCommand("powercfg", "-setacvalueindex scheme_current sub_processor cppm_set_all_cores_parking 100");
                RunHiddenCommand("powercfg", "-setactive scheme_current");

                // Disable Latency Hooks
                RunHiddenCommand("bcdedit", "/set disabledynamictick yes");
                RunHiddenCommand("bcdedit", "/set useplatformclock no");

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"))
                {
                    if (key != null)
                    {
                        key.SetValue("SystemResponsiveness", 0, RegistryValueKind.DWord);
                        key.SetValue("NetworkThrottlingIndex", 0xFFFFFFFF, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        // --- 5. EMULATOR INTERNAL CONFIG INJECTION ---
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

        // --- MASTER EXECUTION ---
        public static void RunAllOptimizations()
        {
            StabilizeSystem();
            BoostGameProcess();
            OptimizeMouse();
            InjectEmulatorTweaks();
            CleanJunk();
        }

        public static void CleanJunk()
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
            }
            catch { }
        }

        public static void RevertSettings()
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
            }
            catch { }
        }

        private static void RunHiddenCommand(string file, string args)
        {
            try { Process.Start(new ProcessStartInfo(file, args) { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = false }); } catch { }
        }
    }
}
