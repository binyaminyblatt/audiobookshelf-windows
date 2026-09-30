using System;
using System.IO;
using Microsoft.Win32;
using Newtonsoft.Json;
using NLog;

namespace Audiobookshelf.Common
{
    public static class SettingsHandler
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly object _fileLock = new object();

        private const string LEGACY_REGISTRY_KEY = @"HKEY_CURRENT_USER\Software\Audiobookshelf";

        public static string SettingsFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Audiobookshelf",
            "config.json"
        );

        public static Settings Load()
        {
            lock (_fileLock)
            {
                try
                {
                    string path = SettingsFilePath;
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        var settings = JsonConvert.DeserializeObject<Settings>(json);
                        if (settings != null)
                        {
                            if (string.IsNullOrWhiteSpace(settings.DataDir))
                            {
                                settings.DataDir = Settings.GetDefaultDataDir();
                            }
                            return settings;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to load settings from {SettingsFilePath}: {ex}");
                }

                // If no settings file exists, migrate legacy settings or generate defaults
                var defaultSettings = new Settings();
                defaultSettings.DataDir = Settings.GetDefaultDataDir();

                MigrateLegacySettings(defaultSettings);
                Save(defaultSettings);

                return defaultSettings;
            }
        }

        public static void Save(Settings settings)
        {
            if (settings == null) return;

            lock (_fileLock)
            {
                try
                {
                    string path = SettingsFilePath;
                    string dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                    File.WriteAllText(path, json);
                    _logger.Debug("Saved configuration settings successfully.");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to save settings to {SettingsFilePath}: {ex}");
                }
            }
        }

        private static void MigrateLegacySettings(Settings settings)
        {
            try
            {
                var legacyPort = Registry.GetValue(LEGACY_REGISTRY_KEY, "ServerPort", null) as string;
                if (!string.IsNullOrEmpty(legacyPort))
                {
                    settings.ServerPort = legacyPort;
                }

                var legacyDataDir = Registry.GetValue(LEGACY_REGISTRY_KEY, "DataDir", null) as string;
                if (!string.IsNullOrEmpty(legacyDataDir) && Directory.Exists(legacyDataDir))
                {
                    settings.DataDir = legacyDataDir;
                }

                var legacyAutoUpdate = Registry.GetValue(LEGACY_REGISTRY_KEY, "AutoCheckForUpdates", null);
                if (legacyAutoUpdate is int intAutoUpdate)
                {
                    settings.AutoCheckForUpdates = intAutoUpdate != 0;
                }

                var legacyStartAtLogin = Registry.GetValue(LEGACY_REGISTRY_KEY, "StartAtLogin", null);
                if (legacyStartAtLogin is int intStartAtLogin)
                {
                    settings.StartAtLogin = intStartAtLogin != 0;
                }

                var legacyAppVersion = Registry.GetValue(LEGACY_REGISTRY_KEY, "AppVersion", null) as string;
                if (!string.IsNullOrEmpty(legacyAppVersion))
                {
                    settings.AppVersion = legacyAppVersion;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Legacy settings migration skipped: {ex.Message}");
            }
        }
    }
}
