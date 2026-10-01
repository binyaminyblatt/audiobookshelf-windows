using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Audiobookshelf.Common;

namespace AudiobookshelfTray
{
    public class InstallServiceDialog : Form
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, out IntPtr phToken);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private Label _labelHeader;
        private Label _labelNote;
        private Label _labelUser;
        private TextBox _textBoxUser;
        private Label _labelPass;
        private TextBox _textBoxPass;
        private Button _buttonInstall;
        private Button _buttonCancel;
        private ProgressBar _progressBar;
        private Label _labelStatus;

        public InstallServiceDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Install Audiobookshelf Windows Service";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new Size(460, 295);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            _labelHeader = new Label
            {
                Text = "Install Background Windows Service",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Location = new Point(16, 14),
                AutoSize = true
            };

            _labelNote = new Label
            {
                Text = "Windows account credentials (Username & Password) are required so the service can authenticate and mount network drive shares (e.g. Z:\\) on system startup.",
                Location = new Point(18, 42),
                Size = new Size(424, 42),
                ForeColor = Color.FromArgb(75, 85, 99)
            };

            _labelUser = new Label
            {
                Text = "Windows Username (e.g. .\\Username or DOMAIN\\Username):",
                Location = new Point(18, 92),
                AutoSize = true
            };

            _textBoxUser = new TextBox
            {
                Location = new Point(20, 114),
                Size = new Size(420, 25),
                Text = ".\\" + Environment.UserName
            };

            _labelPass = new Label
            {
                Text = "Windows Password:",
                Location = new Point(18, 148),
                AutoSize = true
            };

            _textBoxPass = new TextBox
            {
                Location = new Point(20, 170),
                Size = new Size(420, 25),
                UseSystemPasswordChar = true
            };

            _progressBar = new ProgressBar
            {
                Location = new Point(20, 208),
                Size = new Size(420, 10),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            _labelStatus = new Label
            {
                Text = "",
                Location = new Point(20, 222),
                Size = new Size(420, 20),
                ForeColor = Color.FromArgb(37, 99, 235),
                Visible = false
            };

            _buttonInstall = new Button
            {
                Text = "Install & Start Service",
                DialogResult = DialogResult.None,
                Location = new Point(210, 250),
                Size = new Size(150, 32),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            _buttonInstall.Click += ButtonInstall_Click;

            _buttonCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(368, 250),
                Size = new Size(72, 32)
            };

            this.Controls.Add(_labelHeader);
            this.Controls.Add(_labelNote);
            this.Controls.Add(_labelUser);
            this.Controls.Add(_textBoxUser);
            this.Controls.Add(_labelPass);
            this.Controls.Add(_textBoxPass);
            this.Controls.Add(_progressBar);
            this.Controls.Add(_labelStatus);
            this.Controls.Add(_buttonInstall);
            this.Controls.Add(_buttonCancel);

            this.AcceptButton = _buttonInstall;
            this.CancelButton = _buttonCancel;
        }

        private async void ButtonInstall_Click(object sender, EventArgs e)
        {
            string username = _textBoxUser.Text.Trim();
            string password = _textBoxPass.Text;

            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show(
                    "Please enter your Windows username.",
                    "Username Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                _textBoxUser.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show(
                    "Please enter your Windows password.\n\nWindows account credentials are required so the background service can authenticate and mount network drives on startup.",
                    "Password Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                _textBoxPass.Focus();
                return;
            }

            // Validate credentials against Windows LSA
            string domain = ".";
            string user = username;
            int slashPos = username.IndexOf('\\');
            if (slashPos >= 0)
            {
                domain = username.Substring(0, slashPos);
                user = username.Substring(slashPos + 1);
            }

            IntPtr hToken;
            // LOGON32_LOGON_NETWORK = 3, LOGON32_PROVIDER_DEFAULT = 0
            bool valid = LogonUser(user, domain, password, 3, 0, out hToken);
            if (!valid)
            {
                // LOGON32_LOGON_INTERACTIVE = 2
                valid = LogonUser(user, domain, password, 2, 0, out hToken);
            }

            if (valid && hToken != IntPtr.Zero)
            {
                CloseHandle(hToken);
            }
            else
            {
                MessageBox.Show(
                    "Windows authentication failed: The username or password entered is incorrect.\n\nPlease verify your Windows login credentials and try again.",
                    "Authentication Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                _textBoxPass.SelectAll();
                _textBoxPass.Focus();
                return;
            }

            _buttonInstall.Enabled = false;
            _buttonCancel.Enabled = false;
            _textBoxUser.Enabled = false;
            _textBoxPass.Enabled = false;
            _progressBar.Visible = true;
            _labelStatus.Visible = true;
            _labelStatus.Text = "Installing Windows Service (Administrator permission required)...";

            string err = string.Empty;
            bool success = false;

            await System.Threading.Tasks.Task.Run(() =>
            {
                success = ServiceControllerHelper.InstallAndStartService(username, password, out err);
            });

            _progressBar.Visible = false;

            if (success)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                _buttonInstall.Enabled = true;
                _buttonCancel.Enabled = true;
                _textBoxUser.Enabled = true;
                _textBoxPass.Enabled = true;
                _labelStatus.Text = "Installation failed.";
                _labelStatus.ForeColor = Color.Red;

                MessageBox.Show(
                    $"Failed to install Audiobookshelf Windows Service:\n\n{err}",
                    "Service Installation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}
