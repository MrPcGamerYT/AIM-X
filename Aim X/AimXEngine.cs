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

        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        public static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, out uint CurrentResolution);

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
                        origXCurve = (byte[])key.GetValue("SmoothMouseXCurve");
                        origYCurve = (byte[])key.GetValue("SmoothMouseYCurve");
                        hasBackup = true;
                    }
                }
            }
            catch { }
        }

        // --- 1. MOUSE OPTIMIZATION (STRICT: NO SENSITIVITY CHANGES) ---
        public static void OptimizeMouse()
        {
            BackupUserSettings();
            try
            {
                // Set Timer Resolution to 0.5ms for lowest input delay
                uint curRes;
                NtSetTimerResolution(5000, true, out curRes);

                // --- 🛡️ SENSITIVITY PROTECTION: SystemParametersInfo CALL REMOVED 🛡️ ---
                
                // Boost Win32 Priority for Foreground Input
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\PriorityControl"))
                {
                    if (key != null) key.SetValue("Win32PrioritySeparation", 38, RegistryValueKind.DWord);
                }

                // Optimize Mouse Interrupt Queue (HID Polling stability)
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Mouclass\Parameters"))
                {
                    if (key != null) 
                    {
                        key.SetValue("MouseDataQueueSize", 25, RegistryValueKind.DWord);
                        key.SetValue("ThreadPriority", 31, RegistryValueKind.DWord); 
                    }
                }

                // Disable USB Power Saving to stop mouse stuttering
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB", true))
                {
                    if (key != null)
                    {
                        foreach (string subkeyName in key.GetSubKeyNames())
                        {
                            try {
                                using (RegistryKey subkey = key.OpenSubKey(subkeyName + @"\Device Parameters", true))
                                {
                                    if (subkey != null) 
                                    {
                                        subkey.SetValue("EnhancedPowerManagementEnabled", 0, RegistryValueKind.DWord);
                                        subkey.SetValue("SelectiveSuspendEnabled", 0, RegistryValueKind.DWord);
                                    }
                                }
                            } catch { }
                        }
                    }
                }

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))
                {
                    if (key != null)
                    {
                        // --- 🛡️ SENSITIVITY PROTECTION: Registry Speed/Threshold keys REMOVED 🛡️ ---
                        
                        key.SetValue("MouseHoverTime", "8", RegistryValueKind.String);

                        // APPLY MAGNET CURVE (Sharper precision, NO SPEED CHANGE)
                        byte[] magnetCurve = { 
                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 
                            0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00 
                        };
                        key.SetValue("SmoothMouseXCurve", magnetCurve, RegistryValueKind.Binary);
                        key.SetValue("SmoothMouseYCurve", magnetCurve, RegistryValueKind.Binary);
                    }
                }

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\PrecisionTouchPad"))
                {
                    if (key != null) key.SetValue("CurvatureSetting", 0, RegistryValueKind.DWord);
                }
            }
            catch { }
        }

        // --- 2. CPU & FPS STABILIZER (Core Unparking) ---
        public static void StabilizeFPS()
        {
            try
            {
                RunHiddenCommand("powercfg", "-setacvalueindex scheme_current sub_processor cppm_set_all_cores_parking 100");
                RunHiddenCommand("powercfg", "-setactive scheme_current");
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

        // --- 3. LOW LATENCY ENGINE TWEAKS ---
        public static void ApplyEngineTweaks()
        {
            try
            {
                Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore").SetValue("GameDVR_Enabled", 0, RegistryValueKind.DWord);
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers"))
                {
                    if (key != null) key.SetValue("HwSchMode", 2, RegistryValueKind.DWord);
                }

                RunHiddenCommand("sc", "stop WSearch");

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"))
                {
                    if (key != null) key.SetValue("InterruptModeration", 0, RegistryValueKind.String);
                }
            }
            catch { }
        }

        // --- 4. EMULATOR INJECTION ---
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

        public static void RunAllOptimizations()
        {
            OptimizeMouse();
            StabilizeFPS();
            ApplyEngineTweaks();
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
                        else key.DeleteValue("SmoothMouseXCurve", false);

                        if (origYCurve != null) key.SetValue("SmoothMouseYCurve", origYCurve, RegistryValueKind.Binary);
                        else key.DeleteValue("SmoothMouseYCurve", false);
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
