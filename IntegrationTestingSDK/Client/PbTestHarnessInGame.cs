using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.Infrastructure;

namespace IntegrationTestingSDK.Client
{
    /// <summary>
    ///     Test harness for integration tests.
    ///     <see cref="StartWorld"/> / <see cref="StopWorld"/> manage game lifecycle
    ///     via <see cref="GameProcessManager"/> + <see cref="WorldManager"/>.
    ///     In-game operations delegate to <see cref="IInGameHarness"/>
    ///     registered via <see cref="HarnessRegistry"/>.
    /// </summary>
    internal sealed class PbTestHarnessInGame : IPbTestHarness
    {
        private readonly GameProcessManager _game;
        private readonly WorldManager _worlds;
        private readonly string _scriptCode;
        private string _workingCopyName;
        private bool _disposed;

        public PbTestHarnessInGame(
            GameProcessManager game,
            WorldManager worlds,
            string scriptCode)
        {
            _game = game;
            _worlds = worlds;
            _scriptCode = scriptCode;
        }

        public void StartWorld(string templateWorldName, string workingCopyName)
        {
            _workingCopyName = workingCopyName;
            _worlds.CopyWorld(templateWorldName, workingCopyName);

            // Configure AutoWorldLoader to load this specific world
            WriteAutoWorldLoaderConfig(workingCopyName);

            _game.Launch(workingCopyName);

            // Wait for the game session to be ready (plugin loaded, world running)
            WaitForSession(TimeSpan.FromMinutes(3));
        }

        public void StopWorld()
        {
            _game.Kill();
            if (_workingCopyName != null)
            {
                _worlds.DeleteWorld(_workingCopyName);
                _workingCopyName = null;
            }
        }

        public IReadOnlyList<long> SpawnTestGrid(string blueprintName, double x, double y, double z)
        {
            return Resolve().SpawnTestGrid(blueprintName, x, y, z);
        }

        public void RemoveTestGrid(long gridId)
        {
            Resolve().RemoveTestGrid(gridId);
        }

        public void UploadScript(long gridId)
        {
            if (string.IsNullOrEmpty(_scriptCode))
                throw new InvalidOperationException("No script code provided.");
            Resolve().SetScriptCode(gridId, _scriptCode);
        }

        public string RunScript(long gridId, string argument = null)
        {
            return Resolve().RunScript(gridId, argument);
        }

        public string GetLcdContent(long gridId)
        {
            return Resolve().GetLcdContent(gridId);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { StopWorld(); } catch { }
        }

        // ── World lifecycle helpers ────────────────────────────

        private static void WriteAutoWorldLoaderConfig(string worldName)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configPath = Path.Combine(appData, "SpaceEngineers", "AutoWorldLoader.json");
            var dir = Path.GetDirectoryName(configPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = $"{{\"worldName\": \"{worldName}\"}}";
            File.WriteAllText(configPath, json);
            SdkLog.Info($"AutoWorldLoader config written: {configPath} → {worldName}");
        }

        private static void WaitForSession(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            var pollInterval = TimeSpan.FromSeconds(2);

            SdkLog.Info($"Waiting for session (timeout: {timeout.TotalSeconds:N0}s)...");

            while (DateTime.UtcNow < deadline)
            {
                var harness = HarnessRegistry.Current;
                if (harness != null && harness.IsReady)
                {
                    SdkLog.Info($"Session ready after {timeout.TotalSeconds - (deadline - DateTime.UtcNow).TotalSeconds:N0}s");
                    return;
                }

                Thread.Sleep(pollInterval);
            }

            throw new TimeoutException(
                $"Timed out waiting for game session after {timeout.TotalSeconds:N0}s. " +
                "Is AutoWorldLoader + IntegrationTestingSDK.Plugin installed?");
        }

        private static IInGameHarness Resolve()
        {
            var h = HarnessRegistry.Current;
            if (h == null)
                throw new InvalidOperationException(
                    "Harness not registered. Is IntegrationTestingSDK.Plugin loaded in the game?");
            return h;
        }
    }
}
