using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows.Forms;
using Audiobookshelf.Common;
using AudiobookshelfTray.Properties;
using Microsoft.Win32;
using NLog;
using Octokit;

namespace AudiobookshelfTray
{
    public class AppTray : ApplicationContext
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly string _appName = "Audiobookshelf";
        private readonly string _trayAppName = "AudiobookshelfTray";
        private readonly string _repoOwner = "binyaminyblatt";
        private readonly string _repoName = "audiobookshelf-windows";
        private readonly System.Timers.Timer _dailyTimer = new();
        private readonly System.Windows.Forms.Timer _statusTimer = new();

        private ServerLogs _serverLogsForm = null;
        private bool _shouldExit = false;
        private bool _runInstall = false;
        private bool _isOperatingService = false;
        private string _installerPath;
        private ServerMonitor _localServerMonitor = null;

        private readonly NotifyIcon _trayIcon;
        private readonly ToolStripMenuItem _stopServerMenuItem;
        private readonly ToolStripMenuItem _startServerMenuItem;
        private readonly ToolStripMenuItem _restartServerMenuItem;
        private readonly ToolStripMenuItem _openServerMenuItem;
        private readonly ToolStripMenuItem _serverLogsMenuItem;
        private readonly ToolStripMenuItem _aboutMenuItem;
        private readonly ToolStripMenuItem _startAtLoginCheckboxMenuItem;
        private readonly ToolStripMenuItem _autoCheckForUpdatesCheckboxMenuItem;
        private readonly ToolStripMenuItem _settingsMenuItem;
        private readonly ToolStripMenuItem _checkForUpdatesMenuItem;

        private DismissableMessageBox _newVersionAvailableDialog = null;

        public AppTray()
        {
            var settings = SettingsHandler.Load();

            _stopServerMenuItem = new ToolStripMenuItem("Stop Service", null, StopServiceClicked) { Enabled = false };
            _startServerMenuItem = new ToolStripMenuItem("Start Service", null, StartServiceClicked) { Enabled = false };
            _restartServerMenuItem = new ToolStripMenuItem("Restart Service", null, RestartServiceClicked) { Enabled = false };
            _serverLogsMenuItem = new ToolStripMenuItem("Server Logs", null, ShowServerLogsClicked) { Enabled = true };
            _openServerMenuItem = new ToolStripMenuItem("Open Audiobookshelf...", null, OpenClicked) { Enabled = true };
            _openServerMenuItem.Font = new Font(_openServerMenuItem.Font.Name, _openServerMenuItem.Font.Size, FontStyle.Bold);
            _aboutMenuItem = new ToolStripMenuItem("About Audiobookshelf", null, AboutClicked);

            _startAtLoginCheckboxMenuItem = new ToolStripMenuItem("Start Audiobookshelf Tray at Login") { CheckOnClick = true };
            _startAtLoginCheckboxMenuItem.CheckedChanged += StartAtLoginCheckedChanged;
            _startAtLoginCheckboxMenuItem.Checked = settings.StartAtLogin;

            _autoCheckForUpdatesCheckboxMenuItem = new ToolStripMenuItem("Automatically Check for Updates") { CheckOnClick = true };
            _autoCheckForUpdatesCheckboxMenuItem.CheckedChanged += AutoCheckForUpdatesChanged;
            _autoCheckForUpdatesCheckboxMenuItem.Checked = settings.AutoCheckForUpdates;

            _settingsMenuItem = new ToolStripMenuItem("Settings", null, SettingsClicked);
            _checkForUpdatesMenuItem = new ToolStripMenuItem("Check for Updates", null, CheckForUpdates);

            _dailyTimer.Interval = 24 * 60 * 60 * 1000; // 24 hours
            _dailyTimer.Elapsed += CheckForUpdates;

            _trayIcon = new NotifyIcon()
            {
                Icon = Resources.AppIcon,
                ContextMenuStrip = new ContextMenuStrip()
                {
                    Items = {
                        _openServerMenuItem,
                        _startAtLoginCheckboxMenuItem,
                        _autoCheckForUpdatesCheckboxMenuItem,
                        _settingsMenuItem,
                        new ToolStripSeparator(),
                        _startServerMenuItem,
                        _stopServerMenuItem,
                        _restartServerMenuItem,
                        _serverLogsMenuItem,
                        new ToolStripSeparator(),
                        _aboutMenuItem,
                        _checkForUpdatesMenuItem,
                        new ToolStripSeparator(),
                        new ToolStripMenuItem("Exit", null, ExitClicked)
                    }
                },
                Visible = true,
                Text = _appName
            };

            MainForm = new Form
            {
                Text = _trayAppName,
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized,
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                Opacity = 0,
            };
            MainForm.Load += (sender, e) => { if (_shouldExit) ExitClicked(sender, e); };

            Init();
        }

