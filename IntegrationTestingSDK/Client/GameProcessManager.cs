using System;
using System.Diagnostics;
using System.IO;
using IntegrationTestingSDK.Infrastructure;

namespace IntegrationTestingSDK.Client
{
    /// <summary>
    ///     Manages the Space Engineers game process:
    ///     launch via <c>SpaceEngineersLauncher.exe</c>, kill on demand.
    /// </summary>
    internal sealed class GameProcessManager
    {
        private readonly string _seExePath;
        private Process _process;

        public GameProcessManager(string seBin64)
        {
            _seExePath = Path.Combine(seBin64, "SpaceEngineersLauncher.exe");
            if (!File.Exists(_seExePath))
                throw new FileNotFoundException(
                    $"SpaceEngineersLauncher.exe not found at {_seExePath}. " +
                    "Update seBin64 path in build-config.json.");
        }

        public void Launch(string worldName)
        {
            SdkLog.Info($"Launching SE: {_seExePath}");
            KillExisting();

            var psi = new ProcessStartInfo
            {
                FileName = _seExePath,
                Arguments = $"-appdata \"%APPDATA%\\SpaceEngineers\"",
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(_seExePath)
            };

            _process = Process.Start(psi);

            if (_process == null)
            {
                SdkLog.Error("Failed to start SpaceEngineersLauncher.exe");
                throw new InvalidOperationException("Failed to start SpaceEngineersLauncher.exe");
            }

            SdkLog.Info($"SE process started, PID={_process.Id}");
        }

        public void Kill()
        {
            if (_process == null || _process.HasExited)
            {
                SdkLog.Info("Kill: process already exited or null, skipping");
                return;
            }

            SdkLog.Info($"Killing SE process PID={_process.Id}...");
            try
            {
                _process.Kill();
                _process.WaitForExit(10_000);
                SdkLog.Info("SE process killed successfully");
            }
            catch (Exception ex)
            {
                SdkLog.Error("Failed to kill SE process", ex);
            }
            finally
            {
                _process?.Dispose();
                _process = null;
            }
        }

        public static void KillExisting()
        {
            foreach (var name in new[] { "SpaceEngineers", "SpaceEngineersLauncher" })
            {
                var procs = Process.GetProcessesByName(name);
                if (procs.Length == 0)
                    continue;

                SdkLog.Info($"Killing {procs.Length} existing {name} process(es)...");
                foreach (var proc in procs)
                {
                    try
                    {
                        SdkLog.Info($"  PID={proc.Id}");
                        proc.Kill();
                        proc.WaitForExit(10_000);
                        SdkLog.Info($"  PID={proc.Id} killed");
                    }
                    catch (Exception ex)
                    {
                        SdkLog.Error($"  Failed to kill PID={proc.Id}", ex);
                    }
                    finally
                    {
                        proc?.Dispose();
                    }
                }
            }
        }
    }
}
