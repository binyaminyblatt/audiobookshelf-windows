using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
using Newtonsoft.Json;
using NLog;

namespace Audiobookshelf.Common
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class DriveMap
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        [DllImport("mpr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int WNetAddConnection2W(ref NetworkResource netRes, string password, string username, int flags);

        [DllImport("mpr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int WNetCancelConnection2W(string name, int flags, int force);

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NetworkResource
        {
            public int Scope;
            public int Type;
            public int DisplayType;
            public int Usage;
            public string LocalName;
            public string RemoteName;
            public string Comment;
            public string Provider;
        }

        private const int RESOURCETYPE_DISK = 0x00000001;
        private const int CONNECT_UPDATE_PROFILE = 0x00000001;
        private const int NO_ERROR = 0;
        private const int ERROR_ALREADY_ASSIGNED = 85;
        private const int ERROR_DEVICE_ALREADY_REMEMBERED = 1202;
        private const int ERROR_SESSION_CREDENTIAL_CONFLICT = 1219;

        [JsonProperty]
        public string ShareName { get; set; } = string.Empty;

        [JsonProperty]
        public string DriveLetter { get; set; } = string.Empty;

        [JsonProperty]
        public string Username { get; set; } = string.Empty;

        [JsonProperty]
        public string Password { get; set; } = string.Empty;

        public DriveMap()
        {
        }

        public DriveMap(string shareName, string driveLetter, string username = "", string password = "")
        {
            ShareName = shareName;
            DriveLetter = driveLetter;
            Username = username;
            Password = password;
        }

        public string GetNormalizedDriveLetter()
        {
            if (string.IsNullOrWhiteSpace(DriveLetter))
                return string.Empty;

            string clean = DriveLetter.Trim().TrimEnd(':');
            return clean.Length > 0 ? clean.Substring(0, 1).ToUpper() + ":" : string.Empty;
        }

        public static int GetDriveBitMask(string driveLetter)
        {
            if (string.IsNullOrWhiteSpace(driveLetter)) return 0;
            char c = char.ToUpperInvariant(driveLetter.Trim()[0]);
            if (c >= 'A' && c <= 'Z')
            {
                int shift = c - 'A';
                return 1 << shift;
            }
            return 0;
        }

        /// <summary>
        /// Sets or clears the NoDrives policy in the registry to hide/unhide the drive from "This PC" / File Explorer.
        /// </summary>
        public static void SetDriveHidden(string driveLetter, bool hide)
        {
            int mask = GetDriveBitMask(driveLetter);
            if (mask == 0) return;

            UpdateNoDrivesRegistry(Registry.CurrentUser, mask, hide);
            UpdateNoDrivesRegistry(Registry.LocalMachine, mask, hide);

            try
            {
                // Notify Windows Explorer to immediately refresh policies and drive visibility
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error sending shell notification: {ex.Message}");
            }
        }

        private static void UpdateNoDrivesRegistry(RegistryKey rootKey, int mask, bool hide)
        {
            try
            {
                const string subKey = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer";
                using (var key = rootKey.OpenSubKey(subKey, true) ?? rootKey.CreateSubKey(subKey))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("NoDrives");
                        int current = 0;
                        if (val is int intVal)
                        {
                            current = intVal;
                        }
                        else if (val != null && int.TryParse(val.ToString(), out int parsed))
                        {
                            current = parsed;
                        }

                        int updated = hide ? (current | mask) : (current & ~mask);

                        if (updated != current || val == null)
                        {
                            key.SetValue("NoDrives", updated, RegistryValueKind.DWord);
                            _logger.Info($"Updated NoDrives on {rootKey.Name}: 0x{current:X} -> 0x{updated:X} (mask: 0x{mask:X}, hide: {hide})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Could not update NoDrives on {rootKey.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Syncs hidden state for all configured drive mappings.
        /// </summary>
        public static void SyncHiddenDrives(IEnumerable<DriveMap> configuredDriveMaps)
        {
            int combinedMask = 0;
            if (configuredDriveMaps != null)
            {
                foreach (var dm in configuredDriveMaps)
                {
                    combinedMask |= GetDriveBitMask(dm.DriveLetter);
                }
            }

            SetCombinedMask(Registry.CurrentUser, combinedMask);
            SetCombinedMask(Registry.LocalMachine, combinedMask);

            try
            {
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            }
            catch { }
        }

        /// <summary>
        /// Unmaps all currently configured network drives.
        /// </summary>
        public static void UnmapAllConfiguredDrives()
        {
            try
            {
                var settings = SettingsHandler.Load();
                if (settings.DriveMaps != null && settings.DriveMaps.Count > 0)
                {
                    _logger.Info($"Unmapping {settings.DriveMaps.Count} configured network drive(s)...");
                    foreach (var dm in settings.DriveMaps)
                    {
                        try
                        {
                            dm.UnMapDrive(true);
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"Error unmapping {dm.DriveLetter}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error in UnmapAllConfiguredDrives: {ex.Message}");
            }
        }

        private static void SetCombinedMask(RegistryKey rootKey, int mask)
        {
            try
            {
                const string subKey = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer";
                using (var key = rootKey.OpenSubKey(subKey, true) ?? rootKey.CreateSubKey(subKey))
                {
                    if (key != null)
                    {
                        key.SetValue("NoDrives", mask, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Could not set combined NoDrives mask on {rootKey.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Maps the network drive to the specified share and hides it in This PC.
        /// </summary>
        /// <param name="force">If true, attempts to unmap any existing connection on this drive letter first.</param>
        public void MapDrive(bool force = true)
        {
            string drive = GetNormalizedDriveLetter();
            if (string.IsNullOrEmpty(drive) && string.IsNullOrWhiteSpace(ShareName))
            {
                throw new InvalidOperationException("Invalid drive mapping: Share name and Drive letter cannot both be empty.");
            }

            if (force && !string.IsNullOrEmpty(drive))
            {
                try
                {
                    UnMapDrive(true);
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Previous connection cleanup on {drive}: {ex.Message}");
                }
            }

            var netRes = new NetworkResource
            {
                Scope = 2,
                Type = RESOURCETYPE_DISK,
                DisplayType = 3,
                Usage = 1,
                RemoteName = ShareName?.Trim(),
                LocalName = string.IsNullOrEmpty(drive) ? null : drive
            };

            string user = string.IsNullOrWhiteSpace(Username) ? null : Username.Trim();
            string pass = string.IsNullOrWhiteSpace(Password) ? null : Password;

            int result = WNetAddConnection2W(ref netRes, pass, user, 0);

            if (result != NO_ERROR && 
                result != ERROR_ALREADY_ASSIGNED && 
                result != ERROR_DEVICE_ALREADY_REMEMBERED && 
                result != ERROR_SESSION_CREDENTIAL_CONFLICT)
            {
                throw new Win32Exception(result, $"WNetAddConnection2 failed mapping '{drive}' to '{ShareName}' with error code {result}");
            }

            // Hide the mapped drive in "This PC" / File Explorer
            if (!string.IsNullOrEmpty(drive))
            {
                SetDriveHidden(drive, true);
            }

            _logger.Info($"Successfully mapped drive (hidden in This PC): {drive} -> {ShareName}");
        }

        /// <summary>
        /// Unmaps the network drive and restores visibility in This PC.
        /// </summary>
        public void UnMapDrive(bool force = true)
        {
            string drive = GetNormalizedDriveLetter();
            string target = !string.IsNullOrEmpty(drive) ? drive : ShareName;

            if (string.IsNullOrEmpty(target))
                return;

            int result = WNetCancelConnection2W(target, 0, force ? 1 : 0);

            // Restore visibility in "This PC"
            if (!string.IsNullOrEmpty(drive))
            {
                SetDriveHidden(drive, false);
            }

            if (result != NO_ERROR && result != 2250) // 2250 = ERROR_NOT_CONNECTED
            {
                throw new Win32Exception(result, $"WNetCancelConnection2 failed unmapping '{target}' with error code {result}");
            }
        }

        /// <summary>
        /// Tries to map the drive with retry attempts and delay.
        /// </summary>
        public bool TryMap(int maxRetries = 5, int delaySeconds = 5)
        {
            int remaining = Math.Max(1, maxRetries);
            string drive = GetNormalizedDriveLetter();

            while (remaining > 0)
            {
                try
                {
                    MapDrive(true);
                    return true;
                }
                catch (Exception ex)
                {
                    remaining--;
                    _logger.Warn($"Failed to map {drive} -> {ShareName}: {ex.Message}. Remaining attempts: {remaining}");
                    if (remaining > 0)
                    {
                        Thread.Sleep(delaySeconds * 1000);
                    }
                }
            }

            return false;
        }
    }
}