        private void Init()
        {
            _trayIcon.DoubleClick += OpenClicked;
            _trayIcon.BalloonTipClicked += BalloonTipClicked;

            System.Windows.Forms.Application.ApplicationExit += ApplicationExited;

            // If service is not installed, initialize local ServerMonitor for standalone process mode
            if (!ServiceControllerHelper.IsServiceInstalled())
            {
                _localServerMonitor = new ServerMonitor();
                if (!IsLocalServerProcessRunning())
                {
                    _logger.Info("Service not installed. Starting Audiobookshelf standalone server process...");
                    _localServerMonitor.Start();
                }
            }

            // Timer for checking service / server status
            _statusTimer.Interval = 2000;
            _statusTimer.Tick += (s, e) => UpdateServiceStatusUI();
            _statusTimer.Start();

            UpdateServiceStatusUI();

            if (_autoCheckForUpdatesCheckboxMenuItem.Checked)
            {
                _dailyTimer.Start();
            }
        }

        private bool IsLocalServerProcessRunning()
        {
            if (_localServerMonitor != null && _localServerMonitor.IsRunning) return true;
            try
            {
                return Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ServerMonitor.SERVER_BINARY)).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private void UpdateServiceStatusUI()
        {
            if (MainForm != null && MainForm.InvokeRequired)
            {
                try
                {
                    MainForm.BeginInvoke(new Action(UpdateServiceStatusUI));
                }
                catch { }
                return;
            }

            if (_isOperatingService)
            {
                return;
            }

            bool isInstalled = ServiceControllerHelper.IsServiceInstalled();
            _openServerMenuItem.Enabled = true;

            if (isInstalled)
            {
                if (_localServerMonitor != null && _localServerMonitor.IsRunning)
                {
                    _logger.Info("Service is installed; stopping local standalone server monitor.");
                    _localServerMonitor.Stop();
                }

                _startServerMenuItem.Text = "Start Service";
                _stopServerMenuItem.Text = "Stop Service";
                _restartServerMenuItem.Text = "Restart Service";

                var status = ServiceControllerHelper.GetServiceStatus();
                if (status == ServiceControllerStatus.Running)
                {
                    _startServerMenuItem.Enabled = false;
                    _stopServerMenuItem.Enabled = true;
                    _restartServerMenuItem.Enabled = true;
                    _trayIcon.Text = "Audiobookshelf - Service Running";
                }
                else if (status == ServiceControllerStatus.Stopped)
                {
                    _startServerMenuItem.Enabled = true;
                    _stopServerMenuItem.Enabled = false;
                    _restartServerMenuItem.Enabled = false;
                    _trayIcon.Text = "Audiobookshelf - Service Stopped";
                }
                else if (status == ServiceControllerStatus.StartPending)
                {
                    _startServerMenuItem.Enabled = false;
                    _stopServerMenuItem.Enabled = false;
                    _restartServerMenuItem.Enabled = false;
                    _trayIcon.Text = "Audiobookshelf - Starting Service...";
                }
                else if (status == ServiceControllerStatus.StopPending)
                {
                    _startServerMenuItem.Enabled = false;
                    _stopServerMenuItem.Enabled = false;
                    _restartServerMenuItem.Enabled = false;
                    _trayIcon.Text = "Audiobookshelf - Stopping Service...";
                }
                else
                {
                    _startServerMenuItem.Enabled = false;
                    _stopServerMenuItem.Enabled = false;
                    _restartServerMenuItem.Enabled = false;
                    string statusText = status.HasValue ? status.ToString() : "Unknown";
                    _trayIcon.Text = $"Audiobookshelf - {statusText}";
                }
            }
            else
            {
                // Standalone process mode
                _startServerMenuItem.Text = "Start Server";
                _stopServerMenuItem.Text = "Stop Server";
                _restartServerMenuItem.Text = "Restart Server";

                bool isRunning = IsLocalServerProcessRunning();
                if (isRunning)
                {
                    _startServerMenuItem.Enabled = false;
                    _stopServerMenuItem.Enabled = true;
                    _restartServerMenuItem.Enabled = true;
                    _trayIcon.Text = "Audiobookshelf - Server Running";
                }
                else
                {
                    _startServerMenuItem.Enabled = true;
                    _stopServerMenuItem.Enabled = false;
                    _restartServerMenuItem.Enabled = false;
                    _trayIcon.Text = "Audiobookshelf - Server Stopped";
                }
            }
        }

