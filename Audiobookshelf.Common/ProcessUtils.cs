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
            if (process == null) return;

            try
            {
                if (process.HasExited) return;

                int pid = process.Id;
                _logger.Info($"Initiating graceful shutdown for process {pid} ({process.ProcessName})...");

                // 1. If standard input is redirected, close standard input to trigger EOF
                try
                {
                    if (process.StartInfo.RedirectStandardInput && !process.HasExited)
                    {
                        process.StandardInput.Close();
                    }
                }
                catch { }

                // 2. Attempt to attach console and send CTRL_C_EVENT or CTRL_BREAK_EVENT
                try
                {
                    if (AttachConsole((uint)pid))
                    {
                        SetConsoleCtrlHandler(null, true);
                        bool sent = GenerateConsoleCtrlEvent(CtrlTypes.CTRL_C_EVENT, 0);
                        if (!sent)
                        {
                            sent = GenerateConsoleCtrlEvent(CtrlTypes.CTRL_BREAK_EVENT, 0);
                        }

                        if (sent)
                        {
                            _logger.Debug($"Sent console break signal to process {pid}. Waiting up to 5 seconds for database flush...");
                            if (process.WaitForExit(5000))
                            {
                                _logger.Info($"Process {pid} exited gracefully.");
                                SetConsoleCtrlHandler(null, false);
                                FreeConsole();
                                return;
                            }
                        }
                        SetConsoleCtrlHandler(null, false);
                        FreeConsole();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Console break signal exception on PID {pid}: {ex.Message}");
                }

                // 3. Try CloseMainWindow if process has a UI or message pump
                try
                {
                    if (!process.HasExited)
                    {
                        process.CloseMainWindow();
                        if (process.WaitForExit(1000))
                        {
                            _logger.Info($"Process {pid} exited following CloseMainWindow.");
                            return;
                        }
                    }
                }
                catch { }

                // 4. Force kill if process failed to exit gracefully
                if (!process.HasExited)
                {
                    _logger.Warn($"Process {pid} did not exit gracefully within timeout. Forcing termination via Kill()...");
                    try
                    {
                        process.Kill();
                        process.WaitForExit(3000);
                        _logger.Info($"Process {pid} terminated via Kill().");
                    }
                    catch (Exception e)
                    {
                        _logger.Error($"Exception during Process.Kill for PID {pid}: {e}");
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
