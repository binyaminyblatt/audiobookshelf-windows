namespace AudiobookshelfTray
{
    partial class SettingsDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsDialog));
            
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPageServer = new System.Windows.Forms.TabPage();
            this.groupBoxServer = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanelServer = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutDataFolder = new System.Windows.Forms.TableLayoutPanel();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.labelDataFolder = new System.Windows.Forms.Label();
            this.textBoxDataFolder = new System.Windows.Forms.TextBox();
            this.tableLayoutPanelPort = new System.Windows.Forms.TableLayoutPanel();
            this.labelPort = new System.Windows.Forms.Label();
            this.textBoxPort = new System.Windows.Forms.TextBox();
            
            this.tabPageDrives = new System.Windows.Forms.TabPage();
            this.groupBoxDrives = new System.Windows.Forms.GroupBox();
            this.listViewDrives = new System.Windows.Forms.ListView();
            this.colDrive = new System.Windows.Forms.ColumnHeader();
            this.colShare = new System.Windows.Forms.ColumnHeader();
            this.colUser = new System.Windows.Forms.ColumnHeader();
            this.panelDriveButtons = new System.Windows.Forms.Panel();
            this.buttonAddDrive = new System.Windows.Forms.Button();
            this.buttonEditDrive = new System.Windows.Forms.Button();
            this.buttonRemoveDrive = new System.Windows.Forms.Button();
            this.buttonTestDrive = new System.Windows.Forms.Button();
            this.groupBoxRemount = new System.Windows.Forms.GroupBox();
            this.checkBoxAutoRemount = new System.Windows.Forms.CheckBox();
            this.labelRetryCount = new System.Windows.Forms.Label();
            this.numericUpDownRetryCount = new System.Windows.Forms.NumericUpDown();
            this.labelRetryDelay = new System.Windows.Forms.Label();
            this.numericUpDownRetryDelay = new System.Windows.Forms.NumericUpDown();
            this.checkBoxStartOnFail = new System.Windows.Forms.CheckBox();

            this.tabPageUpdates = new System.Windows.Forms.TabPage();
            this.groupBoxApiKey = new System.Windows.Forms.GroupBox();
            this.labelApiKey = new System.Windows.Forms.Label();
            this.textBoxApiKey = new System.Windows.Forms.TextBox();
            this.buttonTestApiKey = new System.Windows.Forms.Button();
            this.labelApiKeyInfo = new System.Windows.Forms.Label();
            this.groupBoxGitHubUpdates = new System.Windows.Forms.GroupBox();
            this.checkBoxGitHubUpdates = new System.Windows.Forms.CheckBox();
            this.labelGitHubUpdatesInfo = new System.Windows.Forms.Label();
            this.groupBoxFolderUpdates = new System.Windows.Forms.GroupBox();
            this.checkBoxFolderUpdates = new System.Windows.Forms.CheckBox();
            this.labelFolderUpdatesPath = new System.Windows.Forms.Label();
            this.textBoxUpdatesFolder = new System.Windows.Forms.TextBox();
            this.buttonOpenUpdatesFolder = new System.Windows.Forms.Button();
            this.labelFolderUpdatesInfo = new System.Windows.Forms.Label();

            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonSave = new System.Windows.Forms.Button();
            this.errorProviderPort = new System.Windows.Forms.ErrorProvider(this.components);
            this.errorProviderDataFolder = new System.Windows.Forms.ErrorProvider(this.components);
            this.errorProviderApiKey = new System.Windows.Forms.ErrorProvider(this.components);

            this.tabControl.SuspendLayout();
            this.tabPageServer.SuspendLayout();
            this.groupBoxServer.SuspendLayout();
            this.tableLayoutPanelServer.SuspendLayout();
            this.tableLayoutDataFolder.SuspendLayout();
            this.tableLayoutPanelPort.SuspendLayout();
            this.tabPageDrives.SuspendLayout();
            this.groupBoxDrives.SuspendLayout();
            this.panelDriveButtons.SuspendLayout();
            this.groupBoxRemount.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownRetryCount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownRetryDelay)).BeginInit();
            this.tabPageUpdates.SuspendLayout();
            this.groupBoxApiKey.SuspendLayout();
            this.groupBoxGitHubUpdates.SuspendLayout();
            this.groupBoxFolderUpdates.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderDataFolder)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderApiKey)).BeginInit();
            this.SuspendLayout();

            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.tabPageServer);
            this.tabControl.Controls.Add(this.tabPageDrives);
            this.tabControl.Controls.Add(this.tabPageUpdates);
            this.tabControl.Location = new System.Drawing.Point(12, 12);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(618, 430);
            this.tabControl.TabIndex = 0;

            // 
            // tabPageServer
            // 
            this.tabPageServer.Controls.Add(this.groupBoxServer);
            this.tabPageServer.Location = new System.Drawing.Point(4, 25);
            this.tabPageServer.Name = "tabPageServer";
            this.tabPageServer.Padding = new System.Windows.Forms.Padding(10);
            this.tabPageServer.Size = new System.Drawing.Size(610, 401);
            this.tabPageServer.TabIndex = 0;
            this.tabPageServer.Text = "Server Settings";
            this.tabPageServer.UseVisualStyleBackColor = true;

            // 
            // groupBoxServer
            // 
            this.groupBoxServer.Controls.Add(this.tableLayoutPanelServer);
            this.groupBoxServer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxServer.Location = new System.Drawing.Point(10, 10);
            this.groupBoxServer.Name = "groupBoxServer";
            this.groupBoxServer.Size = new System.Drawing.Size(590, 381);
            this.groupBoxServer.TabIndex = 0;
            this.groupBoxServer.TabStop = false;
            this.groupBoxServer.Text = "General";

            // 
            // tableLayoutPanelServer
            // 
            this.tableLayoutPanelServer.ColumnCount = 1;
            this.tableLayoutPanelServer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelServer.Controls.Add(this.tableLayoutDataFolder, 0, 1);
            this.tableLayoutPanelServer.Controls.Add(this.tableLayoutPanelPort, 0, 0);
            this.tableLayoutPanelServer.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanelServer.Location = new System.Drawing.Point(3, 18);
            this.tableLayoutPanelServer.Name = "tableLayoutPanelServer";
            this.tableLayoutPanelServer.Padding = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanelServer.RowCount = 2;
            this.tableLayoutPanelServer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.tableLayoutPanelServer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.tableLayoutPanelServer.Size = new System.Drawing.Size(584, 170);
            this.tableLayoutPanelServer.TabIndex = 0;

            // 
            // tableLayoutDataFolder
            // 
            this.tableLayoutDataFolder.ColumnCount = 2;
            this.tableLayoutDataFolder.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutDataFolder.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutDataFolder.Controls.Add(this.buttonBrowse, 1, 1);
            this.tableLayoutDataFolder.Controls.Add(this.labelDataFolder, 0, 0);
            this.tableLayoutDataFolder.Controls.Add(this.textBoxDataFolder, 0, 1);
            this.tableLayoutDataFolder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutDataFolder.Location = new System.Drawing.Point(9, 83);
            this.tableLayoutDataFolder.Name = "tableLayoutDataFolder";
            this.tableLayoutDataFolder.RowCount = 2;
            this.tableLayoutDataFolder.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutDataFolder.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableLayoutDataFolder.Size = new System.Drawing.Size(566, 78);
            this.tableLayoutDataFolder.TabIndex = 1;

            // 
            // labelDataFolder
            // 
            this.labelDataFolder.AutoSize = true;
            this.labelDataFolder.Location = new System.Drawing.Point(3, 3);
            this.labelDataFolder.Name = "labelDataFolder";
            this.labelDataFolder.Size = new System.Drawing.Size(86, 17);
            this.labelDataFolder.TabIndex = 0;
            this.labelDataFolder.Text = "Data Folder:";

            // 
            // textBoxDataFolder
            // 
            this.textBoxDataFolder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxDataFolder.Location = new System.Drawing.Point(3, 28);
            this.textBoxDataFolder.Name = "textBoxDataFolder";
            this.textBoxDataFolder.Size = new System.Drawing.Size(460, 22);
            this.textBoxDataFolder.TabIndex = 1;

            // 
            // buttonBrowse
            // 
            this.buttonBrowse.Location = new System.Drawing.Point(469, 28);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(90, 26);
            this.buttonBrowse.TabIndex = 2;
            this.buttonBrowse.Text = "Browse...";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            this.buttonBrowse.Click += new System.EventHandler(this.BrowseClicked);

            // 
            // tableLayoutPanelPort
            // 
            this.tableLayoutPanelPort.ColumnCount = 1;
            this.tableLayoutPanelPort.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelPort.Controls.Add(this.labelPort, 0, 0);
            this.tableLayoutPanelPort.Controls.Add(this.textBoxPort, 0, 1);
            this.tableLayoutPanelPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelPort.Location = new System.Drawing.Point(9, 9);
            this.tableLayoutPanelPort.Name = "tableLayoutPanelPort";
            this.tableLayoutPanelPort.RowCount = 2;
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableLayoutPanelPort.Size = new System.Drawing.Size(566, 68);
            this.tableLayoutPanelPort.TabIndex = 0;

            // 
            // labelPort
            // 
            this.labelPort.AutoSize = true;
            this.labelPort.Location = new System.Drawing.Point(3, 3);
            this.labelPort.Name = "labelPort";
            this.labelPort.Size = new System.Drawing.Size(84, 17);
            this.labelPort.TabIndex = 0;
            this.labelPort.Text = "Server Port:";

            // 
            // textBoxPort
            // 
            this.textBoxPort.Location = new System.Drawing.Point(3, 28);
            this.textBoxPort.Name = "textBoxPort";
            this.textBoxPort.Size = new System.Drawing.Size(90, 22);
            this.textBoxPort.TabIndex = 1;

            // 
            // tabPageDrives
            // 
            this.tabPageDrives.Controls.Add(this.groupBoxDrives);
            this.tabPageDrives.Controls.Add(this.groupBoxRemount);
            this.tabPageDrives.Location = new System.Drawing.Point(4, 25);
            this.tabPageDrives.Name = "tabPageDrives";
            this.tabPageDrives.Padding = new System.Windows.Forms.Padding(10);
            this.tabPageDrives.Size = new System.Drawing.Size(610, 401);
            this.tabPageDrives.TabIndex = 1;
            this.tabPageDrives.Text = "Network Drives";
            this.tabPageDrives.UseVisualStyleBackColor = true;

            // 
            // groupBoxDrives
            // 
            this.groupBoxDrives.Controls.Add(this.listViewDrives);
            this.groupBoxDrives.Controls.Add(this.panelDriveButtons);
            this.groupBoxDrives.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxDrives.Location = new System.Drawing.Point(10, 10);
            this.groupBoxDrives.Name = "groupBoxDrives";
            this.groupBoxDrives.Padding = new System.Windows.Forms.Padding(8);
            this.groupBoxDrives.Size = new System.Drawing.Size(590, 255);
            this.groupBoxDrives.TabIndex = 0;
            this.groupBoxDrives.TabStop = false;
            this.groupBoxDrives.Text = "Mapped Network Drives";

            // 
            // listViewDrives
            // 
            this.listViewDrives.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colDrive,
            this.colShare,
            this.colUser});
            this.listViewDrives.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewDrives.FullRowSelect = true;
            this.listViewDrives.GridLines = true;
            this.listViewDrives.HideSelection = false;
            this.listViewDrives.Location = new System.Drawing.Point(8, 23);
            this.listViewDrives.MultiSelect = false;
            this.listViewDrives.Name = "listViewDrives";
            this.listViewDrives.Size = new System.Drawing.Size(474, 224);
            this.listViewDrives.TabIndex = 0;
            this.listViewDrives.UseCompatibleStateImageBehavior = false;
            this.listViewDrives.View = System.Windows.Forms.View.Details;

            // 
            // colDrive
            // 
            this.colDrive.Text = "Drive";
            this.colDrive.Width = 60;

            // 
            // colShare
            // 
            this.colShare.Text = "Remote Share";
            this.colShare.Width = 260;

            // 
            // colUser
            // 
            this.colUser.Text = "User";
            this.colUser.Width = 120;

            // 
            // panelDriveButtons
            // 
            this.panelDriveButtons.Controls.Add(this.buttonTestDrive);
            this.panelDriveButtons.Controls.Add(this.buttonRemoveDrive);
            this.panelDriveButtons.Controls.Add(this.buttonEditDrive);
            this.panelDriveButtons.Controls.Add(this.buttonAddDrive);
            this.panelDriveButtons.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelDriveButtons.Location = new System.Drawing.Point(482, 23);
            this.panelDriveButtons.Name = "panelDriveButtons";
            this.panelDriveButtons.Size = new System.Drawing.Size(100, 224);
            this.panelDriveButtons.TabIndex = 1;

            // 
            // buttonAddDrive
            // 
            this.buttonAddDrive.Location = new System.Drawing.Point(5, 5);
            this.buttonAddDrive.Name = "buttonAddDrive";
            this.buttonAddDrive.Size = new System.Drawing.Size(90, 28);
            this.buttonAddDrive.TabIndex = 0;
            this.buttonAddDrive.Text = "Add...";
            this.buttonAddDrive.UseVisualStyleBackColor = true;
            this.buttonAddDrive.Click += new System.EventHandler(this.ButtonAddDrive_Click);

            // 
            // buttonEditDrive
            // 
            this.buttonEditDrive.Location = new System.Drawing.Point(5, 38);
            this.buttonEditDrive.Name = "buttonEditDrive";
            this.buttonEditDrive.Size = new System.Drawing.Size(90, 28);
            this.buttonEditDrive.TabIndex = 1;
            this.buttonEditDrive.Text = "Edit...";
            this.buttonEditDrive.UseVisualStyleBackColor = true;
            this.buttonEditDrive.Click += new System.EventHandler(this.ButtonEditDrive_Click);

            // 
            // buttonRemoveDrive
            // 
            this.buttonRemoveDrive.Location = new System.Drawing.Point(5, 71);
            this.buttonRemoveDrive.Name = "buttonRemoveDrive";
            this.buttonRemoveDrive.Size = new System.Drawing.Size(90, 28);
            this.buttonRemoveDrive.TabIndex = 2;
            this.buttonRemoveDrive.Text = "Remove";
            this.buttonRemoveDrive.UseVisualStyleBackColor = true;
            this.buttonRemoveDrive.Click += new System.EventHandler(this.ButtonRemoveDrive_Click);

            // 
            // buttonTestDrive
            // 
            this.buttonTestDrive.Location = new System.Drawing.Point(5, 104);
            this.buttonTestDrive.Name = "buttonTestDrive";
            this.buttonTestDrive.Size = new System.Drawing.Size(90, 28);
            this.buttonTestDrive.TabIndex = 3;
            this.buttonTestDrive.Text = "Test Map";
            this.buttonTestDrive.UseVisualStyleBackColor = true;
            this.buttonTestDrive.Click += new System.EventHandler(this.ButtonTestDrive_Click);

            // 
            // groupBoxRemount
            // 
            this.groupBoxRemount.Controls.Add(this.checkBoxStartOnFail);
            this.groupBoxRemount.Controls.Add(this.numericUpDownRetryDelay);
            this.groupBoxRemount.Controls.Add(this.labelRetryDelay);
            this.groupBoxRemount.Controls.Add(this.numericUpDownRetryCount);
            this.groupBoxRemount.Controls.Add(this.labelRetryCount);
            this.groupBoxRemount.Controls.Add(this.checkBoxAutoRemount);
            this.groupBoxRemount.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.groupBoxRemount.Location = new System.Drawing.Point(10, 265);
            this.groupBoxRemount.Name = "groupBoxRemount";
            this.groupBoxRemount.Size = new System.Drawing.Size(590, 126);
            this.groupBoxRemount.TabIndex = 1;
            this.groupBoxRemount.TabStop = false;
            this.groupBoxRemount.Text = "Remount Options";

            // 
            // checkBoxAutoRemount
            // 
            this.checkBoxAutoRemount.AutoSize = true;
            this.checkBoxAutoRemount.Location = new System.Drawing.Point(15, 25);
            this.checkBoxAutoRemount.Name = "checkBoxAutoRemount";
            this.checkBoxAutoRemount.Size = new System.Drawing.Size(262, 21);
            this.checkBoxAutoRemount.TabIndex = 0;
            this.checkBoxAutoRemount.Text = "Auto-remount drives on service start";
            this.checkBoxAutoRemount.UseVisualStyleBackColor = true;

            // 
            // labelRetryCount
            // 
            this.labelRetryCount.AutoSize = true;
            this.labelRetryCount.Location = new System.Drawing.Point(15, 58);
            this.labelRetryCount.Name = "labelRetryCount";
            this.labelRetryCount.Size = new System.Drawing.Size(107, 17);
            this.labelRetryCount.TabIndex = 1;
            this.labelRetryCount.Text = "Retry Attempts:";

            // 
            // numericUpDownRetryCount
            // 
            this.numericUpDownRetryCount.Location = new System.Drawing.Point(130, 56);
            this.numericUpDownRetryCount.Maximum = new decimal(new int[] { 30, 0, 0, 0 });
            this.numericUpDownRetryCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownRetryCount.Name = "numericUpDownRetryCount";
            this.numericUpDownRetryCount.Size = new System.Drawing.Size(60, 22);
            this.numericUpDownRetryCount.TabIndex = 2;
            this.numericUpDownRetryCount.Value = new decimal(new int[] { 5, 0, 0, 0 });

            // 
            // labelRetryDelay
            // 
            this.labelRetryDelay.AutoSize = true;
            this.labelRetryDelay.Location = new System.Drawing.Point(215, 58);
            this.labelRetryDelay.Name = "labelRetryDelay";
            this.labelRetryDelay.Size = new System.Drawing.Size(111, 17);
            this.labelRetryDelay.TabIndex = 3;
            this.labelRetryDelay.Text = "Delay (seconds):";

            // 
            // numericUpDownRetryDelay
            // 
            this.numericUpDownRetryDelay.Location = new System.Drawing.Point(335, 56);
            this.numericUpDownRetryDelay.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            this.numericUpDownRetryDelay.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownRetryDelay.Name = "numericUpDownRetryDelay";
            this.numericUpDownRetryDelay.Size = new System.Drawing.Size(60, 22);
            this.numericUpDownRetryDelay.TabIndex = 4;
            this.numericUpDownRetryDelay.Value = new decimal(new int[] { 5, 0, 0, 0 });

            // 
            // checkBoxStartOnFail
            // 
            this.checkBoxStartOnFail.AutoSize = true;
            this.checkBoxStartOnFail.Location = new System.Drawing.Point(15, 90);
            this.checkBoxStartOnFail.Name = "checkBoxStartOnFail";
            this.checkBoxStartOnFail.Size = new System.Drawing.Size(342, 21);
            this.checkBoxStartOnFail.TabIndex = 5;
            this.checkBoxStartOnFail.Text = "Start Audiobookshelf even if network drive fails";
            this.checkBoxStartOnFail.UseVisualStyleBackColor = true;

            // 
            // tabPageUpdates
            // 
            this.tabPageUpdates.Controls.Add(this.groupBoxFolderUpdates);
            this.tabPageUpdates.Controls.Add(this.groupBoxGitHubUpdates);
            this.tabPageUpdates.Controls.Add(this.groupBoxApiKey);
            this.tabPageUpdates.Location = new System.Drawing.Point(4, 25);
            this.tabPageUpdates.Name = "tabPageUpdates";
            this.tabPageUpdates.Padding = new System.Windows.Forms.Padding(8);
            this.tabPageUpdates.Size = new System.Drawing.Size(610, 401);
            this.tabPageUpdates.TabIndex = 2;
            this.tabPageUpdates.Text = "Updates & Backups";
            this.tabPageUpdates.UseVisualStyleBackColor = true;

            // 
            // groupBoxApiKey
            // 
            this.groupBoxApiKey.Controls.Add(this.labelApiKeyInfo);
            this.groupBoxApiKey.Controls.Add(this.buttonTestApiKey);
            this.groupBoxApiKey.Controls.Add(this.textBoxApiKey);
            this.groupBoxApiKey.Controls.Add(this.labelApiKey);
            this.groupBoxApiKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxApiKey.Location = new System.Drawing.Point(8, 8);
            this.groupBoxApiKey.Name = "groupBoxApiKey";
            this.groupBoxApiKey.Padding = new System.Windows.Forms.Padding(8);
            this.groupBoxApiKey.Size = new System.Drawing.Size(594, 98);
            this.groupBoxApiKey.TabIndex = 0;
            this.groupBoxApiKey.TabStop = false;
            this.groupBoxApiKey.Text = "Authentication & API Key";

            // 
            // labelApiKey
            // 
            this.labelApiKey.AutoSize = true;
            this.labelApiKey.Location = new System.Drawing.Point(12, 22);
            this.labelApiKey.Name = "labelApiKey";
            this.labelApiKey.Size = new System.Drawing.Size(306, 17);
            this.labelApiKey.TabIndex = 0;
            this.labelApiKey.Text = "Admin API Key (Required for Updates/Backups):";

            // 
            // textBoxApiKey
            // 
            this.textBoxApiKey.Location = new System.Drawing.Point(12, 42);
            this.textBoxApiKey.Name = "textBoxApiKey";
            this.textBoxApiKey.Size = new System.Drawing.Size(460, 22);
            this.textBoxApiKey.TabIndex = 1;

            // 
            // buttonTestApiKey
            // 
            this.buttonTestApiKey.Location = new System.Drawing.Point(480, 40);
            this.buttonTestApiKey.Name = "buttonTestApiKey";
            this.buttonTestApiKey.Size = new System.Drawing.Size(95, 26);
            this.buttonTestApiKey.TabIndex = 2;
            this.buttonTestApiKey.Text = "Test Key";
            this.buttonTestApiKey.UseVisualStyleBackColor = true;
            this.buttonTestApiKey.Click += new System.EventHandler(this.ButtonTestApiKey_Click);

            // 
            // labelApiKeyInfo
            // 
            this.labelApiKeyInfo.ForeColor = System.Drawing.Color.DimGray;
            this.labelApiKeyInfo.Location = new System.Drawing.Point(12, 68);
            this.labelApiKeyInfo.Name = "labelApiKeyInfo";
            this.labelApiKeyInfo.Size = new System.Drawing.Size(560, 24);
            this.labelApiKeyInfo.TabIndex = 3;
            this.labelApiKeyInfo.Text = "Generate in Audiobookshelf (Web UI -> Settings -> Users -> API Keys).";

            // 
            // groupBoxGitHubUpdates
            // 
            this.groupBoxGitHubUpdates.Controls.Add(this.labelGitHubUpdatesInfo);
            this.groupBoxGitHubUpdates.Controls.Add(this.checkBoxGitHubUpdates);
            this.groupBoxGitHubUpdates.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxGitHubUpdates.Location = new System.Drawing.Point(8, 106);
            this.groupBoxGitHubUpdates.Name = "groupBoxGitHubUpdates";
            this.groupBoxGitHubUpdates.Padding = new System.Windows.Forms.Padding(8);
            this.groupBoxGitHubUpdates.Size = new System.Drawing.Size(594, 110);
            this.groupBoxGitHubUpdates.TabIndex = 1;
            this.groupBoxGitHubUpdates.TabStop = false;
            this.groupBoxGitHubUpdates.Text = "GitHub Auto-Updates";

            // 
            // checkBoxGitHubUpdates
            // 
            this.checkBoxGitHubUpdates.AutoSize = true;
            this.checkBoxGitHubUpdates.Location = new System.Drawing.Point(12, 24);
            this.checkBoxGitHubUpdates.Name = "checkBoxGitHubUpdates";
            this.checkBoxGitHubUpdates.Size = new System.Drawing.Size(490, 21);
            this.checkBoxGitHubUpdates.TabIndex = 0;
            this.checkBoxGitHubUpdates.Text = "Automatically download and apply updates from GitHub at midnight (00:00)";
            this.checkBoxGitHubUpdates.UseVisualStyleBackColor = true;

            // 
            // labelGitHubUpdatesInfo
            // 
            this.labelGitHubUpdatesInfo.ForeColor = System.Drawing.Color.DimGray;
            this.labelGitHubUpdatesInfo.Location = new System.Drawing.Point(12, 48);
            this.labelGitHubUpdatesInfo.Name = "labelGitHubUpdatesInfo";
            this.labelGitHubUpdatesInfo.Size = new System.Drawing.Size(560, 52);
            this.labelGitHubUpdatesInfo.TabIndex = 1;
            this.labelGitHubUpdatesInfo.Text = "Note: Disabled by default. When enabled, the service checks GitHub daily at midnight local time, creates a pre-update backup, downloads the new audiobookshelf.exe, and restarts.";

            // 
            // groupBoxFolderUpdates
            // 
            this.groupBoxFolderUpdates.Controls.Add(this.labelFolderUpdatesInfo);
            this.groupBoxFolderUpdates.Controls.Add(this.buttonOpenUpdatesFolder);
            this.groupBoxFolderUpdates.Controls.Add(this.textBoxUpdatesFolder);
            this.groupBoxFolderUpdates.Controls.Add(this.labelFolderUpdatesPath);
            this.groupBoxFolderUpdates.Controls.Add(this.checkBoxFolderUpdates);
            this.groupBoxFolderUpdates.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxFolderUpdates.Location = new System.Drawing.Point(8, 216);
            this.groupBoxFolderUpdates.Name = "groupBoxFolderUpdates";
            this.groupBoxFolderUpdates.Padding = new System.Windows.Forms.Padding(8);
            this.groupBoxFolderUpdates.Size = new System.Drawing.Size(594, 177);
            this.groupBoxFolderUpdates.TabIndex = 2;
            this.groupBoxFolderUpdates.TabStop = false;
            this.groupBoxFolderUpdates.Text = "Local Drop-in Folder Updates";

            // 
            // checkBoxFolderUpdates
            // 
            this.checkBoxFolderUpdates.AutoSize = true;
            this.checkBoxFolderUpdates.Location = new System.Drawing.Point(12, 22);
            this.checkBoxFolderUpdates.Name = "checkBoxFolderUpdates";
            this.checkBoxFolderUpdates.Size = new System.Drawing.Size(430, 21);
            this.checkBoxFolderUpdates.TabIndex = 0;
            this.checkBoxFolderUpdates.Text = "Automatically apply updates from drop folder at midnight (00:00)";
            this.checkBoxFolderUpdates.UseVisualStyleBackColor = true;

            // 
            // labelFolderUpdatesPath
            // 
            this.labelFolderUpdatesPath.AutoSize = true;
            this.labelFolderUpdatesPath.Location = new System.Drawing.Point(12, 46);
            this.labelFolderUpdatesPath.Name = "labelFolderUpdatesPath";
            this.labelFolderUpdatesPath.Size = new System.Drawing.Size(124, 17);
            this.labelFolderUpdatesPath.TabIndex = 1;
            this.labelFolderUpdatesPath.Text = "Update Drop Folder:";

            // 
            // textBoxUpdatesFolder
            // 
            this.textBoxUpdatesFolder.Location = new System.Drawing.Point(12, 66);
            this.textBoxUpdatesFolder.Name = "textBoxUpdatesFolder";
            this.textBoxUpdatesFolder.ReadOnly = true;
            this.textBoxUpdatesFolder.Size = new System.Drawing.Size(460, 22);
            this.textBoxUpdatesFolder.TabIndex = 2;

            // 
            // buttonOpenUpdatesFolder
            // 
            this.buttonOpenUpdatesFolder.Location = new System.Drawing.Point(480, 64);
            this.buttonOpenUpdatesFolder.Name = "buttonOpenUpdatesFolder";
            this.buttonOpenUpdatesFolder.Size = new System.Drawing.Size(95, 26);
            this.buttonOpenUpdatesFolder.TabIndex = 3;
            this.buttonOpenUpdatesFolder.Text = "Open Folder...";
            this.buttonOpenUpdatesFolder.UseVisualStyleBackColor = true;
            this.buttonOpenUpdatesFolder.Click += new System.EventHandler(this.ButtonOpenUpdatesFolder_Click);

            // 
            // labelFolderUpdatesInfo
            // 
            this.labelFolderUpdatesInfo.ForeColor = System.Drawing.Color.DimGray;
            this.labelFolderUpdatesInfo.Location = new System.Drawing.Point(12, 94);
            this.labelFolderUpdatesInfo.Name = "labelFolderUpdatesInfo";
            this.labelFolderUpdatesInfo.Size = new System.Drawing.Size(560, 75);
            this.labelFolderUpdatesInfo.TabIndex = 4;
            this.labelFolderUpdatesInfo.Text = "Drop an audiobookshelf.exe into this folder. At midnight local time, the service will trigger a pre-update backup of database and files, replace the executable, and restart the server.";

            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(545, 452);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(85, 30);
            this.buttonCancel.TabIndex = 1;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;

            // 
            // buttonSave
            // 
            this.buttonSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSave.Location = new System.Drawing.Point(454, 452);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(85, 30);
            this.buttonSave.TabIndex = 2;
            this.buttonSave.Text = "Save";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.SaveClicked);

            // 
            // errorProviderPort
            // 
            this.errorProviderPort.ContainerControl = this;

            // 
            // errorProviderDataFolder
            // 
            this.errorProviderDataFolder.ContainerControl = this;

            // 
            // errorProviderApiKey
            // 
            this.errorProviderApiKey.ContainerControl = this;

            // 
            // SettingsDialog
            // 
            this.AcceptButton = this.buttonSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(642, 492);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.buttonCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(658, 530);
            this.Name = "SettingsDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Audiobookshelf Settings";

            this.tabControl.ResumeLayout(false);
            this.tabPageServer.ResumeLayout(false);
            this.groupBoxServer.ResumeLayout(false);
            this.tableLayoutPanelServer.ResumeLayout(false);
            this.tableLayoutDataFolder.ResumeLayout(false);
            this.tableLayoutDataFolder.PerformLayout();
            this.tableLayoutPanelPort.ResumeLayout(false);
            this.tableLayoutPanelPort.PerformLayout();
            this.tabPageDrives.ResumeLayout(false);
            this.groupBoxDrives.ResumeLayout(false);
            this.panelDriveButtons.ResumeLayout(false);
            this.groupBoxRemount.ResumeLayout(false);
            this.groupBoxRemount.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownRetryCount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownRetryDelay)).EndInit();
            this.tabPageUpdates.ResumeLayout(false);
            this.groupBoxApiKey.ResumeLayout(false);
            this.groupBoxApiKey.PerformLayout();
            this.groupBoxGitHubUpdates.ResumeLayout(false);
            this.groupBoxGitHubUpdates.PerformLayout();
            this.groupBoxFolderUpdates.ResumeLayout(false);
            this.groupBoxFolderUpdates.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderDataFolder)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderApiKey)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPageServer;
        private System.Windows.Forms.TabPage tabPageDrives;
        private System.Windows.Forms.TabPage tabPageUpdates;

        private System.Windows.Forms.GroupBox groupBoxServer;
        private System.Windows.Forms.TextBox textBoxPort;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelServer;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelPort;
        private System.Windows.Forms.TableLayoutPanel tableLayoutDataFolder;
        private System.Windows.Forms.TextBox textBoxDataFolder;
        private System.Windows.Forms.ErrorProvider errorProviderPort;
        private System.Windows.Forms.ErrorProvider errorProviderDataFolder;
        private System.Windows.Forms.ErrorProvider errorProviderApiKey;
        private System.Windows.Forms.Label labelPort;
        private System.Windows.Forms.Label labelDataFolder;

        private System.Windows.Forms.GroupBox groupBoxDrives;
        private System.Windows.Forms.ListView listViewDrives;
        private System.Windows.Forms.ColumnHeader colDrive;
        private System.Windows.Forms.ColumnHeader colShare;
        private System.Windows.Forms.ColumnHeader colUser;
        private System.Windows.Forms.Panel panelDriveButtons;
        private System.Windows.Forms.Button buttonAddDrive;
        private System.Windows.Forms.Button buttonEditDrive;
        private System.Windows.Forms.Button buttonRemoveDrive;
        private System.Windows.Forms.Button buttonTestDrive;

        private System.Windows.Forms.GroupBox groupBoxRemount;
        private System.Windows.Forms.CheckBox checkBoxAutoRemount;
        private System.Windows.Forms.Label labelRetryCount;
        private System.Windows.Forms.NumericUpDown numericUpDownRetryCount;
        private System.Windows.Forms.Label labelRetryDelay;
        private System.Windows.Forms.NumericUpDown numericUpDownRetryDelay;
        private System.Windows.Forms.CheckBox checkBoxStartOnFail;

        private System.Windows.Forms.GroupBox groupBoxApiKey;
        private System.Windows.Forms.Label labelApiKey;
        private System.Windows.Forms.TextBox textBoxApiKey;
        private System.Windows.Forms.Button buttonTestApiKey;
        private System.Windows.Forms.Label labelApiKeyInfo;

        private System.Windows.Forms.GroupBox groupBoxGitHubUpdates;
        private System.Windows.Forms.CheckBox checkBoxGitHubUpdates;
        private System.Windows.Forms.Label labelGitHubUpdatesInfo;

        private System.Windows.Forms.GroupBox groupBoxFolderUpdates;
        private System.Windows.Forms.CheckBox checkBoxFolderUpdates;
        private System.Windows.Forms.Label labelFolderUpdatesPath;
        private System.Windows.Forms.TextBox textBoxUpdatesFolder;
        private System.Windows.Forms.Button buttonOpenUpdatesFolder;
        private System.Windows.Forms.Label labelFolderUpdatesInfo;
    }
}