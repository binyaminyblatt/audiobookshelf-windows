using System;
using System.Diagnostics;
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

        private const string HKLM_REGISTRY_KEY = @"HKEY_LOCAL_MACHINE\Software\Audiobookshelf";
        private const string LEGACY_REGISTRY_KEY = @"HKEY_CURRENT_USER\Software\Audiobookshelf";

        public static string SettingsFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Audiobookshelf",
            "config.json"
        );

        public static string GetInstalledVersion()
        {
            // 1. Check Registry keys written by Inno Setup / Installer
            string[] registryKeys = new[]
            {
                @"HKEY_LOCAL_MACHINE\Software\Audiobookshelf",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Audiobookshelf",
                @"HKEY_CURRENT_USER\Software\Audiobookshelf",
                @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Uninstall\{398D8732-4648-4C71-A30C-C0688D46BB13}_is1",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{398D8732-4648-4C71-A30C-C0688D46BB13}_is1",
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\{398D8732-4648-4C71-A30C-C0688D46BB13}_is1"
            };

            foreach (var key in registryKeys)
            {
                try
                {
                    var val = (Registry.GetValue(key, "AppVersion", null) ?? 
                               Registry.GetValue(key, "DisplayVersion", null)) as string;
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        return val.Trim();
                    }
                }
                catch { }
            }

            // 2. Check unins000.exe ProductVersion in installation directories
            string[] probeDirs = new[]
            {
                AppDomain.CurrentDomain.BaseDirectory,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Audiobookshelf"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Audiobookshelf")
            };

            foreach (var dir in probeDirs)
            {
                try
                {
                    string uninsPath = Path.Combine(dir, "unins000.exe");
                    if (File.Exists(uninsPath))
                    {
                        var vi = FileVersionInfo.GetVersionInfo(uninsPath);
                        if (!string.IsNullOrWhiteSpace(vi.ProductVersion))
                        {
                            return vi.ProductVersion.Trim();
                        }
                    }
                }
                catch { }
            }

            return string.Empty;
        }

        public static bool IsNewerVersion(string latestVersionStr, string currentVersionStr)
        {
            if (string.IsNullOrWhiteSpace(latestVersionStr)) return false;
            if (string.IsNullOrWhiteSpace(currentVersionStr)) return true;

            string cleanLatest = latestVersionStr.Trim().TrimStart('v', 'V');
            string cleanCurrent = currentVersionStr.Trim().TrimStart('v', 'V');

            if (string.Equals(cleanLatest, cleanCurrent, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (Version.TryParse(cleanLatest, out var latestVer) && Version.TryParse(cleanCurrent, out var currentVer))
            {
                return latestVer > currentVer;
            }

            // Fallback: compare numerical components
            var latestParts = cleanLatest.Split('.', '-', '+');
            var currentParts = cleanCurrent.Split('.', '-', '+');
            int count = Math.Min(latestParts.Length, currentParts.Length);
            for (int i = 0; i < count; i++)
            {
                if (int.TryParse(latestParts[i], out int lNum) && int.TryParse(currentParts[i], out int cNum))
                {
                    if (lNum != cNum) return lNum > cNum;
                }
                else
                {
                    int cmp = string.Compare(latestParts[i], currentParts[i], StringComparison.OrdinalIgnoreCase);
                    if (cmp != 0) return cmp > 0;
                }
            }

            return latestParts.Length > currentParts.Length;
        }

        public static Settings Load()
        {
            lock (_fileLock)
            {
                Settings settings = null;
                try
                {
                    string path = SettingsFilePath;
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        settings = JsonConvert.DeserializeObject<Settings>(json);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to load settings from {SettingsFilePath}: {ex}");
                }

                if (settings == null)
                {
                    settings = new Settings();
                    MigrateLegacySettings(settings);
                }

                if (string.IsNullOrWhiteSpace(settings.DataDir))
                {
                    settings.DataDir = Settings.GetDefaultDataDir();
                }

                // Always synchronize AppVersion with the latest installed version from Registry/Uninstaller
                string installedVersion = GetInstalledVersion();
                if (!string.IsNullOrWhiteSpace(installedVersion) && !string.Equals(settings.AppVersion, installedVersion, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Info($"Updating configured AppVersion from '{settings.AppVersion}' to installed version '{installedVersion}'.");
                    settings.AppVersion = installedVersion;
                    Save(settings);
                }

                return settings;
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
                var legacyPort = (Registry.GetValue(HKLM_REGISTRY_KEY, "ServerPort", null) ?? Registry.GetValue(LEGACY_REGISTRY_KEY, "ServerPort", null)) as string;
                if (!string.IsNullOrEmpty(legacyPort))
                {
                    settings.ServerPort = legacyPort;
                }

                var legacyHost = (Registry.GetValue(HKLM_REGISTRY_KEY, "ServerHost", null) ?? Registry.GetValue(LEGACY_REGISTRY_KEY, "ServerHost", null)) as string;
                if (!string.IsNullOrEmpty(legacyHost))
                {
                    settings.ServerHost = legacyHost;
                }

                var legacyDataDir = (Registry.GetValue(HKLM_REGISTRY_KEY, "DataDir", null) ?? Registry.GetValue(LEGACY_REGISTRY_KEY, "DataDir", null)) as string;
                if (!string.IsNullOrEmpty(legacyDataDir) && Directory.Exists(legacyDataDir))
                {
                    settings.DataDir = legacyDataDir;
                }

                var legacyAutoUpdate = Registry.GetValue(HKLM_REGISTRY_KEY, "AutoCheckForUpdates", null) ?? Registry.GetValue(LEGACY_REGISTRY_KEY, "AutoCheckForUpdates", null);
                if (legacyAutoUpdate is int intAutoUpdate)
                {
                    settings.AutoCheckForUpdates = intAutoUpdate != 0;
                }

                var legacyStartAtLogin = Registry.GetValue(HKLM_REGISTRY_KEY, "StartAtLogin", null) ?? Registry.GetValue(LEGACY_REGISTRY_KEY, "StartAtLogin", null);
                if (legacyStartAtLogin is int intStartAtLogin)
                {
                    settings.StartAtLogin = intStartAtLogin != 0;
                }

                var legacyAppVersion = GetInstalledVersion();
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

