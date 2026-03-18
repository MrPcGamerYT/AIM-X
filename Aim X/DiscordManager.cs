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
                Console.WriteLine($"[Discord] Aim X Connected to: {e.User.Username}");
            };

            client.Initialize();
        }


        public void UpdateStatus(string detail, string state)
        {
            if (client == null || !client.IsInitialized) return;

            client.SetPresence(new RichPresence()
            {
                Details = detail, // Line 1: 🎯 Aim X: High-performance system optimizer and gaming engine for low-end PCs.
                State = state,   // Line 2: The Action
                Timestamps = _elapsedTime,
                Assets = new Assets()
                {
                    LargeImageKey = "logo_main",
                    LargeImageText = "🎯 Aim X: High-performance system optimizer and gaming engine for low-end PCs.",
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