        public string GetServerPort()
        {
            return SettingsHandler.Load().ServerPort;
        }

        public void SaveServerPort(string serverPort)
        {
            var settings = SettingsHandler.Load();
            settings.ServerPort = serverPort;
            SettingsHandler.Save(settings);
        }

        public string GetServerDataDir()
        {
            return SettingsHandler.Load().DataDir;
        }

        public void SaveServerDataDir(string serverDataDir)
        {
            var settings = SettingsHandler.Load();
            settings.DataDir = serverDataDir;
            SettingsHandler.Save(settings);
        }

        private void AutoCheckForUpdatesChanged(object sender, EventArgs e)
        {
            var settings = SettingsHandler.Load();
            settings.AutoCheckForUpdates = _autoCheckForUpdatesCheckboxMenuItem.Checked;
            SettingsHandler.Save(settings);

            if (_autoCheckForUpdatesCheckboxMenuItem.Checked)
            {
                CheckForUpdates(sender, e);
                _dailyTimer.Start();
            }
            else
            {
                _dailyTimer.Stop();
            }
        }

        private void StartAtLoginCheckedChanged(object sender, EventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            var settings = SettingsHandler.Load();
            settings.StartAtLogin = _startAtLoginCheckboxMenuItem.Checked;
            SettingsHandler.Save(settings);

            if (_startAtLoginCheckboxMenuItem.Checked)
            {
                _logger.Debug("Adding tray app to startup");
                rk?.SetValue("AudiobookshelfTray", System.Windows.Forms.Application.ExecutablePath);
            }
            else
            {
                _logger.Debug("Removing tray app from startup");
                rk?.DeleteValue("AudiobookshelfTray", false);
            }
        }

        private void SettingsClicked(object sender, EventArgs e)
        {
            SettingsDialog settingsDialog = new(this);
            settingsDialog.ShowDialog();
        }

        private void AboutClicked(object sender, EventArgs e)
        {
            AboutBox aboutBox = new(this);
            aboutBox.ShowDialog();
        }

