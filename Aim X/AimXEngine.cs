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

        // --- 2. MOUSE OPTIMIZATION (THE MAGNET + DEEP SYSTEM TWEAKS) ---
        public static void OptimizeMouse()
        {
            string[] emuNames = { "HD-Player", "LdVBoxHeadless", "dnplayer", "SmartGaGa", "aow_exe", "AndroidProcess" };
            if (!emuNames.Any(t => Process.GetProcessesByName(t).Length > 0)) return;

            BackupUserSettings();
            try
            {
                // Force 0.5ms System Timer for instant click registration
                uint curRes;
                NtSetTimerResolution(5000, true, out curRes); 

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))
                {
                    if (key != null)
                    {
                        // THE MAGNET CURVE: Removes pixel-skipping for smooth vertical drag-shots
                        byte[] magnetCurve = { 
                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00 
                        };
                        key.SetValue("SmoothMouseXCurve", magnetCurve, RegistryValueKind.Binary);
                        key.SetValue("SmoothMouseYCurve", magnetCurve, RegistryValueKind.Binary);
                        
                        // Disable Windows Acceleration (Keeps User Sensitivity Slider same)
                        key.SetValue("MouseSpeed", "0", RegistryValueKind.String);
                        key.SetValue("MouseThreshold1", "0", RegistryValueKind.String);
                        key.SetValue("MouseThreshold2", "0", RegistryValueKind.String);
                    }
                }

                // HIGH-PRIORITY USB INPUT QUEUE
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Mouclass\Parameters"))
                {
                    if (key != null) 
                    {
                        key.SetValue("MouseDataQueueSize", 20, RegistryValueKind.DWord);
                        key.SetValue("ThreadPriority", 31, RegistryValueKind.DWord);
                    }
                }

                // NEW: CSRSS PRIORITY BOOT (Makes the mouse cursor draw faster on screen)
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\PriorityControl"))
                {
                    if (key != null) key.SetValue("Win32PrioritySeparation", 38, RegistryValueKind.DWord);
                }

                // DISABLE USB POWER SAVING (Instant wakeup for all USB HID devices)
                using (RegistryKey usbKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB", true))
                {
                    if (usbKey != null)
                    {
                        foreach (string sub in usbKey.GetSubKeyNames())
                        {
                            try {
                                using (RegistryKey devParam = usbKey.OpenSubKey(sub + @"\Device Parameters", true))
                                {
                                    if (devParam != null) devParam.SetValue("EnhancedPowerManagementEnabled", 0, RegistryValueKind.DWord);
                                }
                            } catch { }
                        }
                    }
                }
            }
            catch { }
        }

        // --- 3. STABILIZE FPS ---
        public static void StabilizeFPS()
        {
            try
            {
                RunHiddenCommand("powercfg", "-setacvalueindex scheme_current sub_processor cppm_set_all_cores_parking 100");
                RunHiddenCommand("powercfg", "-setactive scheme_current");
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

                string[] targets = { "HD-Player", "LdVBoxHeadless", "dnplayer", "SmartGaGa", "aow_exe", "AndroidProcess" };
                foreach (var name in targets)
                {
                    foreach (var p in Process.GetProcessesByName(name))
                    {
                        try {
                            p.PriorityClass = ProcessPriorityClass.High;
                            SetProcessWorkingSetSize(p.Handle, -1, -1); 

                            int coreCount = Environment.ProcessorCount;
                            if (coreCount > 1)
                            {
                                long affinityMask = 0;
                                for (int i = 1; i < coreCount; i++) affinityMask |= (1L << i);
                                p.ProcessorAffinity = (IntPtr)affinityMask;
                            }
                        } catch { }
                    }
                }
            }
            catch { }
        }

        // --- 4. ENGINE TWEAKS (NOW WITH DPI SCALING FIX) ---
        public static void ApplyEngineTweaks()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers"))
                {
                    if (key != null) key.SetValue("HwSchMode", 2, RegistryValueKind.DWord);
                }

                // NEW: Disable High DPI scaling for smoother mouse movement
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop"))
                {
                    if (key != null) key.SetValue("Win8DpiScaling", 0, RegistryValueKind.DWord);
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

        public static void RunAllOptimizations()
        {
            StabilizeFPS();
            ApplyEngineTweaks();
            OptimizeMouse();
            InjectEmulatorTweaks();
            CleanSystem();
        }

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
                        key.SetValue("MouseSpeed", "1", RegistryValueKind.String); 
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
