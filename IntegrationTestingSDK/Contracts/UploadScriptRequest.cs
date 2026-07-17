using System.Text.Json.Serialization;

namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Request to upload code to a programmable block.
    /// </summary>
    public class UploadScriptRequest
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }
    }
}
