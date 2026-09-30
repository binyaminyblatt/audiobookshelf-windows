using System;
using System.Windows.Forms;
using Audiobookshelf.Common;

namespace AudiobookshelfTray
{
    public class DriveMapEditDialog : Form
    {
        private ComboBox _comboBoxDriveLetter;
        private TextBox _textBoxShareName;
        private TextBox _textBoxUsername;
        private TextBox _textBoxPassword;
        private Button _buttonOk;
        private Button _buttonCancel;
        private Label _labelDrive;
        private Label _labelShare;
        private Label _labelUser;
        private Label _labelPass;

        public DriveMap DriveMap { get; private set; }

        public DriveMapEditDialog(DriveMap existing = null)
        {
            InitializeComponent();

            // Populate drive letters A: to Z:
            for (char c = 'D'; c <= 'Z'; c++)
            {
                _comboBoxDriveLetter.Items.Add($"{c}:");
            }

            if (existing != null)
            {
                Text = "Edit Network Drive Mapping";
                string drive = existing.GetNormalizedDriveLetter();
                int idx = _comboBoxDriveLetter.Items.IndexOf(drive);
                if (idx >= 0) _comboBoxDriveLetter.SelectedIndex = idx;
                else if (!string.IsNullOrEmpty(drive)) _comboBoxDriveLetter.Text = drive;

                _textBoxShareName.Text = existing.ShareName ?? "";
                _textBoxUsername.Text = existing.Username ?? "";
                _textBoxPassword.Text = existing.Password ?? "";
                DriveMap = existing;
            }
            else
            {
                Text = "Add Network Drive Mapping";
                _comboBoxDriveLetter.SelectedIndex = _comboBoxDriveLetter.Items.Count - 1; // Default to Z:
                _textBoxShareName.Text = @"\\server\share";
            }
        }

        private void InitializeComponent()
        {
            this._labelDrive = new Label();
            this._labelShare = new Label();
            this._labelUser = new Label();
            this._labelPass = new Label();
            this._comboBoxDriveLetter = new ComboBox();
            this._textBoxShareName = new TextBox();
            this._textBoxUsername = new TextBox();
            this._textBoxPassword = new TextBox();
            this._buttonOk = new Button();
            this._buttonCancel = new Button();

            this.SuspendLayout();

            // Labels
            this._labelDrive.Text = "Drive Letter:";
            this._labelDrive.Location = new System.Drawing.Point(15, 15);
            this._labelDrive.AutoSize = true;

            this._labelShare.Text = @"Remote Share (e.g. \\NAS\Audiobooks):";
            this._labelShare.Location = new System.Drawing.Point(15, 65);
            this._labelShare.AutoSize = true;

            this._labelUser.Text = "Username (Optional):";
            this._labelUser.Location = new System.Drawing.Point(15, 115);
            this._labelUser.AutoSize = true;

            this._labelPass.Text = "Password (Optional):";
            this._labelPass.Location = new System.Drawing.Point(15, 165);
            this._labelPass.AutoSize = true;

            // Inputs
            this._comboBoxDriveLetter.Location = new System.Drawing.Point(18, 35);
            this._comboBoxDriveLetter.Size = new System.Drawing.Size(90, 24);
            this._comboBoxDriveLetter.DropDownStyle = ComboBoxStyle.DropDownList;

            this._textBoxShareName.Location = new System.Drawing.Point(18, 85);
            this._textBoxShareName.Size = new System.Drawing.Size(340, 24);

            this._textBoxUsername.Location = new System.Drawing.Point(18, 135);
            this._textBoxUsername.Size = new System.Drawing.Size(340, 24);

            this._textBoxPassword.Location = new System.Drawing.Point(18, 185);
            this._textBoxPassword.Size = new System.Drawing.Size(340, 24);
            this._textBoxPassword.UseSystemPasswordChar = true;

            // Buttons
            this._buttonOk.Text = "OK";
            this._buttonOk.Location = new System.Drawing.Point(190, 230);
            this._buttonOk.Size = new System.Drawing.Size(80, 28);
            this._buttonOk.Click += ButtonOk_Click;

            this._buttonCancel.Text = "Cancel";
            this._buttonCancel.DialogResult = DialogResult.Cancel;
            this._buttonCancel.Location = new System.Drawing.Point(278, 230);
            this._buttonCancel.Size = new System.Drawing.Size(80, 28);

            // Form properties
            this.ClientSize = new System.Drawing.Size(380, 275);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.AcceptButton = this._buttonOk;
            this.CancelButton = this._buttonCancel;

            this.Controls.Add(this._labelDrive);
            this.Controls.Add(this._comboBoxDriveLetter);
            this.Controls.Add(this._labelShare);
            this.Controls.Add(this._textBoxShareName);
            this.Controls.Add(this._labelUser);
            this.Controls.Add(this._textBoxUsername);
            this.Controls.Add(this._labelPass);
            this.Controls.Add(this._textBoxPassword);
            this.Controls.Add(this._buttonOk);
            this.Controls.Add(this._buttonCancel);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void ButtonOk_Click(object sender, EventArgs e)
        {
            string share = _textBoxShareName.Text.Trim();
            if (string.IsNullOrWhiteSpace(share) || !share.StartsWith(@"\\"))
            {
                MessageBox.Show(@"Please enter a valid UNC share path (e.g. \\192.168.1.50\audiobooks)", "Invalid Share", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string drive = _comboBoxDriveLetter.SelectedItem?.ToString() ?? "Z:";

            if (DriveMap == null)
            {
                DriveMap = new DriveMap(share, drive, _textBoxUsername.Text.Trim(), _textBoxPassword.Text);
            }
            else
            {
                DriveMap.ShareName = share;
                DriveMap.DriveLetter = drive;
                DriveMap.Username = _textBoxUsername.Text.Trim();
                DriveMap.Password = _textBoxPassword.Text;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
