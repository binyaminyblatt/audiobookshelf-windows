using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace AudiobookshelfTray
{
    partial class ServerLogs
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServerLogs));
            this.panelTop = new System.Windows.Forms.Panel();
            this.labelLogFile = new System.Windows.Forms.Label();
            this.comboBoxLogFile = new System.Windows.Forms.ComboBox();
            this.buttonRefresh = new System.Windows.Forms.Button();
            this.buttonOpenFolder = new System.Windows.Forms.Button();
            this.checkBoxAutoScroll = new System.Windows.Forms.CheckBox();
            this.logsListBox = new System.Windows.Forms.ListBox();
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copySelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panelTop.SuspendLayout();
            this.contextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelTop.Controls.Add(this.labelLogFile);
            this.panelTop.Controls.Add(this.comboBoxLogFile);
            this.panelTop.Controls.Add(this.buttonRefresh);
            this.panelTop.Controls.Add(this.buttonOpenFolder);
            this.panelTop.Controls.Add(this.checkBoxAutoScroll);
            this.panelTop.Location = new System.Drawing.Point(12, 8);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(760, 38);
            this.panelTop.TabIndex = 0;
            // 
            // labelLogFile
            // 
            this.labelLogFile.AutoSize = true;
            this.labelLogFile.Location = new System.Drawing.Point(4, 9);
            this.labelLogFile.Name = "labelLogFile";
            this.labelLogFile.Size = new System.Drawing.Size(89, 17);
            this.labelLogFile.TabIndex = 0;
            this.labelLogFile.Text = "Log Source:";
            // 
            // comboBoxLogFile
            // 
            this.comboBoxLogFile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLogFile.DropDownWidth = 420;
            this.comboBoxLogFile.FormattingEnabled = true;
            this.comboBoxLogFile.Location = new System.Drawing.Point(98, 6);
            this.comboBoxLogFile.Name = "comboBoxLogFile";
            this.comboBoxLogFile.Size = new System.Drawing.Size(320, 24);
            this.comboBoxLogFile.TabIndex = 1;
            // 
            // buttonRefresh
            // 
            this.buttonRefresh.Location = new System.Drawing.Point(428, 5);
            this.buttonRefresh.Name = "buttonRefresh";
            this.buttonRefresh.Size = new System.Drawing.Size(82, 26);
            this.buttonRefresh.TabIndex = 2;
            this.buttonRefresh.Text = "Reload";
            this.buttonRefresh.UseVisualStyleBackColor = true;
            // 
            // buttonOpenFolder
            // 
            this.buttonOpenFolder.Location = new System.Drawing.Point(518, 5);
            this.buttonOpenFolder.Name = "buttonOpenFolder";
            this.buttonOpenFolder.Size = new System.Drawing.Size(130, 26);
            this.buttonOpenFolder.TabIndex = 3;
            this.buttonOpenFolder.Text = "Open Logs Folder";
            this.buttonOpenFolder.UseVisualStyleBackColor = true;
            // 
            // checkBoxAutoScroll
            // 
            this.checkBoxAutoScroll.AutoSize = true;
            this.checkBoxAutoScroll.Checked = true;
            this.checkBoxAutoScroll.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxAutoScroll.Location = new System.Drawing.Point(658, 9);
            this.checkBoxAutoScroll.Name = "checkBoxAutoScroll";
            this.checkBoxAutoScroll.Size = new System.Drawing.Size(95, 21);
            this.checkBoxAutoScroll.TabIndex = 4;
            this.checkBoxAutoScroll.Text = "Auto-scroll";
            this.checkBoxAutoScroll.UseVisualStyleBackColor = true;
            // 
            // logsListBox
            // 
            this.logsListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logsListBox.ContextMenuStrip = this.contextMenuStrip;
            this.logsListBox.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.logsListBox.FormattingEnabled = true;
            this.logsListBox.HorizontalScrollbar = true;
            this.logsListBox.ItemHeight = 15;
            this.logsListBox.Location = new System.Drawing.Point(12, 50);
            this.logsListBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.logsListBox.Name = "logsListBox";
            this.logsListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.logsListBox.Size = new System.Drawing.Size(760, 484);
            this.logsListBox.TabIndex = 1;
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.selectAllToolStripMenuItem,
            this.copySelectedToolStripMenuItem,
            this.clearViewToolStripMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip1";
            this.contextMenuStrip.Size = new System.Drawing.Size(176, 78);
            // 
            // selectAllToolStripMenuItem
            // 
            this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
            this.selectAllToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
            this.selectAllToolStripMenuItem.Size = new System.Drawing.Size(175, 24);
            this.selectAllToolStripMenuItem.Text = "Select All";
            // 
            // copySelectedToolStripMenuItem
            // 
            this.copySelectedToolStripMenuItem.Name = "copySelectedToolStripMenuItem";
            this.copySelectedToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.copySelectedToolStripMenuItem.Size = new System.Drawing.Size(175, 24);
            this.copySelectedToolStripMenuItem.Text = "Copy Selected";
            // 
            // clearViewToolStripMenuItem
            // 
            this.clearViewToolStripMenuItem.Name = "clearViewToolStripMenuItem";
            this.clearViewToolStripMenuItem.Size = new System.Drawing.Size(175, 24);
            this.clearViewToolStripMenuItem.Text = "Clear View";
            // 
            // ServerLogs
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 545);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.logsListBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "ServerLogs";
            this.Text = "Audiobookshelf Logs";
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.contextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label labelLogFile;
        private System.Windows.Forms.ComboBox comboBoxLogFile;
        private System.Windows.Forms.Button buttonRefresh;
        private System.Windows.Forms.Button buttonOpenFolder;
        private System.Windows.Forms.CheckBox checkBoxAutoScroll;
        private System.Windows.Forms.ListBox logsListBox;
        private ContextMenuStrip contextMenuStrip;
        private ToolStripMenuItem selectAllToolStripMenuItem;
        private ToolStripMenuItem copySelectedToolStripMenuItem;
        private ToolStripMenuItem clearViewToolStripMenuItem;
    }
}