using System;
using System.ServiceProcess;
using System.Threading;
using Audiobookshelf.Common;
using NLog;

namespace AudiobookshelfService
{
    public class AudiobookshelfService : ServiceBase
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly ServerMonitor _monitor;

        public AudiobookshelfService()
        {
            ServiceName = "AudiobookshelfService";
            CanStop = true;
            CanShutdown = true;
            CanPauseAndContinue = false;
            AutoLog = false;

            _monitor = new ServerMonitor();
        }

        protected override void OnStart(string[] args)
        {
            _logger.Info("AudiobookshelfService is starting...");
            try
            {
                _monitor.Start();
                base.OnStart(args);
                _logger.Info("AudiobookshelfService started successfully.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to start AudiobookshelfService: {ex}");
                throw;
            }
        }

        protected override void OnStop()
        {
            _logger.Info("AudiobookshelfService is stopping...");
            try
            {
                _monitor.Stop();
                base.OnStop();
                _logger.Info("AudiobookshelfService stopped successfully.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error stopping AudiobookshelfService: {ex}");
            }
        }

        protected override void OnShutdown()
        {
            _logger.Info("System shutdown detected. Stopping AudiobookshelfService...");
            OnStop();
            base.OnShutdown();
        }

        // For console/interactive debugging
        public void StartInteractive(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("=================================================");
            Console.WriteLine(" Starting AudiobookshelfService (Interactive)   ");
            Console.WriteLine("=================================================");
            Console.WriteLine();

            OnStart(args);

            Console.WriteLine();
            Console.WriteLine("Service is running. Press Ctrl+C to stop.");
            Console.WriteLine();

            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                exitEvent.Set();
            };

            exitEvent.WaitOne();

            Console.WriteLine();
            Console.WriteLine("Stopping AudiobookshelfService...");
            OnStop();
            Console.WriteLine("AudiobookshelfService stopped.");
        }
    }
}
