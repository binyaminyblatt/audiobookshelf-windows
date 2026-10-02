using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Audiobookshelf.Common
{
    public class BackupResult
    {
        public bool Success { get; }
        public string Message { get; }

        public BackupResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public void Deconstruct(out bool success, out string message)
        {
            success = Success;
            message = Message;
        }
    }

    public static class ServerHealthHelper
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

        public static async Task<bool> CheckServerHealthAsync(string port, int timeoutMs = 2000)
        {
            if (string.IsNullOrWhiteSpace(port)) port = "13378";

            try
            {
                using (var cts = new CancellationTokenSource(timeoutMs))
                {
                    string url = $"http://127.0.0.1:{port}/ping";
                    var response = await _httpClient.GetAsync(url, cts.Token).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }

                    // Fallback to /api/libraries or root endpoint
                    url = $"http://127.0.0.1:{port}/";
                    var fallbackResp = await _httpClient.GetAsync(url, cts.Token).ConfigureAwait(false);
                    return fallbackResp.IsSuccessStatusCode || fallbackResp.StatusCode == System.Net.HttpStatusCode.Unauthorized;
                }
            }
            catch
            {
                return false;
            }
        }

        public static async Task<BackupResult> TriggerBackupAsync(string port, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(port)) port = "13378";

            var settings = SettingsHandler.Load();
            string dataDir = string.IsNullOrWhiteSpace(settings.DataDir)
                ? Settings.GetDefaultDataDir()
                : settings.DataDir;

            // 1. If API Key is provided, attempt server-side backup via official API first
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                try
                {
                    using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
                        client.DefaultRequestHeaders.Add("x-api-key", apiKey.Trim());

                        // Primary endpoints: POST /api/backups or POST /audiobookshelf/api/backups
                        string[] endpointUrls = new[]
                        {
                            $"http://127.0.0.1:{port}/api/backups",
                            $"http://127.0.0.1:{port}/audiobookshelf/api/backups",
                            $"http://127.0.0.1:{port}/api/backup/create",
                            $"http://127.0.0.1:{port}/api/backup"
                        };

                        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                        foreach (var url in endpointUrls)
                        {
                            try
                            {
                                var response = await client.PostAsync(url, content).ConfigureAwait(false);
                                if (response.IsSuccessStatusCode)
                                {
                                    string respBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                                    _logger.Info($"Audiobookshelf server backup API completed successfully at {url}: {respBody}");
                                    return new BackupResult(true, "Database backup created successfully via Audiobookshelf API in metadata\\backups.");
                                }
                            }
                            catch { }
                        }

                        _logger.Warn("Audiobookshelf backup API endpoints did not succeed. Falling back to direct database snapshot...");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Audiobookshelf backup API request failed: {ex.Message}. Falling back to direct database snapshot...");
                }
            }

            // 2. Direct database snapshot backup (works whether server is running, stopped, or if no API key is configured)
            return CreateDirectDatabaseBackup(dataDir);
        }

        public static BackupResult CreateDirectDatabaseBackup(string dataDir)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dataDir))
                {
                    dataDir = Settings.GetDefaultDataDir();
                }

                string configDir = Path.Combine(dataDir, "config");
                string metadataDir = Path.Combine(dataDir, "metadata");
                string backupsDir = Path.Combine(metadataDir, "backups");
                if (!Directory.Exists(backupsDir))
                {
                    Directory.CreateDirectory(backupsDir);
                }

                // Look for database files in config\, metadata\, and dataDir\
                string[] candidateDbPaths = new[]
                {
                    Path.Combine(configDir, "absdatabase.sqlite"),
                    Path.Combine(configDir, "audiobookshelf.db"),
                    Path.Combine(configDir, "database.sqlite"),
                    Path.Combine(metadataDir, "absdatabase.sqlite"),
                    Path.Combine(metadataDir, "audiobookshelf.db"),
                    Path.Combine(metadataDir, "database.sqlite"),
                    Path.Combine(dataDir, "absdatabase.sqlite"),
                    Path.Combine(dataDir, "audiobookshelf.db"),
                    Path.Combine(dataDir, "database.sqlite")
                };

                string foundDb = null;
                foreach (var path in candidateDbPaths)
                {
                    if (File.Exists(path))
                    {
                        foundDb = path;
                        break;
                    }
                }

                // If not found in candidates, search for any .sqlite or .db file in config or metadata
                if (foundDb == null && Directory.Exists(configDir))
                {
                    var sqliteFiles = Directory.GetFiles(configDir, "*.sqlite");
                    if (sqliteFiles.Length > 0)
                    {
                        foundDb = sqliteFiles[0];
                    }
                    else
                    {
                        var dbFiles = Directory.GetFiles(configDir, "*.db");
                        if (dbFiles.Length > 0)
                        {
                            foundDb = dbFiles[0];
                        }
                    }
                }

                string timeStr = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

                if (foundDb != null)
                {
                    string ext = Path.GetExtension(foundDb);
                    string backupDbFile = Path.Combine(backupsDir, $"absdatabase_backup_{timeStr}{ext}");

                    // Safely copy the SQLite database with FileShare.ReadWrite to allow copying while database is active
                    using (var sourceStream = new FileStream(foundDb, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var destStream = new FileStream(backupDbFile, FileMode.Create, FileAccess.Write))
                    {
                        sourceStream.CopyTo(destStream);
                    }

                    // Also copy WAL and SHM files if present
                    string walFile = foundDb + "-wal";
                    if (File.Exists(walFile))
                    {
                        try
                        {
                            using (var src = new FileStream(walFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (var dst = new FileStream(backupDbFile + "-wal", FileMode.Create, FileAccess.Write))
                            {
                                src.CopyTo(dst);
                            }
                        }
                        catch { }
                    }

                    string shmFile = foundDb + "-shm";
                    if (File.Exists(shmFile))
                    {
                        try
                        {
                            using (var src = new FileStream(shmFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (var dst = new FileStream(backupDbFile + "-shm", FileMode.Create, FileAccess.Write))
                            {
                                src.CopyTo(dst);
                            }
                        }
                        catch { }
                    }

                    // Also copy config.json if present
                    string configJsonPath = Path.Combine(configDir, "config.json");
                    if (!File.Exists(configJsonPath)) configJsonPath = Path.Combine(dataDir, "config.json");
                    if (File.Exists(configJsonPath))
                    {
                        try
                        {
                            string backupConfigJson = Path.Combine(backupsDir, $"config_backup_{timeStr}.json");
                            File.Copy(configJsonPath, backupConfigJson, true);
                        }
                        catch { }
                    }

                    _logger.Info($"Direct database snapshot created: {backupDbFile}");
                    return new BackupResult(true, $"Database backup created successfully: {Path.GetFileName(backupDbFile)} in metadata\\backups");
                }

                // If no database file found yet, snapshot config directory
                if (Directory.Exists(configDir))
                {
                    string backupTargetDir = Path.Combine(backupsDir, $"backup_{timeStr}_config");
                    Directory.CreateDirectory(backupTargetDir);
                    CopyDirectory(configDir, backupTargetDir);
                    return new BackupResult(true, $"Config snapshot created successfully in metadata\\backups\\backup_{timeStr}_config");
                }

                return new BackupResult(false, $"No database file found to back up in {dataDir}. Ensure Audiobookshelf has started at least once.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Direct database backup failed: {ex}");
                return new BackupResult(false, $"Failed to create database backup: {ex.Message}");
            }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir, string excludeDirName = null)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                try
                {
                    using (var src = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var dst = new FileStream(destFile, FileMode.Create, FileAccess.Write))
                    {
                        src.CopyTo(dst);
                    }
                }
                catch { }
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(dir);
                if (!string.IsNullOrEmpty(excludeDirName) && dirName.Equals(excludeDirName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                string destSubDir = Path.Combine(destinationDir, dirName);
                CopyDirectory(dir, destSubDir, excludeDirName);
            }
        }
    }
}

