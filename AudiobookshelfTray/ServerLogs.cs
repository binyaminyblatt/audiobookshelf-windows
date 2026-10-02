using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using NLog;

namespace AudiobookshelfTray
{
    public partial class ServerLogs : Form
    {
        private class LogSourceItem
        {
            public string DisplayName { get; }
            public string FilePath { get; }

            public LogSourceItem(string displayName, string filePath)
            {
                DisplayName = displayName;
                FilePath = filePath;
            }

            public override string ToString() => DisplayName;
        }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly Timer _logTailTimer = new Timer();
        private long _lastFilePosition = 0;
        private string _activeLogFilePath;
        private bool _isWaitingMessageShown = false;
        private string _lastDayChecked = DateTime.Now.ToString("yyyy-MM-dd");

        public ServerLogs()
        {
            InitializeComponent();

            selectAllToolStripMenuItem.Click += (s, e) => SelectAllToolStripMenuItem_Click(s, e);
            copySelectedToolStripMenuItem.Click += (s, e) => CopySelectedToolStripMenuItem_Click(s, e);
            clearViewToolStripMenuItem.Click += (s, e) => ClearViewToolStripMenuItem_Click(s, e);
            buttonRefresh.Click += (s, e) => { PopulateLogSources(); ReloadActiveLog(); };
            buttonOpenFolder.Click += (s, e) => OpenLogsFolder();
            comboBoxLogFile.SelectedIndexChanged += (s, e) => OnLogSourceChanged();

            PopulateLogSources();

            _logTailTimer.Interval = 1000;
            _logTailTimer.Tick += (s, e) => TailLogFile();
            _logTailTimer.Start();

            FormClosing += (s, e) => _logTailTimer.Stop();
        }

        private string GetDataDirectory()
        {
            var settings = Audiobookshelf.Common.SettingsHandler.Load();
            return string.IsNullOrWhiteSpace(settings.DataDir)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Audiobookshelf")
                : settings.DataDir;
        }

        private void PopulateLogSources()
        {
            string previousPath = (comboBoxLogFile.SelectedItem as LogSourceItem)?.FilePath ?? _activeLogFilePath;

            comboBoxLogFile.BeginUpdate();
            comboBoxLogFile.Items.Clear();

            string dataDir = GetDataDirectory();
            string todayStr = DateTime.Now.ToString("yyyy-MM-dd");
            _lastDayChecked = todayStr;

            // 1. Audiobookshelf native daily logs in metadata\logs\daily\
            string dailyLogsDir = Path.Combine(dataDir, "metadata", "logs", "daily");
            if (Directory.Exists(dailyLogsDir))
            {
                var dailyFiles = Directory.GetFiles(dailyLogsDir, "*.txt")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.Name)
                    .ToList();

                foreach (var fi in dailyFiles)
                {
                    bool isToday = fi.Name.StartsWith(todayStr, StringComparison.OrdinalIgnoreCase);
                    string label = isToday
                        ? $"Audiobookshelf Server (Active Daily: {fi.Name})"
                        : $"Audiobookshelf Server ({fi.Name})";
                    comboBoxLogFile.Items.Add(new LogSourceItem(label, fi.FullName));
                }
            }

            // 2. Application logs in logs\ (server.log, service.log, tray.log)
            string logsDir = Path.Combine(dataDir, "logs");
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }

            string serverLog = Path.Combine(logsDir, "server.log");
            string serviceLog = Path.Combine(logsDir, "service.log");
            string trayLog = Path.Combine(logsDir, "tray.log");

            comboBoxLogFile.Items.Add(new LogSourceItem("Audiobookshelf Console (server.log)", serverLog));
            comboBoxLogFile.Items.Add(new LogSourceItem("Audiobookshelf Windows Service (service.log)", serviceLog));
            comboBoxLogFile.Items.Add(new LogSourceItem("Audiobookshelf Tray Application (tray.log)", trayLog));

            // Determine best initial selection
            int targetIndex = 0;
            if (!string.IsNullOrEmpty(previousPath))
            {
                for (int i = 0; i < comboBoxLogFile.Items.Count; i++)
                {
                    if (comboBoxLogFile.Items[i] is LogSourceItem item &&
                        string.Equals(item.FilePath, previousPath, StringComparison.OrdinalIgnoreCase))
                    {
                        targetIndex = i;
                        break;
                    }
                }
            }

            comboBoxLogFile.EndUpdate();

