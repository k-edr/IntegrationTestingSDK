using System.Text.Json.Serialization;

namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Request to spawn a grid from a blueprint.
    /// </summary>
    public class SpawnRequest
    {
        [JsonPropertyName("blueprint")]
        public string Blueprint { get; set; }

        [JsonPropertyName("position")]
        public SpawnPosition Position { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }
    }
}
