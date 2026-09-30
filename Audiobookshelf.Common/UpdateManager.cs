using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NLog;

namespace Audiobookshelf.Common
{
    public class UpdateManager : IDisposable
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private Timer _midnightTimer;
        private readonly string _targetExePath;
        private readonly string _updatesDir;
        private readonly Func<Task> _stopServerAction;
        private readonly Action _startServerAction;
        private bool _isUpdating = false;
        private bool _isDisposed = false;

        private const string DEFAULT_REPO_OWNER = "binyaminyblatt";
        private const string DEFAULT_REPO_NAME = "audiobookshelf-windows";
        public const string SERVER_EXE_NAME = "audiobookshelf.exe";

        static UpdateManager()
        {
            try
            {
                // Ensure TLS 1.2 is enabled for HTTPS connections on .NET Framework 4.6.1+
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not set SecurityProtocol: {ex.Message}");
            }
        }

        private readonly Func<bool> _isServerRunningCheck;

        public UpdateManager(string targetExePath, Func<Task> stopServerAction, Action startServerAction, Func<bool> isServerRunningCheck = null)
        {
            _targetExePath = targetExePath;
            _stopServerAction = stopServerAction;
            _startServerAction = startServerAction;
            _isServerRunningCheck = isServerRunningCheck;

            _updatesDir = Settings.GetDefaultUpdatesDir();
            EnsureUpdatesDirectory();

            ScheduleNextMidnightCheck();
        }

