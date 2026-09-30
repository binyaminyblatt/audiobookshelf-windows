using Audiobookshelf.Common;
using AudiobookshelfTray.Properties;
using System;
using System.Windows.Forms;

namespace AudiobookshelfTray
{
    partial class AboutBox : Form
    {
        private readonly string _serverName;
        private readonly string _serverVersion;
        private readonly string _appVersion;

        public AboutBox(AppTray appTray)
        {
            var settings = SettingsHandler.Load();
            _serverName = "Audiobookshelf";
            _serverVersion = string.IsNullOrEmpty(settings.AppVersion) ? "v2.x" : settings.AppVersion;
            _appVersion = string.IsNullOrEmpty(settings.AppVersion) ? "v2.x" : settings.AppVersion;
            InitializeComponent();
            Text = String.Format("About {0}", AssemblyTitle);
            labelProductName.Text = AssemblyProduct;
            labelServerVersion.Text = String.Format("Server Version {0}", AssemblyServerVersion);
            labelServerVersion.Click += (s, e) => System.Diagnostics.Process.Start("https://github.com/advplyr/audiobookshelf/releases/tag/" + AssemblyServerVersion);
            labelAppVersion.Text = String.Format("App Version {0}", AssemblyAppVersion);
            labelAppVersion.Click += (s, e) => System.Diagnostics.Process.Start("https://github.com/binyaminyblatt/audiobookshelf-windows/releases/tag/" + AssemblyAppVersion);
            labelURL.Text = "audiobookshelf.org";
            labelURL.Click += (s, e) => System.Diagnostics.Process.Start("https://www.audiobookshelf.org/");
            StartPosition = FormStartPosition.CenterScreen;
        }

        #region Assembly Attribute Accessors

        public string AssemblyProduct
        {
            get
            {
                return string.Format("{0}\nSelf-hosted audiobook and podcast server", _serverName);
            }
        }

        public string AssemblyServerVersion
        {
            get
            {
                return String.IsNullOrEmpty(_serverVersion) ? "unknown" : _serverVersion;
            }
        }

        public string AssemblyAppVersion
        {
            get
            {
                return String.IsNullOrEmpty(_appVersion) ? "unknown" : _appVersion;
            }
        }

        public string AssemblyTitle
        {
            get
            {
                return _serverName;
            }
        }

        #endregion
    }
}
