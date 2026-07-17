using IntegrationTestingSDK.ModAPI.Enums;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for motor stator (rotor) blocks.
    /// </summary>
    public interface IMyMotorStator : IMyFunctionalBlock
    {
        float Angle { get; }
        float Torque { get; set; }
        float BrakingTorque { get; set; }
        float TargetVelocityRPM { get; set; }
        float TargetVelocityRad { get; set; }
        float LowerLimitDeg { get; set; }
        float UpperLimitDeg { get; set; }
        float LowerLimitRad { get; set; }
        float UpperLimitRad { get; set; }
        float Displacement { get; set; }
        bool RotorLock { get; set; }

        void RotateToAngle(MyRotationDirection dir, float angle, float velocity);
    }
}