        public void EnsureUpdatesDirectory()
        {
            try
            {
                if (!Directory.Exists(_updatesDir))
                {
                    Directory.CreateDirectory(_updatesDir);
                    _logger.Info($"Created update drop folder: {_updatesDir}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to create update drop directory {_updatesDir}: {ex}");
            }
        }

        public void ScheduleNextMidnightCheck()
        {
            if (_isDisposed) return;

            DateTime now = DateTime.Now;
            var settings = SettingsHandler.Load();
            int targetHour = Math.Max(0, Math.Min(23, settings.UpdateHour));

            DateTime nextRun = DateTime.Today.AddHours(targetHour);
            if (now >= nextRun)
            {
                nextRun = nextRun.AddDays(1);
            }

            TimeSpan delay = nextRun - now;
            _logger.Info($"Next scheduled midnight update check in {delay.TotalHours:F1} hours (at {nextRun:yyyy-MM-dd HH:mm:ss} local time).");

            _midnightTimer?.Dispose();
            _midnightTimer = new Timer(async _ =>
            {
                await RunMidnightUpdateSequenceAsync();
                ScheduleNextMidnightCheck();
            }, null, delay, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Runs the scheduled midnight check:
        /// 1. Verifies that an Admin API Key is configured.
        /// 2. If GitHub updates are enabled (OFF by default), checks and downloads new audiobookshelf.exe.
        /// 3. If audiobookshelf.exe is staged in the updates drop folder, waits for API backup completion and applies update.
        /// </summary>
        public async Task<bool> RunMidnightUpdateSequenceAsync()
        {
            if (_isUpdating || _isDisposed) return false;

            _logger.Info("Starting midnight update check sequence...");
            var settings = SettingsHandler.Load();

            // Check Admin API Key requirement
            if (string.IsNullOrWhiteSpace(settings.AdminApiKey))
            {
                _logger.Warn("Update aborted: Admin API Key is required for updates, but none is configured in Settings. Please configure the Admin API Key in Settings.");
                return false;
            }

            // 1. GitHub API check (ONLY if enabled in settings, disabled by default)
            if (settings.AutoUpdateFromGitHub)
            {
                _logger.Info("GitHub auto-update is enabled. Checking for latest releases on GitHub...");
                try
                {
                    await CheckAndDownloadFromGitHubAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error during GitHub release check: {ex}");
                }
            }
            else
            {
                _logger.Debug("GitHub auto-update is disabled (default). Skipping GitHub check.");
            }

            // 2. Check if a new audiobookshelf.exe is staged in the updates folder
            if (settings.AutoApplyFolderUpdates)
            {
                string stagedExe = Path.Combine(_updatesDir, SERVER_EXE_NAME);
                if (File.Exists(stagedExe))
                {
                    _logger.Info($"Found new server executable staged in {_updatesDir}. Performing verified backup and applying update...");
                    return await ApplyStagedUpdateAsync(stagedExe);
                }
                else
                {
                    _logger.Debug("No pending server update file found in drop folder.");
                    return false;
                }
            }

            return false;
        }

        public async Task<bool> CheckAndDownloadFromGitHubAsync()
        {
            var settings = SettingsHandler.Load();
            if (string.IsNullOrWhiteSpace(settings.AdminApiKey))
            {
                _logger.Warn("GitHub update check cancelled: Admin API Key is required.");
                return false;
            }

            string owner = string.IsNullOrWhiteSpace(settings.GitHubRepoOwner) ? DEFAULT_REPO_OWNER : settings.GitHubRepoOwner;
            string repo = string.IsNullOrWhiteSpace(settings.GitHubRepoName) ? DEFAULT_REPO_NAME : settings.GitHubRepoName;
            string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Audiobookshelf-Windows-Service");
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

                _logger.Info($"Querying GitHub API at {apiUrl} for latest release...");
                var response = await httpClient.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.Warn($"GitHub API request returned status: {response.StatusCode}");
                    return false;
                }

                string json = await response.Content.ReadAsStringAsync();
                var releaseObj = JObject.Parse(json);
                string latestTag = releaseObj["tag_name"]?.ToString();
                string currentVersion = settings.AppVersion;

                _logger.Info($"GitHub latest release tag: {latestTag}, Current configured version: {currentVersion}");

                if (!string.IsNullOrEmpty(latestTag) && latestTag != currentVersion)
                {
                    var assets = releaseObj["assets"] as JArray;
                    // Find audiobookshelf.exe asset in release
                    var serverAsset = assets?.FirstOrDefault(a => 
                        string.Equals(a["name"]?.ToString(), SERVER_EXE_NAME, StringComparison.OrdinalIgnoreCase)
                    );

                    if (serverAsset != null)
                    {
                        string downloadUrl = serverAsset["browser_download_url"]?.ToString();
                        if (!string.IsNullOrEmpty(downloadUrl))
                        {
                            _logger.Info($"Downloading new server version {latestTag} from {downloadUrl}...");
                            EnsureUpdatesDirectory();

                            string downloadPath = Path.Combine(_updatesDir, SERVER_EXE_NAME + ".download");
                            string finalStagedPath = Path.Combine(_updatesDir, SERVER_EXE_NAME);

                            using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                            using (var downloadStream = await httpClient.GetStreamAsync(downloadUrl))
                            {
                                await downloadStream.CopyToAsync(fileStream);
                            }

                            // 1. Locate and fetch companion checksum and signature assets
                            var sigAsset = assets?.FirstOrDefault(a =>
                                string.Equals(a["name"]?.ToString(), SERVER_EXE_NAME + ".sig", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(a["name"]?.ToString(), "SHA256SUMS.sig", StringComparison.OrdinalIgnoreCase)
                            );

                            var shaAsset = assets?.FirstOrDefault(a =>
                                string.Equals(a["name"]?.ToString(), SERVER_EXE_NAME + ".sha256", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(a["name"]?.ToString(), "SHA256SUMS", StringComparison.OrdinalIgnoreCase)
                            );

                            string expectedHash = null;
                            if (shaAsset != null)
                            {
                                try
                                {
                                    string shaUrl = shaAsset["browser_download_url"]?.ToString();
                                    string shaText = await httpClient.GetStringAsync(shaUrl);
                                    expectedHash = CryptographicVerifier.ExtractHashForFile(shaText, SERVER_EXE_NAME);
                                    _logger.Info($"[Security] Retrieved published SHA-256 checksum from release asset {shaAsset["name"]}: {expectedHash}");
                                }
                                catch (Exception ex)
                                {
                                    _logger.Warn($"[Security] Could not retrieve checksum asset: {ex.Message}");
                                }
                            }

                            byte[] signatureBytes = null;
                            if (sigAsset != null)
                            {
                                try
                                {
                                    string sigUrl = sigAsset["browser_download_url"]?.ToString();
                                    signatureBytes = await httpClient.GetByteArrayAsync(sigUrl);
                                    _logger.Info($"[Security] Retrieved cryptographic signature asset {sigAsset["name"]} ({signatureBytes.Length} bytes).");
                                }
                                catch (Exception ex)
                                {
                                    _logger.Warn($"[Security] Could not retrieve signature asset: {ex.Message}");
                                }
                            }

                            // 2. Perform Supply Chain Provenance & Cryptographic Verification
                            _logger.Info($"[Security] Verifying binary authenticity and integrity for {SERVER_EXE_NAME} ({latestTag})...");
                            bool isVerified = CryptographicVerifier.VerifyFileIntegrityAndAuthenticity(
                                downloadPath,
                                expectedHash,
                                signatureBytes
                            );

                            if (!isVerified)
                            {
                                _logger.Error($"[Security] CRITICAL: Cryptographic verification FAILED for GitHub release {latestTag}! Binary was not verified as authentic. Discarding update.");
                                _logger.Warn("[Security] NOTE: If the signing key was changed/rotated in this release, automatic updates cannot verify the new binary with your older installed public key. You must manually download and run AudiobookshelfInstaller.exe to update.");
                                if (File.Exists(downloadPath))
                                {
                                    try { File.Delete(downloadPath); } catch { }
                                }
                                return false;
                            }

                            _logger.Info($"[Security] Verification SUCCESS: Binary is authentic from GitHub workflow and SHA-256 checksum is valid.");

                            if (File.Exists(finalStagedPath))
                            {
                                File.Delete(finalStagedPath);
                            }
                            File.Move(downloadPath, finalStagedPath);

                            _logger.Info($"Successfully staged verified version {latestTag} to update folder: {finalStagedPath}");
                            return true;
                        }
                    }
                    else
                    {
                        _logger.Warn($"Release {latestTag} exists on GitHub but no '{SERVER_EXE_NAME}' asset was found.");
                    }
                }
                else
                {
                    _logger.Info("Audiobookshelf is already up to date with the latest GitHub release.");
                }
            }

            return false;
        }

        public async Task<bool> ApplyStagedUpdateAsync(string stagedExePath)
        {
            _isUpdating = true;
            string backupDir = string.Empty;
            string localBackupExe = _targetExePath + ".bak";
            FileSecurity originalExeSecurity = null;
            string previousVersion = string.Empty;

            try
            {
                var settings = SettingsHandler.Load();
                previousVersion = settings.AppVersion;

                // 1. Validate Admin API Key requirement
                if (string.IsNullOrWhiteSpace(settings.AdminApiKey))
                {
                    _logger.Error("Update cancelled: Admin API Key is required to apply updates. Please configure the Admin API Key in Settings.");
                    return false;
                }

                if (!File.Exists(stagedExePath))
                {
                    _logger.Warn($"Staged update file does not exist: {stagedExePath}");
                    return false;
                }

                var fileInfo = new FileInfo(stagedExePath);
                if (fileInfo.Length < 1024)
                {
                    _logger.Error($"Staged update file is invalid or too small ({fileInfo.Length} bytes). Aborting update.");
                    return false;
                }

                _logger.Info("Starting pre-update procedures...");

                // Capture original server executable permissions (ACLs) before any modifications
                originalExeSecurity = GetFileSecuritySafely(_targetExePath);

                // Determine current version string for backup renaming
                string currentVersion = GetNormalizedVersion(settings);

                // 2. Trigger Server Database/Metadata Backup via API, wait for API completion confirmation, and rename
                bool apiBackupSuccess = await TriggerServerApiBackupAndWaitAsync(settings, currentVersion);
                if (apiBackupSuccess)
                {
                    _logger.Info("Server API backup verified complete and renamed successfully.");
                }
                else
                {
                    _logger.Warn("Server API backup could not be completed via API. Proceeding with file-system snapshot backup...");
                }

                // 3. Create full File System Snapshot Backup (Executable + Config directory)
                CreateFileSystemBackup(settings, currentVersion, out backupDir);

                // 4. Cleanly Stop Audiobookshelf Server
                _logger.Info("Stopping Audiobookshelf server for update replacement...");
                if (_stopServerAction != null)
                {
                    await _stopServerAction();
                }

                // Wait a brief moment for file handles to release
                await Task.Delay(2000);

                // 5. Replace server executable while preserving file permissions
                if (File.Exists(_targetExePath))
                {
                    try
                    {
                        if (File.Exists(localBackupExe)) File.Delete(localBackupExe);
                        File.Copy(_targetExePath, localBackupExe, true);
                        _logger.Info($"Created local backup at: {localBackupExe}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Could not copy existing binary to backup: {ex.Message}");
                    }
                }

                // Copy new binary over target path
                File.Copy(stagedExePath, _targetExePath, true);
                _logger.Info($"Replaced server executable with new version at: {_targetExePath}");

                // Restore and preserve original file security / permissions (ACLs, Owner, DACL)
                RestoreFileSecuritySafely(_targetExePath, originalExeSecurity);

                // 6. Restart server with new version
                _logger.Info("Restarting Audiobookshelf server with updated version...");
                _startServerAction?.Invoke();

                // 7. Verify that the updated server starts and runs healthily (up to 5 minutes for database migrations)
                _logger.Info("Verifying that the updated server starts and passes health checks (allowing up to 5 minutes for database migrations/startup to complete)...");
                bool isHealthy = await VerifyServerHealthAsync(settings, timeoutSeconds: 300);
                if (!isHealthy)
                {
                    _logger.Error("Updated server failed to start, crashed, or did not respond to health checks within 5 minutes! Initiating automatic rollback to previous version...");
                    await RollbackToPreviousVersionAsync(localBackupExe, backupDir, originalExeSecurity, previousVersion, stagedExePath);
                    return false;
                }

                // 8. Update verified healthy: Update saved version in settings and clean up staged file
                _logger.Info($"Update applied and verified healthy! Pre-update backup preserved at: {backupDir}");

                string newVer = GetCurrentExeVersion();
                if (!string.IsNullOrWhiteSpace(newVer))
                {
                    settings.AppVersion = newVer;
                    SettingsHandler.Save(settings);
                }

                try
                {
                    File.Delete(stagedExePath);
                    _logger.Debug($"Removed staged file: {stagedExePath}");
                }
                catch (Exception delEx)
                {
                    _logger.Warn($"Could not delete staged update file: {delEx.Message}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to apply server update: {ex}. Rolling back...");
                await RollbackToPreviousVersionAsync(localBackupExe, backupDir, originalExeSecurity, previousVersion, stagedExePath);
                return false;
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private async Task RollbackToPreviousVersionAsync(string localBackupExe, string backupDir, FileSecurity originalExeSecurity, string previousVersion, string failedStagedExe)
        {
            try
            {
                _logger.Warn("Beginning rollback procedure to restore previous working version...");

                // 1. Stop any broken or hanging server process
                if (_stopServerAction != null)
                {
                    try
                    {
                        await _stopServerAction();
                    }
                    catch (Exception stopEx)
                    {
                        _logger.Warn($"Error stopping server during rollback: {stopEx.Message}");
                    }
                }
                await Task.Delay(2000);

                // 2. Restore previous working executable binary
                bool restored = false;
                if (!string.IsNullOrEmpty(localBackupExe) && File.Exists(localBackupExe))
                {
                    File.Copy(localBackupExe, _targetExePath, true);
                    RestoreFileSecuritySafely(_targetExePath, originalExeSecurity);
                    _logger.Info($"Restored previous server binary from local backup: {localBackupExe}");
                    restored = true;
                }
                else if (!string.IsNullOrEmpty(backupDir))
                {
                    string archivedExe = Path.Combine(backupDir, SERVER_EXE_NAME);
                    if (File.Exists(archivedExe))
                    {
                        File.Copy(archivedExe, _targetExePath, true);
                        RestoreFileSecuritySafely(_targetExePath, originalExeSecurity);
                        _logger.Info($"Restored previous server binary from snapshot backup: {archivedExe}");
                        restored = true;
                    }
                }

                if (!restored)
                {
                    _logger.Error("Rollback error: No previous working server executable backup was found to restore.");
                }

                // 3. Quarantine failed staged update binary to prevent repeat update loops
                if (!string.IsNullOrEmpty(failedStagedExe) && File.Exists(failedStagedExe))
                {
                    try
                    {
                        string failedMarkerPath = failedStagedExe + ".failed";
                        if (File.Exists(failedMarkerPath)) File.Delete(failedMarkerPath);
                        File.Move(failedStagedExe, failedMarkerPath);
                        _logger.Warn($"Quarantined failed update file to: {failedMarkerPath}");
                    }
                    catch (Exception qEx)
                    {
                        _logger.Warn($"Could not quarantine failed update file: {qEx.Message}");
                    }
                }

                // 4. Revert saved AppVersion
                if (!string.IsNullOrWhiteSpace(previousVersion))
                {
                    try
                    {
                        var settings = SettingsHandler.Load();
                        settings.AppVersion = previousVersion;
                        SettingsHandler.Save(settings);
                    }
                    catch { }
                }

                // 5. Restart server with restored previous version
                if (restored)
                {
                    _logger.Info("Restarting Audiobookshelf server with restored previous version...");
                    _startServerAction?.Invoke();

                    var settings = SettingsHandler.Load();
                    bool isRestoredHealthy = await VerifyServerHealthAsync(settings, timeoutSeconds: 60);
                    if (isRestoredHealthy)
                    {
                        _logger.Info("Server successfully rolled back to previous version and verified healthy.");
                    }
                    else
                    {
                        _logger.Warn("Server rolled back to previous version. Supervisor process watchdog is active.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fatal error during rollback execution: {ex}");
            }
        }

        private async Task<bool> VerifyServerHealthAsync(Settings settings, int timeoutSeconds = 300)
        {
            string port = string.IsNullOrWhiteSpace(settings.ServerPort) ? "13378" : settings.ServerPort.Trim();
            string apiKey = settings.AdminApiKey;

            var stopwatch = Stopwatch.StartNew();
            int lastLoggedSec = -1;

            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) })
            {
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                // Initial grace period for process to spawn
                await Task.Delay(1500);

                while (stopwatch.Elapsed.TotalSeconds < timeoutSeconds)
                {
                    if (_isDisposed) return false;

                    // 1. Process watchdog check: if the server process terminates/crashes, fail immediately and trigger rollback
                    if (_isServerRunningCheck != null && stopwatch.Elapsed.TotalSeconds >= 2)
                    {
                        if (!_isServerRunningCheck())
                        {
                            _logger.Error($"[HealthCheck] Server process terminated/crashed during startup ({stopwatch.Elapsed.TotalSeconds:F1}s after launch). Aborting wait and initiating immediate rollback.");
                            return false;
                        }
                    }

                    // 2. Periodic status logging every 15 seconds while waiting for migrations
                    int elapsedSec = (int)stopwatch.Elapsed.TotalSeconds;
                    if (elapsedSec > 0 && elapsedSec % 15 == 0 && elapsedSec != lastLoggedSec)
                    {
                        lastLoggedSec = elapsedSec;
                        _logger.Info($"[HealthCheck] Waiting for server migration/startup to complete... ({elapsedSec}s / {timeoutSeconds}s elapsed, process is running)");
                    }

                    // 3. HTTP /ping endpoint check
                    try
                    {
                        var pingResp = await client.GetAsync($"http://127.0.0.1:{port}/ping");
                        if (pingResp.IsSuccessStatusCode)
                        {
                            _logger.Info($"[HealthCheck] Server responded OK to /ping in {stopwatch.Elapsed.TotalSeconds:F1}s (startup/migration completed).");
                            return true;
                        }
                    }
                    catch { }

                    // 4. HTTP API endpoint check
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        try
                        {
                            var apiResp = await client.GetAsync($"http://127.0.0.1:{port}/api/me");
                            if (apiResp.IsSuccessStatusCode || apiResp.StatusCode == HttpStatusCode.Unauthorized)
                            {
                                _logger.Info($"[HealthCheck] Server responded OK to /api/me in {stopwatch.Elapsed.TotalSeconds:F1}s (startup/migration completed).");
                                return true;
                            }
                        }
                        catch { }
                    }

                    // 5. HTTP Web Root check
                    try
                    {
                        var rootResp = await client.GetAsync($"http://127.0.0.1:{port}/");
                        if (rootResp.IsSuccessStatusCode || rootResp.StatusCode == HttpStatusCode.Redirect || rootResp.StatusCode == HttpStatusCode.Found)
                        {
                            _logger.Info($"[HealthCheck] Server responded OK to HTTP root in {stopwatch.Elapsed.TotalSeconds:F1}s (startup/migration completed).");
                            return true;
                        }
                    }
                    catch { }

                    await Task.Delay(1000);
                }
            }

            _logger.Warn($"[HealthCheck] Server process did not respond to pings/health checks within {timeoutSeconds} seconds (5 minutes). Rolling back...");
            return false;
        }

        private FileSecurity GetFileSecuritySafely(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return null;

            try
            {
                var fi = new FileInfo(filePath);
                return fi.GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
            }
            catch
            {
                try
                {
                    var fi = new FileInfo(filePath);
                    return fi.GetAccessControl(AccessControlSections.Access);
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Could not retrieve security/permissions for '{filePath}': {ex.Message}");
                    return null;
                }
            }
        }

        private void RestoreFileSecuritySafely(string filePath, FileSecurity originalSecurity)
        {
            if (originalSecurity == null || string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;

            try
            {
                var fi = new FileInfo(filePath);
                fi.SetAccessControl(originalSecurity);
                _logger.Info($"Preserved and applied original file security permissions (ACLs) to '{filePath}'.");
            }
            catch (Exception secEx)
            {
                _logger.Warn($"Could not apply full security descriptor to '{filePath}' ({secEx.Message}). Attempting DACL only...");
                try
                {
                    var daclOnly = new FileSecurity();
                    byte[] daclBytes = originalSecurity.GetSecurityDescriptorBinaryForm();
                    daclOnly.SetSecurityDescriptorBinaryForm(daclBytes, AccessControlSections.Access);
                    var fi = new FileInfo(filePath);
                    fi.SetAccessControl(daclOnly);
                    _logger.Info($"Preserved DACL access permissions on '{filePath}'.");
                }
                catch (Exception daclEx)
                {
                    _logger.Warn($"Could not apply DACL permissions to '{filePath}': {daclEx.Message}");
                }
            }
        }

        private string GetNormalizedVersion(Settings settings)
        {
            string version = !string.IsNullOrWhiteSpace(settings.AppVersion) ? settings.AppVersion.Trim() : GetCurrentExeVersion();
            if (string.IsNullOrWhiteSpace(version))
            {
                version = "current";
            }
            return version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : $"v{version}";
        }

        private string GetCurrentExeVersion()
        {
            try
            {
                if (File.Exists(_targetExePath))
                {
                    var info = FileVersionInfo.GetVersionInfo(_targetExePath);
                    if (!string.IsNullOrWhiteSpace(info.ProductVersion))
                    {
                        return info.ProductVersion.Trim();
                    }
                    if (!string.IsNullOrWhiteSpace(info.FileVersion))
                    {
                        return info.FileVersion.Trim();
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        /// <summary>
        /// Triggers a backup via Audiobookshelf API, continuously polls the API to verify completion
        /// regardless of how long/large the backup is, and renames the resulting backup to "before v... update".
        /// </summary>
        private async Task<bool> TriggerServerApiBackupAndWaitAsync(Settings settings, string currentVersion)
        {
            try
            {
                string port = string.IsNullOrWhiteSpace(settings.ServerPort) ? "13378" : settings.ServerPort;
                string apiKey = settings.AdminApiKey;

                string baseDataDir = string.IsNullOrWhiteSpace(settings.DataDir)
                    ? Settings.GetDefaultDataDir()
                    : settings.DataDir;
                string metadataBackupsDir = Path.Combine(baseDataDir, "metadata", "backups");
                Directory.CreateDirectory(metadataBackupsDir);

                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) })
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                    // 1. Fetch initial backup list from API to know existing backup IDs
                    var initialBackupIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    try
                    {
                        var getResp = await client.GetAsync($"http://127.0.0.1:{port}/api/backups");
                        if (getResp.IsSuccessStatusCode)
                        {
                            string getJson = await getResp.Content.ReadAsStringAsync();
                            var parsed = JToken.Parse(getJson);
                            var backupsArray = parsed is JArray arr ? arr : parsed["backups"] as JArray;
                            if (backupsArray != null)
                            {
                                foreach (var b in backupsArray)
                                {
                                    string id = b["id"]?.ToString();
                                    if (!string.IsNullOrEmpty(id)) initialBackupIds.Add(id);
                                }
                            }
                            _logger.Info($"[Backup] Existing server backups count: {initialBackupIds.Count}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"Could not fetch initial backups list via API: {ex.Message}");
                    }

                    // Also snapshot filesystem entries as secondary check
                    var existingFiles = new HashSet<string>(
                        Directory.GetFileSystemEntries(metadataBackupsDir), 
                        StringComparer.OrdinalIgnoreCase
                    );
                    DateTime triggerTime = DateTime.Now.AddSeconds(-2);

                    // 2. Trigger the backup via POST /api/backups (or fallback /api/backup/create)
                    string postUrl = $"http://127.0.0.1:{port}/api/backups";
                    _logger.Info($"Requesting pre-update database backup from server via API at {postUrl}...");

                    var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                    var postResponse = await client.PostAsync(postUrl, content);

                    if (postResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        postUrl = $"http://127.0.0.1:{port}/api/backup/create";
                        postResponse = await client.PostAsync(postUrl, content);
                    }

                    if (!postResponse.IsSuccessStatusCode)
                    {
                        _logger.Warn($"Server backup API returned status {(int)postResponse.StatusCode} ({postResponse.ReasonPhrase}).");
                        return false;
                    }

                    _logger.Info("Backup API request accepted by Audiobookshelf. Waiting for API verification of backup completion...");

                    // 3. Poll API (and disk) until Audiobookshelf reports the backup is 100% complete
                    // Max wait time: up to 900 seconds (15 minutes) for large libraries
                    string completedBackupPath = null;
                    JObject completedApiBackupObj = null;

                    for (int elapsedSec = 0; elapsedSec < 900; elapsedSec += 2)
                    {
                        await Task.Delay(2000);

                        // Check via API
                        try
                        {
                            var pollResp = await client.GetAsync($"http://127.0.0.1:{port}/api/backups");
                            if (pollResp.IsSuccessStatusCode)
                            {
                                string pollJson = await pollResp.Content.ReadAsStringAsync();
                                var parsed = JToken.Parse(pollJson);
                                var backupsArray = parsed is JArray arr ? arr : parsed["backups"] as JArray;
                                if (backupsArray != null)
                                {
                                    foreach (var b in backupsArray)
                                    {
                                        string id = b["id"]?.ToString();
                                        if (!string.IsNullOrEmpty(id) && !initialBackupIds.Contains(id))
                                        {
                                            // New backup confirmed by Audiobookshelf API!
                                            completedApiBackupObj = b as JObject;
                                            _logger.Info($"[Backup] Audiobookshelf API confirmed backup completion! (Backup ID: {id})");
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"Error polling /api/backups: {ex.Message}");
                        }

                        // Determine the file path on disk
                        if (completedApiBackupObj != null)
                        {
                            string backupFileName = completedApiBackupObj["filename"]?.ToString() 
                                ?? completedApiBackupObj["backupPath"]?.ToString() 
                                ?? completedApiBackupObj["path"]?.ToString();

                            if (!string.IsNullOrEmpty(backupFileName))
                            {
                                string candidatePath = Path.IsPathRooted(backupFileName) 
                                    ? backupFileName 
                                    : Path.Combine(metadataBackupsDir, Path.GetFileName(backupFileName));

                                if (File.Exists(candidatePath) && IsFileReady(candidatePath))
                                {
                                    completedBackupPath = candidatePath;
                                    break;
                                }
                            }
                        }

                        // Also check filesystem in case API returns before file is synced or if API doesn't list
                        var currentFiles = Directory.GetFileSystemEntries(metadataBackupsDir);
                        var newDiskCandidates = currentFiles
                            .Where(f => !existingFiles.Contains(f) || Directory.GetLastWriteTime(f) >= triggerTime)
                            .OrderByDescending(f => Directory.GetLastWriteTime(f))
                            .ToList();

                        if (newDiskCandidates.Count > 0)
                        {
                            string newest = newDiskCandidates.First();
                            string ext = Path.GetExtension(newest);
                            if (!ext.Equals(".tmp", StringComparison.OrdinalIgnoreCase) &&
                                !ext.Equals(".part", StringComparison.OrdinalIgnoreCase) &&
                                !ext.Equals(".download", StringComparison.OrdinalIgnoreCase))
                            {
                                if (File.Exists(newest))
                                {
                                    var fi = new FileInfo(newest);
                                    if (fi.Length > 1024 && IsFileReady(newest))
                                    {
                                        // If API confirmed or file is stable
                                        if (completedApiBackupObj != null || elapsedSec >= 10)
                                        {
                                            completedBackupPath = newest;
                                            break;
                                        }
                                    }
                                }
                                else if (Directory.Exists(newest))
                                {
                                    completedBackupPath = newest;
                                    break;
                                }
                            }
                        }

                        if (elapsedSec % 10 == 0 && elapsedSec > 0)
                        {
                            _logger.Info($"[Backup] Waiting for server backup creation to finish... ({elapsedSec}s elapsed)");
                        }
                    }

                    if (string.IsNullOrEmpty(completedBackupPath))
                    {
                        _logger.Warn("Timed out waiting for server backup completion via API.");
                        return false;
                    }

                    _logger.Info($"[Backup] Server backup completed at: {completedBackupPath}");

                    // 4. Rename backup to "before v... update"
                    string extension = Path.GetExtension(completedBackupPath);
                    string desiredName = $"before {currentVersion} update{extension}";
                    string destinationPath = Path.Combine(metadataBackupsDir, desiredName);

                    if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
                    {
                        string nameNoExt = Path.GetFileNameWithoutExtension(desiredName);
                        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                        destinationPath = Path.Combine(metadataBackupsDir, $"{nameNoExt} ({timestamp}){extension}");
                    }

                    if (File.Exists(completedBackupPath))
                    {
                        File.Move(completedBackupPath, destinationPath);
                    }
                    else if (Directory.Exists(completedBackupPath))
                    {
                        Directory.Move(completedBackupPath, destinationPath);
                    }

                    _logger.Info($"[Backup] Backup successfully renamed to: {Path.GetFileName(destinationPath)}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error while triggering or verifying server API backup: {ex}");
                return false;
            }
        }

        private static bool IsFileReady(string filename)
        {
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return inputStream.Length > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool CreateFileSystemBackup(Settings settings, string currentVersion, out string backupDir)
        {
            backupDir = string.Empty;
            try
            {
                string baseDataDir = string.IsNullOrWhiteSpace(settings.DataDir)
                    ? Settings.GetDefaultDataDir()
                    : settings.DataDir;

                string backupsRoot = Path.Combine(baseDataDir, "backups");
                Directory.CreateDirectory(backupsRoot);

                string desiredDirName = $"before {currentVersion} update";
                backupDir = Path.Combine(backupsRoot, desiredDirName);
                if (Directory.Exists(backupDir))
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    backupDir = Path.Combine(backupsRoot, $"{desiredDirName} ({timestamp})");
                }
                Directory.CreateDirectory(backupDir);

                _logger.Info($"Creating pre-update file snapshot backup at: {backupDir}");

                // 1. Backup current server binary
                if (File.Exists(_targetExePath))
                {
                    string backupExe = Path.Combine(backupDir, SERVER_EXE_NAME);
                    File.Copy(_targetExePath, backupExe, true);
                    _logger.Info($"Backed up {SERVER_EXE_NAME} to {backupExe}");
                }

                // 2. Backup config folder if present
                string configDir = Path.Combine(baseDataDir, "config");
                if (Directory.Exists(configDir))
                {
                    string backupConfigDir = Path.Combine(backupDir, "config");
                    Directory.CreateDirectory(backupConfigDir);
                    CopyDirectory(configDir, backupConfigDir);
                    _logger.Info($"Backed up config directory to {backupConfigDir}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to create pre-update file backup: {ex}");
                return false;
            }
        }

        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            foreach (string dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dir.Replace(sourceDir, targetDir));
            }

            foreach (string file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(file, file.Replace(sourceDir, targetDir), true);
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _midnightTimer?.Dispose();
                _midnightTimer = null;
            }
        }
    }
}
