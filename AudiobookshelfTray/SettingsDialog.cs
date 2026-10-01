using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;
using Audiobookshelf.Common;

namespace AudiobookshelfTray
{
    public partial class SettingsDialog : Form
    {
        private readonly AppTray _app;
        private readonly Settings _originalSettings;
        private readonly List<DriveMap> _workingDriveMaps = new List<DriveMap>();

        private Panel _drivesBanner = null;
        private Panel _updatesBanner = null;

        public SettingsDialog(AppTray app)
        {
            InitializeComponent();

            _app = app;
            _originalSettings = SettingsHandler.Load();

            // Populate Server Tab
            textBoxPort.Text = _originalSettings.ServerPort;
            textBoxDataFolder.Text = _originalSettings.DataDir;

            // Populate Drives Tab
            if (_originalSettings.DriveMaps != null)
            {
                foreach (var dm in _originalSettings.DriveMaps)
                {
                    _workingDriveMaps.Add(new DriveMap(dm.ShareName, dm.DriveLetter, dm.Username, dm.Password));
                }
            }
            RefreshDriveListView();

            // Populate Remount Options
            checkBoxAutoRemount.Checked = _originalSettings.AutoRemount;
            numericUpDownRetryCount.Value = Math.Max(1, Math.Min(30, _originalSettings.AutoRemountCount));
            numericUpDownRetryDelay.Value = Math.Max(1, Math.Min(60, _originalSettings.AutoRemountDelay));
            checkBoxStartOnFail.Checked = _originalSettings.StartServerOnMountFail;

            // Populate Updates & Backups Tab
            textBoxApiKey.Text = _originalSettings.AdminApiKey;
            checkBoxGitHubUpdates.Checked = _originalSettings.AutoUpdateFromGitHub;
            checkBoxFolderUpdates.Checked = _originalSettings.AutoApplyFolderUpdates;
            textBoxUpdatesFolder.Text = Settings.GetDefaultUpdatesDir();

            // If Windows Service is not installed, disable Drive Mappings and Updates tabs and show Install Service button
            bool isServiceInstalled = ServiceControllerHelper.IsServiceInstalled();
            if (!isServiceInstalled)
            {
                tabPageDrives.Enabled = false;
                tabPageUpdates.Enabled = false;

                _drivesBanner = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 44,
                    BackColor = System.Drawing.Color.FromArgb(254, 243, 199),
                    Padding = new Padding(10, 6, 10, 6)
                };

                var btnInstallDrives = new Button
                {
                    Text = "Install Service...",
                    Dock = DockStyle.Right,
                    Width = 125,
                    Font = new Font(this.Font, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    UseVisualStyleBackColor = true
                };
                btnInstallDrives.Click += ButtonInstallService_Click;

                var drivesLabel = new Label
                {
                    Text = "⚠️ Network drive mapping requires Audiobookshelf to be installed as a Windows Service.",
                    Dock = DockStyle.Fill,
                    ForeColor = System.Drawing.Color.FromArgb(146, 64, 14),
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold)
                };

                _drivesBanner.Controls.Add(drivesLabel);
                _drivesBanner.Controls.Add(btnInstallDrives);
                tabPageDrives.Controls.Add(_drivesBanner);
                _drivesBanner.BringToFront();

                _updatesBanner = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 44,
                    BackColor = System.Drawing.Color.FromArgb(254, 243, 199),
                    Padding = new Padding(10, 6, 10, 6)
                };

                var btnInstallUpdates = new Button
                {
                    Text = "Install Service...",
                    Dock = DockStyle.Right,
                    Width = 125,
                    Font = new Font(this.Font, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    UseVisualStyleBackColor = true
                };
                btnInstallUpdates.Click += ButtonInstallService_Click;

                var updatesLabel = new Label
                {
                    Text = "⚠️ Automated background updates and backups require the Audiobookshelf Windows Service.",
                    Dock = DockStyle.Fill,
                    ForeColor = System.Drawing.Color.FromArgb(146, 64, 14),
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold)
                };

