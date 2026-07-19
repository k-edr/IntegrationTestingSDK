using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IntegrationTestingSDK.Contracts
{
    public class BlockDetailDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("gridPosition")]
        public Vector3IDto GridPosition { get; set; }

        [JsonPropertyName("entityId")]
        public long EntityId { get; set; }

        [JsonPropertyName("definition")]
        public string Definition { get; set; }

        [JsonPropertyName("actions")]
        public IReadOnlyList<BlockActionDto> Actions { get; set; }

        [JsonPropertyName("properties")]
        public IReadOnlyList<BlockPropertyDto> Properties { get; set; }
    }

    public class BlockActionDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class BlockPropertyDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("propertyType")]
        public string PropertyType { get; set; }
    }
}
