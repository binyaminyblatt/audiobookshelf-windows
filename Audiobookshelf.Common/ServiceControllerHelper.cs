using System;
using System.Diagnostics;
using System.ServiceProcess;
using NLog;

namespace Audiobookshelf.Common
{
    public static class ServiceControllerHelper
    {
        public const string SERVICE_NAME = "AudiobookshelfService";
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static bool IsServiceInstalled()
        {
            try
            {
                using (var sc = new ServiceController(SERVICE_NAME))
                {
                    var status = sc.Status;
                    return true;
                }
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error checking if service is installed: {ex.Message}");
                return false;
            }
        }

        public static ServiceControllerStatus? GetServiceStatus()
        {
            try
            {
                using (var sc = new ServiceController(SERVICE_NAME))
                {
                    sc.Refresh();
                    return sc.Status;
                }
            }
            catch (Exception ex)
            {
                _logger.Trace($"Unable to query service status: {ex.Message}");
                return null;
            }
        }

        public static bool StartService(int timeoutSeconds = 30)
        {
            if (!IsServiceInstalled())
            {
                _logger.Warn($"Cannot start service {SERVICE_NAME}: Service is not installed.");
                return false;
            }

            try
            {
                using (var sc = new ServiceController(SERVICE_NAME))
                {
                    sc.Refresh();
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        return true;
                    }

                    if (sc.Status == ServiceControllerStatus.StopPending)
                    {
                        _logger.Debug("Service is currently stopping. Waiting for it to stop before starting...");
                        try
                        {
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(Math.Min(15, timeoutSeconds)));
                        }
                        catch (Exception ex)
                        {
                            _logger.Warn($"Wait for Stopped status timed out/failed: {ex.Message}");
                        }
                        sc.Refresh();
                    }

                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        _logger.Info($"Starting service {SERVICE_NAME}...");
                        sc.Start();
                    }

