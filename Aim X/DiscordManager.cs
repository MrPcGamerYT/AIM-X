using System;
using DiscordRPC;
using DiscordRPC.Logging;

namespace Aim_X
{
    public class DiscordManager
    {
        public DiscordRpcClient client;
        private Timestamps _elapsedTime;

        // Your Application ID
        private const string ApplicationId = "1380037908685262878";

        public void Initialize()
        {
            client = new DiscordRpcClient(ApplicationId);
            client.Logger = new ConsoleLogger() { Level = LogLevel.Error };

            _elapsedTime = Timestamps.Now;

            client.OnReady += (sender, e) =>
            {
                Console.WriteLine($"[Discord] Aim X Optimizer Live: {e.User.Username}");
            };

            client.Initialize();
            
            // Default Professional Aim Status
            SetAimStatus("Engine: Ready", "Aim: Stabilized");
        }

        /// <summary>
        /// Updates the Discord status with real-time optimization work.
        /// </summary>
        /// <param name="workStatus">What the tool is doing (e.g., "Optimizing Mouse", "Injecting Config")</param>
        /// <param name="aimStatus">The result (e.g., "Aim: Precision Mode", "Lag: Ultra Low")</param>
        public void SetAimStatus(string workStatus, string aimStatus)
        {
            if (client == null || !client.IsInitialized) return;

            client.SetPresence(new RichPresence()
            {
                // Line 1: The specific work the tool is performing
                Details = workStatus, 
                
                // Line 2: The pro-level aim optimization result
                State = $"{aimStatus} | Input Lag: 0.1ms",   
                
                Timestamps = _elapsedTime,
                Assets = new Assets()
                {
                    LargeImageKey = "logo_main",
                    LargeImageText = "Aim X | Ultimate Aim Optimizer",
                    SmallImageKey = "verified",
                    SmallImageText = "Aim Engine Verified"
                },
                Buttons = new DiscordRPC.Button[]
                {
                    new DiscordRPC.Button() { Label = "Get Aim X", Url = "https://github.com/MrPcGamerYT/AIM-X" },
                    new DiscordRPC.Button() { Label = "YouTube Channel", Url = "https://youtube.com/@MR.PC_GAMER_YT" }
                }
            });
        }

        public void Deinitialize()
        {
            if (client != null)
            {
                client.ClearPresence();
                client.Dispose();
            }
        }
    }
}