            if (comboBoxLogFile.Items.Count > 0)
            {
                comboBoxLogFile.SelectedIndex = targetIndex;
            }
        }

        private void OnLogSourceChanged()
        {
            if (comboBoxLogFile.SelectedItem is LogSourceItem item)
            {
                _activeLogFilePath = item.FilePath;
                ReloadActiveLog();
            }
        }

        private void OpenLogsFolder()
        {
            try
            {
                if (!string.IsNullOrEmpty(_activeLogFilePath) && File.Exists(_activeLogFilePath))
                {
                    string dir = Path.GetDirectoryName(_activeLogFilePath);
                    if (Directory.Exists(dir))
                    {
                        Process.Start("explorer.exe", dir);
                        return;
                    }
                }

                string dataDir = GetDataDirectory();
                string dailyLogsDir = Path.Combine(dataDir, "metadata", "logs", "daily");
                if (Directory.Exists(dailyLogsDir))
                {
                    Process.Start("explorer.exe", dailyLogsDir);
                    return;
                }

                string logsDir = Path.Combine(dataDir, "logs");
                if (!Directory.Exists(logsDir)) Directory.CreateDirectory(logsDir);
                Process.Start("explorer.exe", logsDir);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to open logs directory: {ex.Message}");
            }
        }

        private static string FormatLogLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return line;

            string trimmed = line.Trim();
            if (trimmed.StartsWith("{") && trimmed.EndsWith("}") && trimmed.Contains("\"message\""))
            {
                try
                {
                    var obj = JObject.Parse(trimmed);
                    string timestamp = (string)obj["timestamp"] ?? "";
                    string levelName = (string)obj["levelName"] ?? "INFO";
                    string message = (string)obj["message"] ?? "";

                    if (!string.IsNullOrEmpty(timestamp) && !string.IsNullOrEmpty(message))
                    {
                        return $"[{timestamp}] [{levelName.ToUpperInvariant()}] {message}";
                    }
                }
                catch
                {
                    // Fallback to raw line if JSON parsing fails
                }
            }

            return line;
        }

        public void ReloadActiveLog()
        {
            _lastFilePosition = 0;
            logsListBox.BeginUpdate();
            logsListBox.Items.Clear();
            _isWaitingMessageShown = false;

            try
            {
                if (string.IsNullOrEmpty(_activeLogFilePath) || !File.Exists(_activeLogFilePath))
                {
                    string fileName = Path.GetFileName(_activeLogFilePath ?? "server.log");
                    logsListBox.Items.Add($"[Waiting for output in {fileName}...]");
                    _isWaitingMessageShown = true;
                    logsListBox.EndUpdate();
                    return;
                }

                using (var fs = new FileStream(_activeLogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(fs, Encoding.UTF8))
                {
                    string content = reader.ReadToEnd();
                    _lastFilePosition = fs.Position;

                    var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            logsListBox.Items.Add(FormatLogLine(line));
                        }
                    }

                    if (logsListBox.Items.Count > 3000)
                    {
                        int toRemove = logsListBox.Items.Count - 3000;
                        for (int i = 0; i < toRemove; i++)
                        {
                            logsListBox.Items.RemoveAt(0);
                        }
                    }

                    if (logsListBox.Items.Count > 0 && checkBoxAutoScroll.Checked)
                    {
                        logsListBox.TopIndex = Math.Max(0, logsListBox.Items.Count - 1);
                    }
                }
            }
            catch (Exception ex)
            {
                logsListBox.Items.Add($"[Error reading log file: {ex.Message}]");
                _logger.Debug($"Error reading log file {_activeLogFilePath}: {ex.Message}");
            }
            finally
            {
                logsListBox.EndUpdate();
            }
        }

        private void TailLogFile()
        {
            // Detect midnight day rollover to refresh daily log sources
            string todayStr = DateTime.Now.ToString("yyyy-MM-dd");
            if (_lastDayChecked != todayStr)
            {
                PopulateLogSources();
                return;
            }

            if (string.IsNullOrEmpty(_activeLogFilePath)) return;

            try
            {
                if (!File.Exists(_activeLogFilePath))
                {
                    if (!_isWaitingMessageShown)
                    {
                        logsListBox.Items.Clear();
                        logsListBox.Items.Add($"[Waiting for output in {Path.GetFileName(_activeLogFilePath)}...]");
                        _isWaitingMessageShown = true;
                        _lastFilePosition = 0;
                    }
                    return;
                }

                if (_isWaitingMessageShown)
                {
                    logsListBox.Items.Clear();
                    _isWaitingMessageShown = false;
                    _lastFilePosition = 0;
                }

                var fileInfo = new FileInfo(_activeLogFilePath);
                if (fileInfo.Length < _lastFilePosition)
                {
                    // File rotated or truncated
                    _lastFilePosition = 0;
                    logsListBox.Items.Clear();
                }

                if (fileInfo.Length > _lastFilePosition)
                {
                    using (var fs = new FileStream(_activeLogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    {
                        fs.Seek(_lastFilePosition, SeekOrigin.Begin);
                        using (var reader = new StreamReader(fs, Encoding.UTF8))
                        {
                            string newContent = reader.ReadToEnd();
                            _lastFilePosition = fs.Position;

                            var lines = newContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                            logsListBox.BeginUpdate();
                            bool hasNewLines = false;
                            foreach (var line in lines)
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    logsListBox.Items.Add(FormatLogLine(line));
                                    hasNewLines = true;
                                    if (logsListBox.Items.Count > 3000)
                                    {
                                        logsListBox.Items.RemoveAt(0);
                                    }
                                }
                            }

                            if (hasNewLines && logsListBox.Items.Count > 0 && checkBoxAutoScroll.Checked)
                            {
                                logsListBox.TopIndex = Math.Max(0, logsListBox.Items.Count - 1);
                            }

                            logsListBox.EndUpdate();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Trace($"Error tailing log file {_activeLogFilePath}: {ex.Message}");
            }
        }

        public void AddLogLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return;

            if (logsListBox.InvokeRequired)
            {
                logsListBox.Invoke((MethodInvoker)delegate { AddLogLine(line); });
                return;
            }

            if (_isWaitingMessageShown)
            {
                logsListBox.Items.Clear();
                _isWaitingMessageShown = false;
            }

            logsListBox.Items.Add(FormatLogLine(line));
            if (checkBoxAutoScroll.Checked)
            {
                logsListBox.TopIndex = Math.Max(0, logsListBox.Items.Count - 1);
            }
        }

        private void CopySelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in logsListBox.SelectedItems)
            {
                sb.AppendLine(item.ToString());
            }
            if (sb.Length > 0)
            {
                Clipboard.SetText(sb.ToString());
            }
        }

        private void SelectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logsListBox.BeginUpdate();
            for (int i = 0; i < logsListBox.Items.Count; i++)
            {
                logsListBox.SetSelected(i, true);
            }
            logsListBox.EndUpdate();
        }

        private void ClearViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logsListBox.Items.Clear();
        }
    }
}


