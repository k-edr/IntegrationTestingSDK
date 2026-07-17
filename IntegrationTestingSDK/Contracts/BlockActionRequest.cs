using System.Text.Json.Serialization;

namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Request to execute a terminal block action.
    /// </summary>
    public class BlockActionRequest
    {
        [JsonPropertyName("actionId")]
        public string ActionId { get; set; }
    }

    /// <summary>
    ///     Request to set a terminal block property.
    /// </summary>
    public class SetPropertyRequest
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    /// <summary>
    ///     Response from <c>GET .../properties/{propId}</c>.
    /// </summary>
    public class PropertyResponse
    {
        [JsonPropertyName("propertyId")]
        public string PropertyId { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}