        public void StartServiceClicked(object sender, EventArgs e)
        {
            if (_isOperatingService) return;
            _isOperatingService = true;

            bool isInstalled = ServiceControllerHelper.IsServiceInstalled();
            _startServerMenuItem.Enabled = false;
            _stopServerMenuItem.Enabled = false;
            _restartServerMenuItem.Enabled = false;
            _trayIcon.Text = isInstalled ? "Audiobookshelf - Starting Service..." : "Audiobookshelf - Starting Server...";

            Task.Run(() =>
            {
                try
                {
                    if (isInstalled)
                    {
                        _logger.Info("Starting Audiobookshelf Service...");
                        bool started = ServiceControllerHelper.StartService();
                        if (started)
                        {
                            _trayIcon.ShowBalloonTip(1500, "Audiobookshelf", "Audiobookshelf Service started successfully.", ToolTipIcon.Info);
                        }
                        else
                        {
                            _trayIcon.ShowBalloonTip(3000, "Audiobookshelf", "Failed to start Audiobookshelf Service. Check Server Logs for details.", ToolTipIcon.Error);
                        }
                    }
                    else
                    {
                        _logger.Info("Starting Audiobookshelf standalone server process...");
                        if (_localServerMonitor == null)
                        {
                            _localServerMonitor = new ServerMonitor();
                        }
                        _localServerMonitor.Start();

                        // Give it up to 4 seconds to start up
                        for (int i = 0; i < 8; i++)
                        {
                            if (IsLocalServerProcessRunning()) break;
                            System.Threading.Thread.Sleep(500);
                        }

                        if (IsLocalServerProcessRunning())
                        {
                            _trayIcon.ShowBalloonTip(1500, "Audiobookshelf", "Audiobookshelf server process started.", ToolTipIcon.Info);
                        }
                        else
                        {
                            _trayIcon.ShowBalloonTip(3000, "Audiobookshelf", "Failed to start server process. Check Server Logs for details.", ToolTipIcon.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Start operation error: {ex}");
                    _trayIcon.ShowBalloonTip(3000, "Audiobookshelf", $"Error starting: {ex.Message}", ToolTipIcon.Error);
                }
                finally
                {
                    _isOperatingService = false;
                    UpdateServiceStatusUI();
                }
            });
        }

        public void StopServiceClicked(object sender, EventArgs e)
        {
            if (_isOperatingService) return;
            _isOperatingService = true;

            bool isInstalled = ServiceControllerHelper.IsServiceInstalled();
            _startServerMenuItem.Enabled = false;
            _stopServerMenuItem.Enabled = false;
            _restartServerMenuItem.Enabled = false;
            _trayIcon.Text = isInstalled ? "Audiobookshelf - Stopping Service..." : "Audiobookshelf - Stopping Server...";

            Task.Run(() =>
            {
                try
                {
                    if (isInstalled)
                    {
                        _logger.Info("Stopping Audiobookshelf Service...");
                        bool stopped = ServiceControllerHelper.StopService();
                        if (stopped)
                        {
                            _trayIcon.ShowBalloonTip(1500, "Audiobookshelf", "Audiobookshelf Service stopped.", ToolTipIcon.Info);
                        }
                        else
                        {
                            _trayIcon.ShowBalloonTip(3000, "Audiobookshelf", "Failed to stop Audiobookshelf Service.", ToolTipIcon.Error);
                        }
                    }
                    else
                    {
                        _logger.Info("Stopping Audiobookshelf standalone server process...");
                        if (_localServerMonitor != null)
                        {
                            _localServerMonitor.Stop();
                        }

                        // Ensure any remaining processes are terminated
                        try
                        {
                            var procName = Path.GetFileNameWithoutExtension(ServerMonitor.SERVER_BINARY);
                            foreach (var proc in Process.GetProcessesByName(procName))
                            {
                                try
                                {
                                    proc.Kill();
                                    proc.WaitForExit(2000);
                                }
                                catch { }
                            }
                        }
                        catch { }

                        _trayIcon.ShowBalloonTip(1500, "Audiobookshelf", "Audiobookshelf server stopped.", ToolTipIcon.Info);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Stop operation error: {ex}");
                    _trayIcon.ShowBalloonTip(3000, "Audiobookshelf", $"Error stopping: {ex.Message}", ToolTipIcon.Error);
                }
                finally
                {
                    _isOperatingService = false;
                    UpdateServiceStatusUI();
                }
            });
        }

        public void RestartServiceClicked(object sender, EventArgs e)
        {
            if (_isOperatingService) return;
            _isOperatingService = true;

            bool isInstalled = ServiceControllerHelper.IsServiceInstalled();
            _startServerMenuItem.Enabled = false;
            _stopServerMenuItem.Enabled = false;
            _restartServerMenuItem.Enabled = false;
            _trayIcon.Text = isInstalled ? "Audiobookshelf - Restarting Service..." : "Audiobookshelf - Restarting Server...";

            Task.Run(() =>
            {
                try
                {
                    if (isInstalled)
                    {
                        _logger.Info("Restarting Audiobookshelf Service...");
                        bool restarted = ServiceControllerHelper.RestartService();
                        if (restarted)
                        {
                            _trayIcon.ShowBalloonTip(1500, "Audiobookshelf", "Audiobookshelf Service restarted successfully.", ToolTipIcon.Info);
                        }
                        else
                        {
                            _trayIcon.ShowBalloonTip(3000, "Audiobookshelf", "Failed to restart Audiobookshelf Service. Check Server Logs for details.", ToolTipIcon.Error);
                        }
                    }
                    else
                    {
                        _logger.Info("Restarting Audiobookshelf standalone server process...");
                        if (_localServerMonitor == null)
                        {
                            _localServerMonitor = new ServerMonitor();
                        }
                        _localServerMonitor.Restart();

                        // Give it up to 4 seconds to start up
                        for (int i = 0; i < 8; i++)
                        {
                            if (IsLocalServerProcessRunning()) break;
                            System.Threading.Thread.Sleep(500);
                        }

                        if (IsLocalServerProcessRunning())
                        {
                            _trayIcon.ShowBalloonTip(1500, "Audiobookshelf", "Audiobookshelf server process restarted.", ToolTipIcon.Info);
                        }
                        else
                        {
                            _trayIcon.ShowBalloonTip(3000, "Audiobookshelf", "Failed to restart server process. Check Server Logs for details.", ToolTipIcon.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Restart operation error: {ex}");
                    _trayIcon.ShowBalloonTip(3000, "Audiobookshelf", $"Error restarting: {ex.Message}", ToolTipIcon.Error);
                }
                finally
                {
                    _isOperatingService = false;
                    UpdateServiceStatusUI();
                }
            });
        }

        public void OpenClicked(object sender, EventArgs e)
        {
            bool isInstalled = ServiceControllerHelper.IsServiceInstalled();

            if (isInstalled)
            {
                var status = ServiceControllerHelper.GetServiceStatus();
                if (status == ServiceControllerStatus.Running)
                {
                    OpenBrowser();
                }
                else
                {
                    if (MessageBox.Show("The Audiobookshelf Service is not currently running.\nDo you want to start it now?",
                        "Audiobookshelf", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        _isOperatingService = true;
                        _startServerMenuItem.Enabled = false;
                        _stopServerMenuItem.Enabled = false;
                        _restartServerMenuItem.Enabled = false;
                        _trayIcon.Text = "Audiobookshelf - Starting Service...";

                        Task.Run(() =>
                        {
                            try
                            {
                                if (ServiceControllerHelper.StartService())
                                {
                                    OpenBrowser();
                                }
                                else
                                {
                                    MessageBox.Show("Could not start Audiobookshelf Service. Please check the logs.", "Audiobookshelf", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            finally
                            {
                                _isOperatingService = false;
                                UpdateServiceStatusUI();
                            }
                        });
                    }
                }
            }
            else
            {
                // Standalone process mode
                bool isRunning = IsLocalServerProcessRunning();
                if (isRunning)
                {
                    OpenBrowser();
                }
                else
                {
                    if (MessageBox.Show("The Audiobookshelf Server is not currently running.\nDo you want to start it now?",
                        "Audiobookshelf", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        _isOperatingService = true;
                        _startServerMenuItem.Enabled = false;
                        _stopServerMenuItem.Enabled = false;
                        _restartServerMenuItem.Enabled = false;
                        _trayIcon.Text = "Audiobookshelf - Starting Server...";

                        Task.Run(() =>
                        {
                            try
                            {
                                if (_localServerMonitor == null)
                                {
                                    _localServerMonitor = new ServerMonitor();
                                }
                                _localServerMonitor.Start();

                                for (int i = 0; i < 8; i++)
                                {
                                    if (IsLocalServerProcessRunning()) break;
                                    System.Threading.Thread.Sleep(500);
                                }

                                if (IsLocalServerProcessRunning())
                                {
                                    OpenBrowser();
                                }
                                else
                                {
                                    MessageBox.Show("Could not start Audiobookshelf server process. Please check the logs.", "Audiobookshelf", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            finally
                            {
                                _isOperatingService = false;
                                UpdateServiceStatusUI();
                            }
                        });
                    }
                }
            }
        }

        private void OpenBrowser()
        {
            string port = GetServerPort();
            try
            {
                Process.Start($"http://localhost:{port}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to open browser: {ex}");
            }
        }

        public void BalloonTipClicked(object sender, EventArgs e)
        {
            OpenBrowser();
        }

        public void ShowServerLogsClicked(object sender, EventArgs e)
        {
            if (_serverLogsForm == null || _serverLogsForm.IsDisposed)
            {
                _serverLogsForm = new ServerLogs();
            }
            _serverLogsForm.Show();
            _serverLogsForm.BringToFront();
        }

        private void ApplicationExited(object sender, EventArgs e)
        {
            _logger.Debug("Tray application exiting...");
            _statusTimer.Stop();

            // Stop standalone server process if running under tray
            try
            {
                if (_localServerMonitor != null && _localServerMonitor.IsRunning)
                {
                    _localServerMonitor.Stop();
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error stopping local server on tray exit: {ex.Message}");
            }

            if (_runInstall)
            {
                try
                {
                    ServiceControllerHelper.StopService(10);
                }
                catch { }

                Process installerProcess = new()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        Arguments = "/SILENT",
                        FileName = _installerPath,
                        WindowStyle = ProcessWindowStyle.Normal,
                        UseShellExecute = true
                    }
                };
                installerProcess.Start();
            }
        }

        public void ExitClicked(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            System.Windows.Forms.Application.Exit();
        }

        private async void CheckForUpdates(object sender, EventArgs e)
        {
            try
            {
                var settings = SettingsHandler.Load();
                string currentVersion = settings.AppVersion;

                GitHubClient client = new(new ProductHeaderValue(_trayAppName));
                IReadOnlyList<Release> releases = await client.Repository.Release.GetAll(_repoOwner, _repoName);
                if (releases == null || releases.Count == 0) return;

                Release latestRelease = releases[0];
                _logger.Debug($"Latest release: {latestRelease.TagName}, Current release: {currentVersion}");

                if (_newVersionAvailableDialog != null)
                {
                    _newVersionAvailableDialog.Dismiss();
                }

                if (latestRelease.TagName != currentVersion)
                {
                    ReleaseAsset exeAsset = latestRelease.Assets.FirstOrDefault(asset => asset.Name.EndsWith(".exe"));
                    if (exeAsset == null)
                    {
                        _logger.Error("No exe asset found in release");
                        if (sender == _checkForUpdatesMenuItem)
                        {
                            MessageBox.Show("Failed to find installer for the latest release.", "Audiobookshelf", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        return;
                    }

                    _newVersionAvailableDialog = new DismissableMessageBox("Audiobookshelf Update");
                    DialogResult result = _newVersionAvailableDialog.Show($"A new version {latestRelease.TagName} is available.\nDownload and install it?", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    _newVersionAvailableDialog = null;

                    if (result == DialogResult.Yes)
                    {
                        string tempDir = Path.Combine(Path.GetTempPath(), "Audiobookshelf");
                        Directory.CreateDirectory(tempDir);
                        _installerPath = Path.Combine(tempDir, exeAsset.Name);

                        if (await DownloadInstaller(exeAsset.BrowserDownloadUrl))
                        {
                            MessageBox.Show("About to install new version. Audiobookshelf will exit now.", "Audiobookshelf", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            _runInstall = true;
                            ExitClicked(sender, e);
                        }
                        else
                        {
                            MessageBox.Show("Failed to download installer.", "Audiobookshelf", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    if (sender == _checkForUpdatesMenuItem)
                    {
                        MessageBox.Show("Audiobookshelf is up to date!", "Audiobookshelf", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Check for updates error: {ex}");
            }
        }

        private async Task<bool> DownloadInstaller(string downloadUrl)
        {
            _logger.Debug("Downloading installer to " + _installerPath);
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                using (var response = await httpClient.GetAsync(downloadUrl))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(_installerPath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                        {
                            await stream.CopyToAsync(fileStream);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to download installer: {ex}");
            }
            return false;
        }
    }
}
