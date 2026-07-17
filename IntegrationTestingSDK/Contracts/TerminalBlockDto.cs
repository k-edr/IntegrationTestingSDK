using System.Text.Json.Serialization;

namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     SDK-side projection of the server's <c>TerminalBlockDto</c>.
    ///     Only includes fields needed by <see cref="PbTestHarnessHttp"/>.
    /// </summary>
    public class TerminalBlockDto
    {
        [JsonPropertyName("entityId")]
        public long EntityId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("definition")]
        public string Definition { get; set; }

        [JsonPropertyName("isWorking")]
        public bool IsWorking { get; set; }
    }
}
