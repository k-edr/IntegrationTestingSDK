using System.Text.Json.Serialization;

namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     3D position for spawn requests and responses.
    /// </summary>
    public class SpawnPosition
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("z")]
        public double Z { get; set; }
    }
}
