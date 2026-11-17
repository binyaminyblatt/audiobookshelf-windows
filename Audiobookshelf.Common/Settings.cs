using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Audiobookshelf.Common
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Settings
    {
        [JsonProperty]
        public string ServerPort { get; set; } = "13378";

        [JsonProperty]
        public string ServerHost { get; set; } = "0.0.0.0";

        [JsonProperty]
        public bool OpenBrowserOnStartup { get; set; } = false;

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

        // Restart the server automatically if it exits unexpectedly (with exponential backoff)
        [JsonProperty]
        public bool AutoRestartOnCrash { get; set; } = true;

        // Give up after this many crashes in a row without a stable run (0 = never give up)
        [JsonProperty]
        public int MaxConsecutiveRestarts { get; set; } = 5;

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

        // Admin API Key required for self updates and backups.
        // Plain text in memory only; it is written to config.json encrypted as "AdminApiKeyProtected".
        public string AdminApiKey { get; set; } = string.Empty;

        private bool _legacyAdminApiKeySeen;

        [JsonProperty("AdminApiKeyProtected")]
        private string AdminApiKeyProtected
        {
            get => SecretProtector.Protect(AdminApiKey);
            set => AdminApiKey = SecretProtector.Unprotect(value);
        }

        // Reads a plain-text "AdminApiKey" from older config.json files. Never written back.
        [JsonProperty("AdminApiKey", NullValueHandling = NullValueHandling.Ignore)]
        private string LegacyAdminApiKey
        {
            get => null;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    AdminApiKey = value;
                    _legacyAdminApiKeySeen = true;
                }
            }
        }

        /// <summary>
        /// True if this config was loaded from a file that still held plain-text secrets.
        /// Saving the settings rewrites them encrypted.
        /// </summary>
        public bool HasLegacyPlaintextSecrets =>
            _legacyAdminApiKeySeen ||
            (DriveMaps != null && DriveMaps.Any(d => d != null && d.NeedsSecretMigration));

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
