using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NLog;

namespace Audiobookshelf.Common
{
    public static class ProcessUtils
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GenerateConsoleCtrlEvent(CtrlTypes dwCtrlEvent, uint dwProcessGroupId);

        [DllImport("Kernel32", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);

        private enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        private delegate bool HandlerRoutine(CtrlTypes CtrlType);

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Tries to stop a process by sending a CTRL_C_EVENT to its console.
        /// This allows the process to perform cleanup operations before exiting, unlike Process.Kill().
        /// If the process does not exit within a reasonable amount of time, Process.Kill() is called.
        /// </summary>
        public static void StopProcess(Process process)
        {
            if (process == null)
            {
                return;
            }

            try
            {
                if (process.HasExited)
                {
                    return;
                }

                int pid = process.Id;

                try
                {
                    if (AttachConsole((uint)pid))
                    {
                        SetConsoleCtrlHandler(null, true);
                        bool ctrlCSent = GenerateConsoleCtrlEvent(CtrlTypes.CTRL_C_EVENT, 0);
                        if (ctrlCSent)
                        {
                            _logger.Debug($"Sent Ctrl+C to process {pid}. Waiting for it to exit...");
                            try
                            {
                                if (!process.WaitForExit(4000))
                                {
                                    _logger.Warn($"Process {pid} did not exit within 4 seconds of Ctrl+C");
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.Error($"Exception thrown by Process.WaitForExit: {e}");
                            }
                        }
                        SetConsoleCtrlHandler(null, false);
                        FreeConsole();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"AttachConsole/Ctrl+C attempt error on PID {pid}: {ex.Message}");
                }

                if (!process.HasExited)
                {
                    _logger.Info($"Terminating process {pid} via Kill...");
                    try
                    {
                        process.Kill();
                        process.WaitForExit(3000);
                    }
                    catch (Exception e)
                    {
                        _logger.Error($"Exception thrown by Process.Kill: {e}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error in StopProcess: {ex.Message}");
            }
        }
    }
}
