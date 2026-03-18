using System;
using System.Diagnostics;
using System.Linq;
using DiscordRPC;
using DiscordRPC.Logging;

namespace Aim_X
{
    public class DiscordManager
    {
        public DiscordRpcClient client;
        private Timestamps _elapsedTime;

        // Your Application ID (linked successfully)
        private const string ApplicationId = "1380037908685262878";

        public void Initialize()
        {
            client = new DiscordRpcClient(ApplicationId);
            client.Logger = new ConsoleLogger() { Level = LogLevel.Error };

            // Start the "Time Elapsed" clock
            _elapsedTime = Timestamps.Now;

            client.OnReady += (sender, e) =>
            {
                Console.WriteLine($"[Discord] Aim X v21 Connected to: {e.User.Username}");
            };

            client.Initialize();
            CheckForEmulators(); // Run initial check immediately
        }

        public void CheckForEmulators()
        {
            if (client == null || !client.IsInitialized) return;

            // List of emulator processes to detect
            var emulators = new (string ProcessName, string CleanName)[]
            {
                ("HD-Player", "BlueStacks 5"),
                ("MSIAppPlayer", "MSI App Player"),
                ("LdVBoxHeadless", "LDPlayer"),
                ("dnplayer", "LDPlayer"),
                ("SmartGaGa", "SmartGaGa"),
                ("aow_exe", "GameLoop"),
                ("AndroidProcess", "Phoenix OS")
            };

            // Detect if any emulator is running
            var runningEmu = emulators.FirstOrDefault(e => Process.GetProcessesByName(e.ProcessName).Any());

            if (runningEmu.ProcessName != null)
            {
                // Real Status when playing
                UpdateStatus("Aim X Engine v1.1.2", $"Boosting: {runningEmu.CleanName}");
            }
            else
            {
                // Real Status when idle
                UpdateStatus("Aim X Engine v1.1.2", "Status: Idle (Ready)");
            }
        }

        public void UpdateStatus(string detail, string state)
        {
            if (client == null || !client.IsInitialized) return;

            client.SetPresence(new RichPresence()
            {
                Details = detail, // Line 1: Aim X Engine v1.1.2
                State = state,   // Line 2: The Action
                Timestamps = _elapsedTime,
                Assets = new Assets()
                {
                    LargeImageKey = "logo_main",
                    LargeImageText = "Aim X Engine v1.1.2",
                    SmallImageKey = "verified",
                    SmallImageText = "Verified Optimizer"
                },
                Buttons = new DiscordRPC.Button[]
                {
                    // Buttons with your real links
                    new DiscordRPC.Button() { Label = "Get Aim X", Url = "https://github.com/MrPcGamerYT/AIM-X" },
                    new DiscordRPC.Button() { Label = "YouTube Channel", Url = "https://youtube.com/@MR.PC_GAMER_YT" }
                }
            });
        }

        public void Deinitialize()
        {
            if (client != null)
            {
                // Clean up before closing
                client.ClearPresence();
                client.Dispose();
            }
        }
    }
}