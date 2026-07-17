using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Response from <c>POST /api/v1/spawn</c>.
    /// </summary>
    public class SpawnResponse
    {
        [JsonPropertyName("grids")]
        public List<GridDto> Grids { get; set; } = new();
    }

    /// <summary>
    ///     Grid info returned in the spawn response, including block list.
    /// </summary>
    public class GridDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("position")]
        public SpawnPosition Position { get; set; }

        [JsonPropertyName("blocks")]
        public List<BlockDto> Blocks { get; set; } = new();
    }
}
