using System.Text.Json.Serialization;

namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Response from <c>GET /api/v1/grids/{id}/lcd</c>.
    /// </summary>
    public class LcdResponse
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }
}
