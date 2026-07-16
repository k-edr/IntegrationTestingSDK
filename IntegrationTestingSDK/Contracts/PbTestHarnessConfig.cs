namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Configuration for the HTTP-based test harness.
    ///     Populated from build-config.json.
    /// </summary>
    public class PbTestHarnessConfig
    {
        /// <summary>API port of the in-game GridSpawner plugin.</summary>
        public int ApiPort { get; set; } = 9997;

        /// <summary>Seconds between health-check polls while waiting for session.</summary>
        public int HealthPollIntervalSeconds { get; set; } = 2;

        /// <summary>Minutes to wait for the game session to become ready.</summary>
        public int SessionTimeoutMinutes { get; set; } = 3;

        /// <summary>HTTP client timeout in seconds.</summary>
        public int HttpTimeoutSeconds { get; set; } = 30;
    }
}
