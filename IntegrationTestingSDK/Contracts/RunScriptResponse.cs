using System.Text.Json.Serialization;

namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Response from <c>POST /api/v1/grids/{id}/run</c>.
    /// </summary>
    public class RunScriptResponse
    {
        [JsonPropertyName("echo")]
        public string Echo { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("argument")]
        public string Argument { get; set; }
    }
}
