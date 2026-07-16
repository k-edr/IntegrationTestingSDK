using System;
using System.IO;
using System.Text.Json;
using IntegrationTestingSDK.Client;
using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.Infrastructure;

namespace IntegrationTestingSDK
{
    /// <summary>
    ///     Creates instances of <see cref="IPbTestHarness"/>.
    ///     Reads paths from <c>build-config.json</c> next to the test executable.
    /// </summary>
    public static class PbTestHarnessFactory
    {
        /// <summary>
        ///     Create a test harness for integration tests.
        ///     Reads <c>build-config.json</c> from the executing directory.
        /// </summary>
        /// <param name="scriptCode">Compiled MDK script code to upload to PBs.</param>
        public static IPbTestHarness Create(string scriptCode)
        {
            SdkLog.Init();

            var config = LoadConfig();

            var seBin64 = config.seBin64
                ?? throw new InvalidOperationException("seBin64 not configured in build-config.json");

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var seDir = Path.Combine(appData, "SpaceEngineers");
            var templatesDir = Path.Combine(seDir, "AutoWorldLoader", "Templates");

            SdkLog.Info($"seBin64: {seBin64}");
            SdkLog.Info($"templates: {templatesDir}");

            // Deploy plugin + blueprints (idempotent — skips if already done)
            PluginDeployer.Deploy(seBin64);

            var harnessConfig = new PbTestHarnessConfig
            {
                ApiPort = config.apiPort,
                HealthPollIntervalSeconds = config.healthPollIntervalSeconds,
                SessionTimeoutMinutes = config.sessionTimeoutMinutes,
                HttpTimeoutSeconds = config.httpTimeoutSeconds
            };

            SdkLog.Info($"GridSpawner API: http://localhost:{harnessConfig.ApiPort}/api/v1");

            return new PbTestHarnessHttp(
                new GameProcessManager(seBin64),
                new WorldManager(templatesDir),
                scriptCode,
                harnessConfig);
        }

        // ── Config loading ─────────────────────────────────────

        private class BuildConfig
        {
            public string seBin64 { get; set; }
            public int apiPort { get; set; } = 9997;
            public int healthPollIntervalSeconds { get; set; } = 2;
            public int sessionTimeoutMinutes { get; set; } = 3;
            public int httpTimeoutSeconds { get; set; } = 30;
        }

        private static BuildConfig LoadConfig()
        {
            // Look for build-config.json alongside the executing assembly
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var configPath = Path.Combine(exeDir, "build-config.json");

            if (!File.Exists(configPath))
            {
                // Fallback: look one level up (for test runners)
                configPath = Path.Combine(exeDir, "..", "..", "..", "build-config.json");
                configPath = Path.GetFullPath(configPath);
            }

            if (!File.Exists(configPath))
                throw new FileNotFoundException(
                    "build-config.json not found. " +
                    "Copy build-config.example.json to build-config.json and set seBin64.");

            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<BuildConfig>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (config?.seBin64 == null)
                throw new InvalidOperationException(
                    "build-config.json is missing 'seBin64'. Set it to your SpaceEngineers Bin64 path.");

            SdkLog.Info($"Loaded config: {configPath}");
            return config;
        }
    }
}