                _updatesBanner.Controls.Add(updatesLabel);
                _updatesBanner.Controls.Add(btnInstallUpdates);
                tabPageUpdates.Controls.Add(_updatesBanner);
                _updatesBanner.BringToFront();
            }
        }

        private void ButtonInstallService_Click(object sender, EventArgs e)
        {
            using (var dlg = new InstallServiceDialog())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    tabPageDrives.Enabled = true;
                    tabPageUpdates.Enabled = true;

                    if (_drivesBanner != null)
                    {
                        tabPageDrives.Controls.Remove(_drivesBanner);
                        _drivesBanner.Dispose();
                        _drivesBanner = null;
                    }

                    if (_updatesBanner != null)
                    {
                        tabPageUpdates.Controls.Remove(_updatesBanner);
                        _updatesBanner.Dispose();
                        _updatesBanner = null;
                    }

                    MessageBox.Show(
                        "Audiobookshelf Windows Service installed and started successfully!\n\nDrive mapping and automated updates are now enabled.",
                        "Audiobookshelf Service Installed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
        }

        private void RefreshDriveListView()
        {
            listViewDrives.BeginUpdate();
            listViewDrives.Items.Clear();
            foreach (var dm in _workingDriveMaps)
            {
                var item = new ListViewItem(dm.GetNormalizedDriveLetter());
                item.SubItems.Add(dm.ShareName ?? "");
                item.SubItems.Add(string.IsNullOrWhiteSpace(dm.Username) ? "(integrated)" : dm.Username);
                item.Tag = dm;
                listViewDrives.Items.Add(item);
            }
            listViewDrives.EndUpdate();
        }

        private void BrowseClicked(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new()
            {
                Description = "Select the folder where your server configuration and metadata should be stored.",
                SelectedPath = textBoxDataFolder.Text
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBoxDataFolder.Text = dialog.SelectedPath;
            }
        }

        private void ButtonAddDrive_Click(object sender, EventArgs e)
        {
            using (var dlg = new DriveMapEditDialog())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.DriveMap != null)
                {
                    _workingDriveMaps.Add(dlg.DriveMap);
                    RefreshDriveListView();
                }
            }
        }

        private void ButtonEditDrive_Click(object sender, EventArgs e)
        {
            if (listViewDrives.SelectedItems.Count == 0) return;

            var selected = listViewDrives.SelectedItems[0].Tag as DriveMap;
            if (selected == null) return;

            using (var dlg = new DriveMapEditDialog(selected))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshDriveListView();
                }
            }
        }

        private void ButtonRemoveDrive_Click(object sender, EventArgs e)
        {
            if (listViewDrives.SelectedItems.Count == 0) return;

            var selected = listViewDrives.SelectedItems[0].Tag as DriveMap;
            if (selected != null)
            {
                if (MessageBox.Show($"Remove drive mapping for {selected.GetNormalizedDriveLetter()} -> {selected.ShareName}?",
                    "Confirm Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    DriveMap.SetDriveHidden(selected.DriveLetter, false);
                    _workingDriveMaps.Remove(selected);
                    RefreshDriveListView();
                }
            }
        }

        private void ButtonTestDrive_Click(object sender, EventArgs e)
        {
            if (listViewDrives.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a drive mapping to test.", "Test Drive Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selected = listViewDrives.SelectedItems[0].Tag as DriveMap;
            if (selected == null) return;

            Cursor = Cursors.WaitCursor;
            try
            {
                bool success = selected.TryMap(1, 1);
                Cursor = Cursors.Default;
                if (success)
                {
                    MessageBox.Show($"Successfully connected {selected.GetNormalizedDriveLetter()} to {selected.ShareName}!",
                        "Drive Map Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to connect {selected.GetNormalizedDriveLetter()} to {selected.ShareName}. Check server path and credentials.",
                        "Drive Map Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                MessageBox.Show($"Error testing drive map: {ex.Message}", "Drive Map Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ButtonTestApiKey_Click(object sender, EventArgs e)
        {
            string apiKey = textBoxApiKey.Text.Trim();
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Please enter an Admin API Key to test.", "Test API Key", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string port = string.IsNullOrWhiteSpace(textBoxPort.Text) ? "13378" : textBoxPort.Text.Trim();
            buttonTestApiKey.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(6) })
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                    // Test with /api/me first
                    var response = await client.GetAsync($"http://127.0.0.1:{port}/api/me");
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Admin API Key verified successfully with Audiobookshelf server!", "API Key Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    else if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        MessageBox.Show("Authentication failed (Invalid API Key). Please check your Admin API Key in the web interface.", "API Key Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Fallback to /api/libraries
                    var fallbackResp = await client.GetAsync($"http://127.0.0.1:{port}/api/libraries");
                    if (fallbackResp.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Admin API Key verified successfully with Audiobookshelf server!", "API Key Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    MessageBox.Show($"Server returned HTTP {(int)response.StatusCode}: {response.ReasonPhrase}", "API Key Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not connect to Audiobookshelf server at http://127.0.0.1:{port}.\nEnsure the Audiobookshelf service is started.\n\nError: {ex.Message}", "API Key Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                buttonTestApiKey.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void ButtonOpenUpdatesFolder_Click(object sender, EventArgs e)
        {
            try
            {
                string updatesDir = Settings.GetDefaultUpdatesDir();
                if (!Directory.Exists(updatesDir))
                {
                    Directory.CreateDirectory(updatesDir);
                }
                Process.Start("explorer.exe", updatesDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open updates folder: {ex.Message}", "Audiobookshelf", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveClicked(object sender, EventArgs e)
        {
            if (!ValidatePort() || !ValidateDataFolder())
            {
                MessageBox.Show("One of the server settings is invalid. Please correct it before saving.", "Audiobookshelf", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool isServiceInstalled = ServiceControllerHelper.IsServiceInstalled();
            string apiKey = textBoxApiKey.Text.Trim();

            if (isServiceInstalled && checkBoxGitHubUpdates.Checked && string.IsNullOrEmpty(apiKey))
            {
                var res = MessageBox.Show("GitHub auto-updates require an Admin API Key to perform pre-update backups.\nAre you sure you want to enable updates without configuring a key now?", "Admin API Key Required", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res == DialogResult.No)
                {
                    tabControl.SelectedTab = tabPageUpdates;
                    textBoxApiKey.Focus();
                    return;
                }
            }

            var newSettings = SettingsHandler.Load();
            newSettings.ServerPort = textBoxPort.Text.Trim();
            newSettings.DataDir = textBoxDataFolder.Text.Trim();

            if (isServiceInstalled)
            {
                newSettings.DriveMaps = new List<DriveMap>(_workingDriveMaps);
                newSettings.AutoRemount = checkBoxAutoRemount.Checked;
                newSettings.AutoRemountCount = (int)numericUpDownRetryCount.Value;
                newSettings.AutoRemountDelay = (int)numericUpDownRetryDelay.Value;
                newSettings.StartServerOnMountFail = checkBoxStartOnFail.Checked;
                newSettings.AdminApiKey = apiKey;
                newSettings.AutoUpdateFromGitHub = checkBoxGitHubUpdates.Checked;
                newSettings.AutoApplyFolderUpdates = checkBoxFolderUpdates.Checked;

                DriveMap.SyncHiddenDrives(_workingDriveMaps);
            }

            SettingsHandler.Save(newSettings);

            string targetName = isServiceInstalled ? "Audiobookshelf Service" : "Audiobookshelf Server";
            var result = MessageBox.Show(
                $"Settings saved successfully.\nWould you like to restart the {targetName} now to apply changes?",
                "Audiobookshelf",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                _app.RestartServiceClicked(sender, e);
            }

            Close();
        }

        private bool ValidatePort()
        {
            if (int.TryParse(textBoxPort.Text, out int portNumber))
            {
                if (portNumber >= 1 && portNumber <= 65535)
                {
                    errorProviderPort.SetError(labelPort, "");
                    return true;
                }
            }
            errorProviderPort.SetError(labelPort, "Invalid port number. Please enter a value between 1 and 65535.");
            return false;
        }

        private bool ValidateDataFolder()
        {
            string datafolder = textBoxDataFolder.Text.Trim();
            if (string.IsNullOrEmpty(datafolder))
            {
                errorProviderDataFolder.SetError(labelDataFolder, "Please enter a valid folder path.");
                return false;
            }

            try
            {
                if (!Directory.Exists(datafolder))
                {
                    Directory.CreateDirectory(datafolder);
                }
                errorProviderDataFolder.SetError(labelDataFolder, "");
                return true;
            }
            catch (Exception ex)
            {
                errorProviderDataFolder.SetError(labelDataFolder, $"Cannot access folder: {ex.Message}");
                return false;
            }
        }
    }
}
