using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
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

        /// <summary>
        /// Maps the network drive to the specified share.
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

            _logger.Info($"Successfully mapped drive: {drive} -> {ShareName}");
        }

        /// <summary>
        /// Unmaps the network drive.
        /// </summary>
        public void UnMapDrive(bool force = true)
        {
            string drive = GetNormalizedDriveLetter();
            string target = !string.IsNullOrEmpty(drive) ? drive : ShareName;

            if (string.IsNullOrEmpty(target))
                return;

            int result = WNetCancelConnection2W(target, 0, force ? 1 : 0);
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
