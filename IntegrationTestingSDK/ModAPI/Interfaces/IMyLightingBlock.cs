using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for lighting blocks (interior lights, spotlights, etc.).
    /// </summary>
    public interface IMyLightingBlock : IMyFunctionalBlock
    {
        Color Color { get; set; }
        float Radius { get; set; }
        float Intensity { get; set; }
        float Falloff { get; set; }
        float BlinkIntervalSeconds { get; set; }
        float BlinkLength { get; set; }
        float BlinkOffset { get; set; }
    }
}
