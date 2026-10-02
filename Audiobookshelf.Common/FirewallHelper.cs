using System;
using System.Diagnostics;
using NLog;

namespace Audiobookshelf.Common
{
    public static class FirewallHelper
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        public const string DEFAULT_RULE_NAME = "Audiobookshelf Server";

        public static bool AddOrUpdateFirewallRule(string port, string ruleName = DEFAULT_RULE_NAME)
        {
            if (string.IsNullOrWhiteSpace(port)) return false;

            try
            {
                // First delete existing rule to avoid duplicates
                RemoveFirewallRule(ruleName);

                _logger.Info($"Configuring Windows Defender Firewall inbound rule '{ruleName}' on TCP port {port}...");
                string args = $"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow protocol=TCP localport={port}";

                var psi = new ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        proc.WaitForExit(10000);
                        bool success = proc.ExitCode == 0;
                        if (success)
                        {
                            _logger.Info($"Firewall rule '{ruleName}' on TCP port {port} created successfully.");
                        }
                        else
                        {
                            _logger.Warn($"netsh firewall rule creation returned exit code: {proc.ExitCode}");
                        }
                        return success;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to configure firewall rule: {ex.Message}");
            }
            return false;
        }

        public static bool RemoveFirewallRule(string ruleName = DEFAULT_RULE_NAME)
        {
            try
            {
                _logger.Debug($"Removing Windows Defender Firewall rule '{ruleName}'...");
                string args = $"advfirewall firewall delete rule name=\"{ruleName}\"";

                var psi = new ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        proc.WaitForExit(10000);
                        return proc.ExitCode == 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error removing firewall rule: {ex.Message}");
            }
            return false;
        }
    }
}
