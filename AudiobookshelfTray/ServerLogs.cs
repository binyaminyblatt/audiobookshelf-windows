using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using NLog;

namespace AudiobookshelfTray
{
    public partial class ServerLogs : Form
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly Timer _logTailTimer = new Timer();
        private long _lastFilePosition = 0;
        private string _logFilePath;

        public ServerLogs()
        {
            InitializeComponent();

            selectAllToolStripMenuItem.Click += new EventHandler(SelectAllToolStripMenuItem_Click);
            copySelectedToolStripMenuItem.Click += new EventHandler(CopySelectedToolStripMenuItem_Click);

            var settings = Audiobookshelf.Common.SettingsHandler.Load();
            string dataDir = string.IsNullOrWhiteSpace(settings.DataDir)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Audiobookshelf")
                : settings.DataDir;
            _logFilePath = Path.Combine(dataDir, "logs", "server.log");

            LoadInitialLogs();

            _logTailTimer.Interval = 1000;
            _logTailTimer.Tick += (s, e) => TailLogFile();
            _logTailTimer.Start();

            FormClosing += (s, e) => _logTailTimer.Stop();
        }

        private void LoadInitialLogs()
        {
            try
            {
                if (!File.Exists(_logFilePath))
                {
                    // If server.log doesn't exist yet, check service.log
                    string serviceLog = Path.Combine(Path.GetDirectoryName(_logFilePath), "service.log");
                    if (File.Exists(serviceLog))
                    {
                        _logFilePath = serviceLog;
                    }
                    else
                    {
                        logsListBox.Items.Add("Waiting for log output...");
                        return;
                    }
                }

                using (var fs = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs, Encoding.UTF8))
                {
                    string content = reader.ReadToEnd();
                    _lastFilePosition = fs.Position;

                    var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    logsListBox.BeginUpdate();
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            logsListBox.Items.Add(line);
                        }
                    }
                    if (logsListBox.Items.Count > 0)
                    {
                        logsListBox.SelectedIndex = logsListBox.Items.Count - 1;
                    }
                    logsListBox.EndUpdate();
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error reading initial logs: {ex.Message}");
            }
        }

        private void TailLogFile()
        {
            try
            {
                if (!File.Exists(_logFilePath)) return;

                var fileInfo = new FileInfo(_logFilePath);
                if (fileInfo.Length < _lastFilePosition)
                {
                    // File rotated or truncated
                    _lastFilePosition = 0;
                    logsListBox.Items.Clear();
                }

                if (fileInfo.Length > _lastFilePosition)
                {
                    using (var fs = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        fs.Seek(_lastFilePosition, SeekOrigin.Begin);
                        using (var reader = new StreamReader(fs, Encoding.UTF8))
                        {
                            string newContent = reader.ReadToEnd();
                            _lastFilePosition = fs.Position;

                            var lines = newContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                            logsListBox.BeginUpdate();
                            foreach (var line in lines)
                            {
                                if (!string.IsNullOrEmpty(line))
                                {
                                    logsListBox.Items.Add(line);
                                    if (logsListBox.Items.Count > 3000)
                                    {
                                        logsListBox.Items.RemoveAt(0);
                                    }
                                }
                            }
                            if (logsListBox.Items.Count > 0)
                            {
                                logsListBox.SelectedIndex = logsListBox.Items.Count - 1;
                            }
                            logsListBox.EndUpdate();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Trace($"Error tailing log file: {ex.Message}");
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

            logsListBox.Items.Add(line);
            logsListBox.SelectedIndex = logsListBox.Items.Count - 1;
        }

        private void CopySelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new();
            foreach (string s in logsListBox.SelectedItems)
            {
                sb.AppendLine(s);
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
    }
}
