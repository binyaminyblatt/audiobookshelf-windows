using System;
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
                    return sc.Status;
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Unable to query service status: {ex.Message}");
                return null;
            }
        }

        public static bool StartService(int timeoutSeconds = 30)
        {
            try
            {
                using (var sc = new ServiceController(SERVICE_NAME))
                {
                    if (sc.Status == ServiceControllerStatus.Stopped || sc.Status == ServiceControllerStatus.StopPending)
                    {
                        if (sc.Status == ServiceControllerStatus.StopPending)
                        {
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                        }

                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(timeoutSeconds));
                        return sc.Status == ServiceControllerStatus.Running;
                    }
                    return sc.Status == ServiceControllerStatus.Running;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to start service {SERVICE_NAME}: {ex.Message}");
                return false;
            }
        }

        public static bool StopService(int timeoutSeconds = 30)
        {
            try
            {
                using (var sc = new ServiceController(SERVICE_NAME))
                {
                    if (sc.Status == ServiceControllerStatus.Running || sc.Status == ServiceControllerStatus.StartPending)
                    {
                        if (sc.CanStop)
                        {
                            sc.Stop();
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(timeoutSeconds));
                            return sc.Status == ServiceControllerStatus.Stopped;
                        }
                    }
                    return sc.Status == ServiceControllerStatus.Stopped;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to stop service {SERVICE_NAME}: {ex.Message}");
                return false;
            }
        }

        public static bool RestartService(int timeoutSeconds = 30)
        {
            StopService(timeoutSeconds / 2);
            return StartService(timeoutSeconds / 2);
        }
    }
}
