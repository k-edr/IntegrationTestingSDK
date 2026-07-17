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
        public List<GridRef> Grids { get; set; } = new();
    }

    /// <summary>
    ///     Minimal grid reference returned in spawn response.
    /// </summary>
    public class GridRef
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
    }
}
