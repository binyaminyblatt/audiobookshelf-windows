using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Audiobookshelf.Common;
using NLog;

namespace AudiobookshelfService
{
    public class ServerMonitor : IDisposable
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly Logger _serverLogger = LogManager.GetLogger("Server");

        private const string SERVER_BINARY = "audiobookshelf.exe";
        private Process _serverProcess;
        private bool _isStopping = false;
        private bool _isUpdating = false;
        private readonly object _lock = new object();
        private CancellationTokenSource _watchdogCts;
        private UpdateManager _updateManager;

        public bool IsRunning => _serverProcess != null && !_serverProcess.HasExited;

        public void Start()
        {
            lock (_lock)
            {
                _isStopping = false;
                _isUpdating = false;
                _watchdogCts = new CancellationTokenSource();

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string serverExePath = Path.Combine(baseDir, SERVER_BINARY);

                // Initialize scheduled midnight self-updater with process liveness checker
                _updateManager?.Dispose();
                _updateManager = new UpdateManager(serverExePath, StopForUpdateAsync, StartAfterUpdate, () => IsRunning);

                Task.Run(() => RunStartupSequence(_watchdogCts.Token));
            }
        }

        public async Task StopForUpdateAsync()
        {
            _logger.Info("Stopping Audiobookshelf server for update installation...");
            Process procToStop = null;
            lock (_lock)
            {
                _isUpdating = true;
                procToStop = _serverProcess;
            }

            if (procToStop != null && !procToStop.HasExited)
            {
                try
                {
                    ProcessUtils.StopProcess(procToStop);

                    // Wait up to 15 seconds for process to exit cleanly
                    for (int i = 0; i < 30; i++)
                    {
                        if (procToStop.HasExited) break;
                        await Task.Delay(500);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error stopping server for update: {ex}");
                }
                finally
                {
                    lock (_lock)
                    {
                        _serverProcess = null;
                    }
                }
            }
        }

        public void StartAfterUpdate()
        {
            _logger.Info("Restarting Audiobookshelf server after update installation...");
            lock (_lock)
            {
                _isUpdating = false;
                _isStopping = false;
                var settings = SettingsHandler.Load();
                StartServerProcess(settings);
            }
        }

        private void RunStartupSequence(CancellationToken cancellationToken)
        {
            try
            {
                _logger.Info("Starting Audiobookshelf server supervision sequence...");

                var settings = SettingsHandler.Load();

                // 1. Handle Drive Mappings if configured
                bool allDrivesMapped = true;
                if (settings.DriveMaps != null && settings.DriveMaps.Count > 0)
                {
                    _logger.Info($"Configured drive mappings detected ({settings.DriveMaps.Count}). Mapping network drives...");

                    foreach (var driveMap in settings.DriveMaps)
                    {
                        if (cancellationToken.IsCancellationRequested) return;

                        int retries = settings.AutoRemount ? settings.AutoRemountCount : 1;
                        int delay = settings.AutoRemountDelay;

                        _logger.Info($"Attempting to map '{driveMap.DriveLetter}' -> '{driveMap.ShareName}'...");
                        bool success = driveMap.TryMap(retries, delay);

                        if (!success)
                        {
                            _logger.Warn($"Failed to map drive '{driveMap.DriveLetter}' to '{driveMap.ShareName}'.");
                            allDrivesMapped = false;
                        }
                        else
                        {
                            _logger.Info($"Drive '{driveMap.DriveLetter}' mapped successfully.");
                        }
                    }
                }

                if (!allDrivesMapped && !settings.StartServerOnMountFail)
                {
                    _logger.Error("One or more drive mappings failed, and StartServerOnMountFail is false. Server will not be started.");
                    return;
                }

                if (cancellationToken.IsCancellationRequested) return;

                // 2. Launch Audiobookshelf server
                StartServerProcess(settings);
            }
            catch (Exception ex)
            {
                _logger.Error($"Exception in startup sequence: {ex}");
            }
        }

        private void StartServerProcess(Settings settings)
        {
            lock (_lock)
            {
                if (_isStopping || _isUpdating) return;

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string serverExePath = Path.Combine(baseDir, SERVER_BINARY);

                if (!File.Exists(serverExePath))
                {
                    _logger.Error($"Server executable not found at: {serverExePath}");
                    return;
                }

                string dataDir = string.IsNullOrWhiteSpace(settings.DataDir) 
                    ? Settings.GetDefaultDataDir() 
                    : settings.DataDir;

                string configPath = Path.Combine(dataDir, "config");
                string metadataPath = Path.Combine(dataDir, "metadata");
                string logsPath = Path.Combine(dataDir, "logs");

                Directory.CreateDirectory(configPath);
                Directory.CreateDirectory(metadataPath);
                Directory.CreateDirectory(logsPath);

                string port = string.IsNullOrWhiteSpace(settings.ServerPort) ? "13378" : settings.ServerPort;
                string arguments = $"-p {port} --config \"{configPath}\" --metadata \"{metadataPath}\" --source windows";

                _logger.Info($"Launching {SERVER_BINARY} with arguments: {arguments}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = serverExePath,
                    Arguments = arguments,
                    WorkingDirectory = baseDir,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                _serverProcess = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };

                _serverProcess.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _serverLogger.Debug(e.Data);
                        if (Environment.UserInteractive)
                        {
                            Console.WriteLine(e.Data);
                        }
                    }
                };

                _serverProcess.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _serverLogger.Error(e.Data);
                        if (Environment.UserInteractive)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(e.Data);
                            Console.ResetColor();
                        }
                    }
                };

                _serverProcess.Exited += OnProcessExited;

                _serverProcess.Start();
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();

                _logger.Info($"Audiobookshelf server process started successfully (PID: {_serverProcess.Id}).");
            }
        }

        private void OnProcessExited(object sender, EventArgs e)
        {
            int exitCode = -1;
            try
            {
                if (sender is Process proc)
                {
                    exitCode = proc.ExitCode;
                }
            }
            catch { }

            _logger.Warn($"Audiobookshelf server process exited with code {exitCode}.");

            lock (_lock)
            {
                _serverProcess = null;

                if (!_isStopping && !_isUpdating)
                {
                    _logger.Info("Watchdog: Server process stopped unexpectedly. Restarting in 5 seconds...");
                    Task.Delay(5000).ContinueWith(_ =>
                    {
                        lock (_lock)
                        {
                            if (!_isStopping && !_isUpdating)
                            {
                                var settings = SettingsHandler.Load();
                                StartServerProcess(settings);
                            }
                        }
                    });
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _isStopping = true;
                _watchdogCts?.Cancel();

                _updateManager?.Dispose();
                _updateManager = null;

                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    _logger.Info($"Stopping Audiobookshelf server process (PID: {_serverProcess.Id})...");
                    try
                    {
                        ProcessUtils.StopProcess(_serverProcess);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error stopping server process: {ex}");
                    }
                    finally
                    {
                        _serverProcess = null;
                    }
                }

                _logger.Info("Audiobookshelf server supervisor stopped.");
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
