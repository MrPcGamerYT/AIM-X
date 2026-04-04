using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Security.Principal;

namespace Aim_X
{
    public static class AimXEngine
    {
        // --- Native & Kernel Imports ---
        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        public static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, out uint CurrentResolution);

        [DllImport("kernel32.dll")]
        static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        [DllImport("psapi.dll")]
        static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("ntdll.dll")]
        public static extern int NtSetSystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength);

        private const uint SPI_SETMOUSE = 0x0004;
        private const uint SPIF_UPDATEINIFILE = 0x01;
        private const uint SPIF_SENDCHANGE = 0x02;

        private static string backupFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AimX_Backup.dat");

        private static bool IsAdmin() => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        // --- 1. BACKUP SYSTEM ---
        private static void BackupUserSettings()
        {
            if (File.Exists(backupFile)) return;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", false))
                {
                    if (key != null)
                    {
                        byte[] x = (byte[])key.GetValue("SmoothMouseXCurve");
                        byte[] y = (byte[])key.GetValue("SmoothMouseYCurve");
                        string speed = key.GetValue("MouseSpeed").ToString();

                        using (BinaryWriter writer = new BinaryWriter(File.Open(backupFile, FileMode.Create)))
                        {
                            writer.Write(x.Length);
                            writer.Write(x);
                            writer.Write(y.Length);
                            writer.Write(y);
                            writer.Write(speed);
                        }
                    }
                }
            }
            catch { }
        }

        // --- 2. MOUSE OPTIMIZATION (ULTRA PRO FIX) ---
        public static void OptimizeMouse()
        {
            BackupUserSettings();
            try
            {
                uint curRes;
                NtSetTimerResolution(5000, true, out curRes);

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))
                {
                    if (key != null)
                    {
                        byte[] zeroCurve = new byte[40]; 
                        key.SetValue("SmoothMouseXCurve", zeroCurve, RegistryValueKind.Binary);
                        key.SetValue("SmoothMouseYCurve", zeroCurve, RegistryValueKind.Binary);
                        key.SetValue("MouseSpeed", "0", RegistryValueKind.String);
                        key.SetValue("MouseThreshold1", "0", RegistryValueKind.String);
                        key.SetValue("MouseThreshold2", "0", RegistryValueKind.String);
                    }
                }

                SystemParametersInfo(SPI_SETMOUSE, 0, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

                if (IsAdmin())
                {
                    using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Mouclass\Parameters"))
                    {
                        key?.SetValue("MouseDataQueueSize", 16, RegistryValueKind.DWord);
                        key?.SetValue("ThreadPriority", 31, RegistryValueKind.DWord);
                    }
                    
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                    {
                        key?.SetValue("UserPreferencesMask", new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary);
                    }
                }
            }
            catch { }
        }

        // --- 3. STABILIZE FPS & NETWORK ---
        public static void StabilizeFPS()
        {
            try
            {
                RunHiddenCommand("powercfg", "-setacvalueindex scheme_current sub_processor cppm_set_all_cores_parking 100");
                RunHiddenCommand("powercfg", "-setactive scheme_current");
                RunHiddenCommand("bcdedit", "/set disabledynamictick yes");
                RunHiddenCommand("bcdedit", "/set useplatformclock no");

                if (IsAdmin())
                {
                    using (RegistryKey interfaces = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", true))
                    {
                        if (interfaces != null)
                        {
                            foreach (string id in interfaces.GetSubKeyNames())
                            {
                                using (RegistryKey card = interfaces.OpenSubKey(id, true))
                                {
                                    card?.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                                    card?.SetValue("TCPNoDelay", 1, RegistryValueKind.DWord);
                                }
                            }
                        }
                    }

                    using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"))
                    {
                        key?.SetValue("SystemResponsiveness", 0, RegistryValueKind.DWord);
                        key?.SetValue("NetworkThrottlingIndex", 0xFFFFFFFF, RegistryValueKind.DWord);
                    }
                }

                string[] targets = { "HD-Player", "LdVBoxHeadless", "dnplayer", "SmartGaGa", "aow_exe", "AndroidProcess" };
                int coreCount = Environment.ProcessorCount;

                foreach (var name in targets)
                {
                    foreach (var p in Process.GetProcessesByName(name))
                    {
                        try
                        {
                            p.PriorityClass = ProcessPriorityClass.High;
                            if (coreCount >= 6)
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
            catch { }
        }

        // --- 4. ENGINE TWEAKS ---
        public static void ApplyEngineTweaks()
        {
            try
            {
                if (IsAdmin())
                {
                    using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\PriorityControl"))
                        key?.SetValue("Win32PrioritySeparation", 38, RegistryValueKind.DWord);

                    using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers"))
                        key?.SetValue("HwSchMode", 2, RegistryValueKind.DWord);

                    string[] emuExes = { "HD-Player.exe", "LdVBoxHeadless.exe", "dnplayer.exe" };
                    foreach (var exe in emuExes)
                    {
                        using (RegistryKey key = Registry.LocalMachine.CreateSubKey($@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\{exe}\PerfOptions"))
                        {
                            key?.SetValue("CpuPriorityClass", 3, RegistryValueKind.DWord);
                            key?.SetValue("IoPriority", 3, RegistryValueKind.DWord);
                        }
                    }

                    RunHiddenCommand("sc", "stop WSearch");
                    RunHiddenCommand("sc", "stop SysMain");
                }

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop"))
                    key?.SetValue("Win8DpiScaling", 0, RegistryValueKind.DWord);

                Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore").SetValue("GameDVR_Enabled", 0, RegistryValueKind.DWord);
            }
            catch { }
        }

        // --- 5. CLEANER ---
        public static void CleanSystem()
        {
            try
            {
                IntPtr cacheSize = new IntPtr(-1);
                NtSetSystemInformation(0x0015, cacheSize, Marshal.SizeOf(cacheSize));

                string[] folders = { Path.GetTempPath(), "C:\\Windows\\Temp", "C:\\Windows\\Prefetch" };
                foreach (var folder in folders.Where(Directory.Exists))
                {
                    DirectoryInfo di = new DirectoryInfo(folder);
                    foreach (FileInfo file in di.GetFiles()) { try { file.Delete(); } catch { } }
                }

                foreach (Process p in Process.GetProcesses())
                {
                    try { EmptyWorkingSet(p.Handle); } catch { }
                }
                RunHiddenCommand("ipconfig", "/flushdns");
            }
            catch { }
        }

        // --- 6. EMULATOR INJECTOR ---
        public static void InjectEmulatorTweaks()
        {
            try
            {
                string[] emus = { "BlueStacks_nxt", "MSI_AppPlayer", "LDPlayer", "SmartGaGa" };
                Dictionary<string, string> settings = new Dictionary<string, string>
                {
                    { "bst.cfg_x_sensitivity", "1.20" },
                    { "bst.cfg_y_sensitivity", "6.80" },
                    { "bst.force_dedicated_gpu", "1" },
                    { "bst.enable_high_fps", "1" }
                };

                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                {
                    foreach (var emu in emus)
                    {
                        string path = Path.Combine(drive.Name, "ProgramData", emu, "bluestacks.conf");
                        if (File.Exists(path)) UpdateConfSafe(path, settings);
                    }
                }
            }
            catch { }
        }

        private static void UpdateConfSafe(string path, Dictionary<string, string> settings)
        {
            var lines = File.ReadAllLines(path).ToList();
            foreach (var setting in settings)
            {
                bool found = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Contains(setting.Key))
                    {
                        lines[i] = $"{setting.Key}=\"{setting.Value}\"";
                        found = true;
                        break;
                    }
                }
                if (!found) lines.Add($"{setting.Key}=\"{setting.Value}\"");
            }
            File.WriteAllLines(path, lines);
        }

        // --- MASTER FUNCTION (WITH AUTO-CLEANUP ON CLOSE) ---
        public static void RunAllOptimizations()
        {
            // Subscribe to Process Exit event automatically
            AppDomain.CurrentDomain.ProcessExit += (s, e) => RevertAllSettings();
            
            OptimizeMouse();
            StabilizeFPS();
            ApplyEngineTweaks();
            InjectEmulatorTweaks();
            CleanSystem();
        }

        // --- 7. REVERT ---
        public static void RevertAllSettings()
        {
            if (!File.Exists(backupFile)) return;
            try
            {
                byte[] x, y;
                string speed;

                using (BinaryReader reader = new BinaryReader(File.Open(backupFile, FileMode.Open)))
                {
                    x = reader.ReadBytes(reader.ReadInt32());
                    y = reader.ReadBytes(reader.ReadInt32());
                    speed = reader.ReadString();
                }

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))
                {
                    if (key != null)
                    {
                        key.SetValue("SmoothMouseXCurve", x, RegistryValueKind.Binary);
                        key.SetValue("SmoothMouseYCurve", y, RegistryValueKind.Binary);
                        key.SetValue("MouseSpeed", speed, RegistryValueKind.String);
                    }
                }

                SystemParametersInfo(SPI_SETMOUSE, 0, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

                if (IsAdmin())
                {
                    RunHiddenCommand("sc", "start WSearch");
                    RunHiddenCommand("sc", "start SysMain");
                    
                    // Remove High Performance overrides for Emulators
                    string[] emuExes = { "HD-Player.exe", "LdVBoxHeadless.exe", "dnplayer.exe" };
                    foreach (var exe in emuExes)
                    {
                        Registry.LocalMachine.DeleteSubKeyTree($@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\{exe}\PerfOptions", false);
                    }
                }

                File.Delete(backupFile);
            }
            catch { }
        }

        private static void RunHiddenCommand(string file, string args)
        {
            try { Process.Start(new ProcessStartInfo(file, args) { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = false }); } catch { }
        }
    }
}
