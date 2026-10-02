using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

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

        private readonly Icon _healthyIcon;
        private readonly Icon _unhealthyIcon;
        private readonly NotifyIcon _trayIcon;
        private readonly ToolStripMenuItem _stopServerMenuItem;
        private readonly ToolStripMenuItem _startServerMenuItem;
        private readonly ToolStripMenuItem _restartServerMenuItem;
        private readonly ToolStripMenuItem _openServerMenuItem;
        private readonly ToolStripMenuItem _serverLogsMenuItem;
        private readonly ToolStripMenuItem _openDataFolderMenuItem;
        private readonly ToolStripMenuItem _openLogsFolderMenuItem;
        private readonly ToolStripMenuItem _backupNowMenuItem;
        private readonly ToolStripMenuItem _aboutMenuItem;
        private readonly ToolStripMenuItem _startAtLoginCheckboxMenuItem;
        private readonly ToolStripMenuItem _autoCheckForUpdatesCheckboxMenuItem;
        private readonly ToolStripMenuItem _settingsMenuItem;
        private readonly ToolStripMenuItem _checkForUpdatesMenuItem;

        private DismissableMessageBox _newVersionAvailableDialog = null;
        private bool _isServerHealthy = false;
        private bool _hasOpenedBrowserOnStartup = false;

        public AppTray()
        {
            var settings = SettingsHandler.Load();

            _healthyIcon = CreateBadgedIcon(Resources.AppIcon, Color.FromArgb(46, 204, 113)); // Bright Emerald Green
            _unhealthyIcon = CreateBadgedIcon(Resources.AppIcon, Color.FromArgb(231, 76, 60)); // Crimson Red

            _stopServerMenuItem = new ToolStripMenuItem("Stop Service", null, StopServiceClicked) { Enabled = false };
            _startServerMenuItem = new ToolStripMenuItem("Start Service", null, StartServiceClicked) { Enabled = false };
            _restartServerMenuItem = new ToolStripMenuItem("Restart Service", null, RestartServiceClicked) { Enabled = false };
            _serverLogsMenuItem = new ToolStripMenuItem("Server Logs", null, ShowServerLogsClicked) { Enabled = true };
            _openServerMenuItem = new ToolStripMenuItem("Open Audiobookshelf...", null, OpenClicked) { Enabled = true };
            _openServerMenuItem.Font = new Font(_openServerMenuItem.Font.Name, _openServerMenuItem.Font.Size, FontStyle.Bold);
            _openDataFolderMenuItem = new ToolStripMenuItem("Open Data Folder", null, OpenDataFolderClicked);
            _openLogsFolderMenuItem = new ToolStripMenuItem("Open Logs Folder", null, OpenLogsFolderClicked);
            _backupNowMenuItem = new ToolStripMenuItem("Backup Database Now", null, BackupNowClicked);
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
                Icon = _unhealthyIcon ?? Resources.AppIcon,
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
                        _openDataFolderMenuItem,
                        _openLogsFolderMenuItem,
                        _backupNowMenuItem,
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

        private static Icon CreateBadgedIcon(Icon baseIcon, Color dotColor)
        {
            if (baseIcon == null) return null;

            try
            {
                int width = baseIcon.Width > 0 ? baseIcon.Width : 32;
                int height = baseIcon.Height > 0 ? baseIcon.Height : 32;

                using (Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                        // Draw base icon
                        g.DrawIcon(baseIcon, new Rectangle(0, 0, width, height));

                        // Dot size and position in bottom-right corner
                        int dotSize = Math.Max(5, (int)(width * 0.36f));
                        int dotX = width - dotSize - 1;
                        int dotY = height - dotSize - 1;

                        // Dark outline for contrast across dark/light taskbars
                        using (Brush outlineBrush = new SolidBrush(Color.FromArgb(230, 20, 20, 20)))
                        {
                            g.FillEllipse(outlineBrush, dotX - 1, dotY - 1, dotSize + 2, dotSize + 2);
                        }

                        // Colored status dot
                        using (Brush dotBrush = new SolidBrush(dotColor))
                        {
                            g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
                        }
                    }

                    IntPtr hIcon = bmp.GetHicon();
                    try
                    {
                        using (Icon tempIcon = Icon.FromHandle(hIcon))
                        {
                            return (Icon)tempIcon.Clone();
                        }
                    }
                    finally
                    {
                        DestroyIcon(hIcon);
                    }
                }
            }
            catch
            {
                return baseIcon;
            }
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

            // Timer for checking service / server status & health
            _statusTimer.Interval = 2500;
            _statusTimer.Tick += (s, e) => CheckHealthAndUpdateUI();
            _statusTimer.Start();

            CheckHealthAndUpdateUI();

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

        private void SetTrayText(string text)
        {
            if (string.IsNullOrEmpty(text)) text = _trayAppName;
            _trayIcon.Text = text.Length > 63 ? text.Substring(0, 63) : text;
        }

        private async void CheckHealthAndUpdateUI()
        {
            if (_isOperatingService) return;

            var settings = SettingsHandler.Load();
            string port = string.IsNullOrWhiteSpace(settings.ServerPort) ? "13378" : settings.ServerPort;

            bool isInstalled = ServiceControllerHelper.IsServiceInstalled();
            bool isProcessOrServiceRunning = isInstalled
                ? (ServiceControllerHelper.GetServiceStatus() == ServiceControllerStatus.Running)
                : IsLocalServerProcessRunning();

            if (isProcessOrServiceRunning)
            {
                _isServerHealthy = await ServerHealthHelper.CheckServerHealthAsync(port);
                if (_isServerHealthy && settings.OpenBrowserOnStartup && !_hasOpenedBrowserOnStartup)
                {
                    _hasOpenedBrowserOnStartup = true;
                    OpenBrowser();
                }
            }
            else
            {
                _isServerHealthy = false;
            }

            UpdateServiceStatusUI();
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

            _trayIcon.Icon = _isServerHealthy ? _healthyIcon : _unhealthyIcon;

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
                    string healthTag = _isServerHealthy ? "Ready" : "Initializing...";
                    SetTrayText($"Audiobookshelf - Service Running ({healthTag})");
                }
                else if (status == ServiceControllerStatus.Stopped)
                {
                    _startServerMenuItem.Enabled = true;
                    _stopServerMenuItem.Enabled = false;
                    _restartServerMenuItem.Enabled = false;
                    SetTrayText("Audiobookshelf - Service Stopped");
                }
                else if (status == ServiceControllerStatus.StartPending)
                {
                    _startServerMenuItem.Enabled = false;
                    _stopServerMenuItem.Enabled = false;
                    _restartServerMenuItem.Enabled = false;
                    SetTrayText("Audiobookshelf - Starting Service...");
                }
                else if (status == ServiceControllerStatus.StopPending)
                {
                    _startServerMenuItem.Enabled = false;
                    _stopServerMenuItem.Enabled = false;
                    _restartServerMenuItem.Enabled = false;
                    SetTrayText("Audiobookshelf - Stopping Service...");
                }
                else
                {
                    _startServerMenuItem.Enabled = false;
                    _stopServerMenuItem.Enabled = false;
                    _restartServerMenuItem.Enabled = false;
                    string statusText = status.HasValue ? status.ToString() : "Unknown";
                    SetTrayText($"Audiobookshelf - {statusText}");
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
                    string healthTag = _isServerHealthy ? "Ready" : "Initializing...";
                    SetTrayText($"Audiobookshelf - Server Running ({healthTag})");
                }
                else
                {
                    _startServerMenuItem.Enabled = true;
                    _stopServerMenuItem.Enabled = false;
                    _restartServerMenuItem.Enabled = false;
                    SetTrayText("Audiobookshelf - Server Stopped");
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

        private void OpenDataFolderClicked(object sender, EventArgs e)
        {
            try
            {
                var settings = SettingsHandler.Load();
                string dataDir = string.IsNullOrWhiteSpace(settings.DataDir) ? Audiobookshelf.Common.Settings.GetDefaultDataDir() : settings.DataDir;
                if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
                Process.Start("explorer.exe", dataDir);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to open data folder: {ex.Message}");
            }
        }

        private void OpenLogsFolderClicked(object sender, EventArgs e)
        {
            try
            {
                var settings = SettingsHandler.Load();
                string dataDir = string.IsNullOrWhiteSpace(settings.DataDir) ? Audiobookshelf.Common.Settings.GetDefaultDataDir() : settings.DataDir;
                string logsDir = Path.Combine(dataDir, "logs");
                if (!Directory.Exists(logsDir)) Directory.CreateDirectory(logsDir);
                Process.Start("explorer.exe", logsDir);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to open logs folder: {ex.Message}");
            }
        }

        private async void BackupNowClicked(object sender, EventArgs e)
        {
            var settings = SettingsHandler.Load();
            _trayIcon.ShowBalloonTip(2000, "Audiobookshelf", "Creating database backup...", ToolTipIcon.Info);
            var (success, message) = await ServerHealthHelper.TriggerBackupAsync(settings.ServerPort, settings.AdminApiKey);
            _trayIcon.ShowBalloonTip(5000, "Audiobookshelf Backup", message, success ? ToolTipIcon.Info : ToolTipIcon.Warning);
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
            _isServerHealthy = false;
            _trayIcon.Icon = _unhealthyIcon;

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
            _isServerHealthy = false;
            _trayIcon.Icon = _unhealthyIcon;

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

            _healthyIcon?.Dispose();
            _unhealthyIcon?.Dispose();

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

                if (SettingsHandler.IsNewerVersion(latestRelease.TagName, currentVersion))
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
