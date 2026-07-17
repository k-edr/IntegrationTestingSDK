using System.Text.Json.Serialization;

namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Request to run a script on a programmable block.
    /// </summary>
    public class RunScriptRequest
    {
        [JsonPropertyName("argument")]
        public string Argument { get; set; }
    }
}
