namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Configuration for the HTTP-based test harness.
    ///     Populated from build-config.json.
    /// </summary>
    public class PbTestHarnessConfig
    {
        /// <summary>API scheme (http or https).</summary>
        public string ApiScheme { get; set; } = "http";

        /// <summary>API host (localhost or + for all interfaces).</summary>
        public string ApiHost { get; set; } = "localhost";

        /// <summary>API port of the in-game GridSpawner plugin.</summary>
        public int ApiPort { get; set; } = 9997;

        /// <summary>Seconds between health-check polls while waiting for session.</summary>
        public int HealthPollIntervalSeconds { get; set; } = 2;

        /// <summary>Minutes to wait for the game session to become ready.</summary>
        public int SessionTimeoutMinutes { get; set; } = 3;

        /// <summary>HTTP client timeout in seconds.</summary>
        public int HttpTimeoutSeconds { get; set; } = 30;

        /// <summary>Max seconds to poll LCD for non-empty content.</summary>
        public double LcdPollTimeoutSeconds { get; set; } = 0.5;

        /// <summary>Milliseconds between LCD poll attempts.</summary>
        public int LcdPollIntervalMs { get; set; } = 50;
    }
}
