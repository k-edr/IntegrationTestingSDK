using IntegrationTestingSDK.ModAPI.Enums;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for piston base blocks.
    /// </summary>
    public interface IMyPistonBase : IMyFunctionalBlock
    {
        float Velocity { get; set; }
        float MinLimit { get; set; }
        float MaxLimit { get; set; }
        float CurrentPosition { get; }
        PistonStatus Status { get; }

        void Extend();
        void Retract();
        void Reverse();
    }
}
