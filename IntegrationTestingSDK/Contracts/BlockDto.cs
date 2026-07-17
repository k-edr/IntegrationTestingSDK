using System.Text.Json.Serialization;

namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Block info returned in the spawn response.
    ///     Contains the block's name, type, and grid position
    ///     (used for block-level API calls).
    /// </summary>
    public class BlockDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("gridPosition")]
        public Vector3IDto GridPosition { get; set; }

        /// <summary>
        ///     Entity ID assigned by the game when the block is spawned.
        ///     Set from the spawn response if available; defaults to 0.
        /// </summary>
        [JsonPropertyName("entityId")]
        public long EntityId { get; set; }
    }
}
