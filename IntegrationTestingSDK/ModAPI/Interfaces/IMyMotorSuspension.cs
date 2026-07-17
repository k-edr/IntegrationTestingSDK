namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for motor suspension (wheel) blocks.
    /// </summary>
    public interface IMyMotorSuspension : IMyFunctionalBlock
    {
        bool Steering { get; set; }
        bool Propulsion { get; set; }
        float Power { get; set; }
        float Strength { get; set; }
        float Height { get; set; }
        float MaxSteerAngle { get; set; }
        float SteerAngle { get; }
        float PropulsionOverride { get; set; }
        float SteeringOverride { get; set; }
        bool AirShockEnabled { get; set; }
        float Friction { get; set; }
        float Damping { get; }
        bool Brake { get; set; }
    }
}
