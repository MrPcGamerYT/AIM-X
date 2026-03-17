using System;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Net.Cache;

namespace Aim_X // Updated to match your current namespace
{
    public static class Updater
    {
        // Points to your new Aim-X repository
        private const string UpdateInfoUrl =
            "https://raw.githubusercontent.com/MrPcGamerYT/Aim-X/main/update.json";

        public static void CheckAndUpdate()
        {
            try
            {
                // Force secure connection protocols
                ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls12 |
                    SecurityProtocolType.Tls11;

                using (WebClient wc = new WebClient())
                {
                    // Prevent Windows from caching the JSON so you always get the latest version info
                    wc.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
                    wc.Headers.Add("Cache-Control", "no-cache");

                    string json = wc.DownloadString(UpdateInfoUrl);

                    // Using your Regex extraction logic
                    string latestVersion = ExtractValue(json, "version");
                    string installerUrl = ExtractValue(json, "url");

                    // Gets the version from your Project Properties -> Assembly Info
                    string currentVersion = Application.ProductVersion;

                    // If latest is NOT higher than current, stop here
                    if (CompareVersions(latestVersion, currentVersion) <= 0)
                        return;

                    DialogResult result = MessageBox.Show(
                        $"New version {latestVersion} is available!\n\nYour version: {currentVersion}\n\nWould you like to download the update now?",
                        "Aim-X Update",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (result != DialogResult.Yes)
                        return;

                    // Download to Temp folder
                    string installerPath = Path.Combine(
                        Path.GetTempPath(),
                        "AimX_Setup.exe");

                    if (File.Exists(installerPath))
                        File.Delete(installerPath);

                    wc.DownloadFile(installerUrl, installerPath);

                    // Launch installer as Administrator
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = installerPath,
                        UseShellExecute = true,
                        Verb = "runas"
                    });

                    // Kill the current app so the installer can replace it
                    Environment.Exit(0);
                }
            }
            catch (WebException)
            {
                // Silent fail if no internet
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Updater Error:\n" + ex.Message,
                    "Aim-X Updater",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static string ExtractValue(string json, string key)
        {
            Match match = Regex.Match(
                json,
                $"\"{key}\"\\s*:\\s*\"([^\"]+)\"",
                RegexOptions.IgnoreCase);

            if (!match.Success)
                throw new Exception("Invalid update.json format on GitHub");

            return match.Groups[1].Value;
        }

        private static int CompareVersions(string latest, string current)
        {
            // Converts string "1.0.0.0" to Version object for proper math comparison
            Version vLatest = new Version(latest);
            Version vCurrent = new Version(current);

            return vLatest.CompareTo(vCurrent);
        }
    }
}