                    try
                    {
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(timeoutSeconds));
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"Wait for Running status timed out/failed: {ex.Message}");
                    }

                    sc.Refresh();
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        _logger.Info($"Service {SERVICE_NAME} is now running.");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Standard StartService encountered exception: {ex.Message}. Attempting elevated fallback...");
                if (RunElevatedCommand("net.exe", $"start {SERVICE_NAME}"))
                {
                    var sw = Stopwatch.StartNew();
                    while (sw.ElapsedMilliseconds < timeoutSeconds * 1000)
                    {
                        if (GetServiceStatus() == ServiceControllerStatus.Running)
                        {
                            _logger.Info($"Service {SERVICE_NAME} started successfully via elevated fallback.");
                            return true;
                        }
                        System.Threading.Thread.Sleep(500);
                    }
                }
            }

            var finalStatus = GetServiceStatus();
            _logger.Info($"StartService completed with status: {finalStatus}");
            return finalStatus == ServiceControllerStatus.Running;
        }

        public static bool StopService(int timeoutSeconds = 30)
        {
            if (!IsServiceInstalled())
            {
                _logger.Warn($"Cannot stop service {SERVICE_NAME}: Service is not installed.");
                return false;
            }

            try
            {
                using (var sc = new ServiceController(SERVICE_NAME))
                {
                    sc.Refresh();
                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        return true;
                    }

                    if (sc.Status == ServiceControllerStatus.StartPending)
                    {
                        _logger.Debug("Service is currently starting. Waiting for it to start before stopping...");
                        try
                        {
                            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(Math.Min(15, timeoutSeconds)));
                        }
                        catch (Exception ex)
                        {
                            _logger.Warn($"Wait for Running status timed out/failed: {ex.Message}");
                        }
                        sc.Refresh();
                    }

                    if (sc.Status == ServiceControllerStatus.Running || sc.Status == ServiceControllerStatus.Paused)
                    {
                        _logger.Info($"Stopping service {SERVICE_NAME}...");
                        sc.Stop();
                    }

                    try
                    {
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(timeoutSeconds));
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"Wait for Stopped status timed out/failed: {ex.Message}");
                    }

                    sc.Refresh();
                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        _logger.Info($"Service {SERVICE_NAME} is now stopped.");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Standard StopService encountered exception: {ex.Message}. Attempting elevated fallback...");
                if (RunElevatedCommand("net.exe", $"stop {SERVICE_NAME}"))
                {
                    var sw = Stopwatch.StartNew();
                    while (sw.ElapsedMilliseconds < timeoutSeconds * 1000)
                    {
                        if (GetServiceStatus() == ServiceControllerStatus.Stopped)
                        {
                            _logger.Info($"Service {SERVICE_NAME} stopped successfully via elevated fallback.");
                            return true;
                        }
                        System.Threading.Thread.Sleep(500);
                    }
                }
            }

            var finalStatus = GetServiceStatus();
            _logger.Info($"StopService completed with status: {finalStatus}");
            return finalStatus == ServiceControllerStatus.Stopped;
        }

        public static bool RestartService(int timeoutSeconds = 30)
        {
            _logger.Info($"Restarting service {SERVICE_NAME} (timeout: {timeoutSeconds}s)...");

            if (!IsServiceInstalled())
            {
                _logger.Warn($"Cannot restart service {SERVICE_NAME}: Service is not installed.");
                return false;
            }

            int stopTimeout = Math.Max(10, timeoutSeconds / 2);
            int startTimeout = Math.Max(15, timeoutSeconds / 2);

            var currentStatus = GetServiceStatus();
            if (currentStatus != ServiceControllerStatus.Stopped)
            {
                bool stopped = StopService(stopTimeout);
                if (!stopped)
                {
                    _logger.Warn($"StopService returned false during restart. Checking if service eventually reached Stopped...");
                    System.Threading.Thread.Sleep(1000);
                    if (GetServiceStatus() != ServiceControllerStatus.Stopped)
                    {
                        _logger.Error($"Failed to stop service {SERVICE_NAME} during restart sequence.");
                        return false;
                    }
                }
            }

            // Brief delay to allow OS socket cleanup
            System.Threading.Thread.Sleep(1000);

            bool started = StartService(startTimeout);
            if (!started)
            {
                _logger.Error($"Failed to start service {SERVICE_NAME} during restart sequence.");
                return false;
            }

            _logger.Info($"Service {SERVICE_NAME} restarted successfully.");
            return true;
        }

        public const string SERVICE_BINARY = "AudiobookshelfService.exe";

        public static string FindServiceBinary()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string directPath = System.IO.Path.Combine(baseDir, SERVICE_BINARY);
            if (System.IO.File.Exists(directPath)) return directPath;

            string[] probePaths = new[]
            {
                System.IO.Path.Combine(baseDir, "..", SERVICE_BINARY),
                System.IO.Path.Combine(baseDir, "..", "..", SERVICE_BINARY),
                System.IO.Path.Combine(baseDir, "..", "AudiobookshelfService", "bin", "x64", "Release", "net461", SERVICE_BINARY),
                System.IO.Path.Combine(baseDir, "..", "..", "AudiobookshelfService", "bin", "x64", "Release", "net461", SERVICE_BINARY),
                System.IO.Path.Combine(baseDir, "..", "AudiobookshelfService", "bin", "x64", "Debug", "net461", SERVICE_BINARY),
                System.IO.Path.Combine(baseDir, "..", "..", "AudiobookshelfService", "bin", "x64", "Debug", "net461", SERVICE_BINARY),
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Audiobookshelf", SERVICE_BINARY),
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Audiobookshelf", SERVICE_BINARY)
            };

            foreach (var probe in probePaths)
            {
                try
                {
                    string fullPath = System.IO.Path.GetFullPath(probe);
                    if (System.IO.File.Exists(fullPath)) return fullPath;
                }
                catch { }
            }

            return directPath;
        }

        public static bool InstallAndStartService(string username, string password, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Windows username and password are required. The service must run under a user account to mount and authenticate network drives.";
                return false;
            }

            string serviceExePath = FindServiceBinary();
            if (!System.IO.File.Exists(serviceExePath))
            {
                errorMessage = $"Could not locate {SERVICE_BINARY} at '{serviceExePath}'.";
                _logger.Error(errorMessage);
                return false;
            }

            try
            {
                string tempBatPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"InstallABS_{Guid.NewGuid():N}.bat");
                string safeUser = username.Trim().Replace("\"", "\\\"");
                string safePass = password.Replace("\"", "\\\"");

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("@echo off");
                sb.AppendLine($"sc.exe stop {SERVICE_NAME} >nul 2>&1");
                sb.AppendLine("taskkill.exe /F /IM audiobookshelf.exe >nul 2>&1");
                sb.AppendLine($"sc.exe delete {SERVICE_NAME} >nul 2>&1");
                sb.AppendLine("timeout /t 1 /nobreak >nul");
                sb.AppendLine($"sc.exe create {SERVICE_NAME} binPath= \"{serviceExePath}\" start= auto DisplayName= \"Audiobookshelf Service\"");
                sb.AppendLine($"sc.exe description {SERVICE_NAME} \"Audiobookshelf Background Server and Network Drive Supervisor\"");
                sb.AppendLine($"sc.exe config {SERVICE_NAME} obj= \"{safeUser}\" password= \"{safePass}\"");
                sb.AppendLine($"sc.exe sdset {SERVICE_NAME} \"D:(A;;CCLCSWRPWPDTLORC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWRPWPDTLORC;;;IU)(A;;CCLCSWRPWPDTLORC;;;AU)(A;;CCLCSWRPWPDTLORC;;;PU)S:(AU;FA;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;WD)\"");
                sb.AppendLine($"net.exe start {SERVICE_NAME}");
                sb.AppendLine("exit /b %ERRORLEVEL%");

                System.IO.File.WriteAllText(tempBatPath, sb.ToString());

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{tempBatPath}\"",
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        proc.WaitForExit(45000);
                    }
                }

                try
                {
                    if (System.IO.File.Exists(tempBatPath)) System.IO.File.Delete(tempBatPath);
                }
                catch { }

                // Wait up to 10 seconds for service to reach Running status
                var sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < 10000)
                {
                    if (IsServiceInstalled() && GetServiceStatus() == ServiceControllerStatus.Running)
                    {
                        _logger.Info($"Service {SERVICE_NAME} installed and started successfully.");
                        return true;
                    }
                    System.Threading.Thread.Sleep(500);
                }

                if (IsServiceInstalled())
                {
                    var status = GetServiceStatus();
                    if (status != ServiceControllerStatus.Running)
                    {
                        StartService(15);
                        if (GetServiceStatus() == ServiceControllerStatus.Running)
                        {
                            return true;
                        }
                    }
                    errorMessage = $"Service was installed but current status is '{status}'. Please check Windows Event Logs and account permissions.";
                    return false;
                }
                else
                {
                    errorMessage = "Failed to create the Windows Service. Administrator privileges are required.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                _logger.Error($"Exception installing service: {ex}");
                return false;
            }
        }

        private static bool RunElevatedCommand(string fileName, string arguments)
        {
            try
            {
                _logger.Info($"Running elevated fallback command: {fileName} {arguments}");
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        proc.WaitForExit(30000);
                        return proc.ExitCode == 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Elevated fallback command failed: {ex.Message}");
            }
            return false;
        }
    }
}
