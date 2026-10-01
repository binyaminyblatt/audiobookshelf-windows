using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Audiobookshelf.Common
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Settings
    {
        [JsonProperty]
        public string ServerPort { get; set; } = "13378";

        [JsonProperty]
        public string DataDir { get; set; } = string.Empty;

        [JsonProperty]
        public List<DriveMap> DriveMaps { get; set; } = new List<DriveMap>();

        [JsonProperty]
        public bool AutoRemount { get; set; } = true;

        [JsonProperty]
        public int AutoRemountCount { get; set; } = 5;

        [JsonProperty]
        public int AutoRemountDelay { get; set; } = 5;

        [JsonProperty]
        public bool StartServerOnMountFail { get; set; } = true;

        // GitHub automated updater at midnight (MUST be OFF by default)
        [JsonProperty]
        public bool AutoUpdateFromGitHub { get; set; } = false;

        // Tray UI automatic update notifications (defaults to false)
        [JsonProperty]
        public bool AutoCheckForUpdates { get; set; } = false;

        // Automatically apply audiobookshelf.exe dropped in updates folder at midnight
        [JsonProperty]
        public bool AutoApplyFolderUpdates { get; set; } = true;

        // Hour of the day for midnight updates (0 = 00:00 midnight)
        [JsonProperty]
        public int UpdateHour { get; set; } = 0;

        [JsonProperty]
        public string GitHubRepoOwner { get; set; } = "binyaminyblatt";

        [JsonProperty]
        public string GitHubRepoName { get; set; } = "audiobookshelf-windows";

        // Admin API Key required for self updates and backups
        [JsonProperty]
        public string AdminApiKey { get; set; } = string.Empty;

        [JsonProperty]
        public bool StartAtLogin { get; set; } = true;

        [JsonProperty]
        public string AppVersion { get; set; } = string.Empty;

        // Custom environment variables passed to the audiobookshelf server process
        private Dictionary<string, string> _envs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty("envs")]
        public Dictionary<string, string> Envs
        {
            get => _envs ?? (_envs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            set => _envs = value != null ? new Dictionary<string, string>(value, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public static string GetDefaultDataDir()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Audiobookshelf"
            );
        }

        public static string GetDefaultUpdatesDir()
        {
            return Path.Combine(GetDefaultDataDir(), "updates");
        }
    }
}
