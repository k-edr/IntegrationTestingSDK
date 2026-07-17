using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Enums;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class MotorStatorProxy : FunctionalBlockProxy, IMyMotorStator
    {
        public float Angle
        {
            get => float.TryParse(GetProperty("Angle"), out var v) ? v : 0f;
        }

        public float Torque
        {
            get => float.TryParse(GetProperty("Torque"), out var v) ? v : 0f;
            set => SetProperty("Torque", value.ToString());
        }

        public float BrakingTorque
        {
            get => float.TryParse(GetProperty("BrakingTorque"), out var v) ? v : 0f;
            set => SetProperty("BrakingTorque", value.ToString());
        }

        public float TargetVelocityRPM
        {
            get => float.TryParse(GetProperty("TargetVelocityRPM"), out var v) ? v : 0f;
            set => SetProperty("TargetVelocityRPM", value.ToString());
        }

        public float TargetVelocityRad
        {
            get => float.TryParse(GetProperty("TargetVelocityRad"), out var v) ? v : 0f;
            set => SetProperty("TargetVelocityRad", value.ToString());
        }

        public float LowerLimitDeg
        {
            get => float.TryParse(GetProperty("LowerLimitDeg"), out var v) ? v : -360f;
            set => SetProperty("LowerLimitDeg", value.ToString());
        }

        public float UpperLimitDeg
        {
            get => float.TryParse(GetProperty("UpperLimitDeg"), out var v) ? v : 360f;
            set => SetProperty("UpperLimitDeg", value.ToString());
        }

        public float LowerLimitRad
        {
            get => float.TryParse(GetProperty("LowerLimitRad"), out var v) ? v : 0f;
            set => SetProperty("LowerLimitRad", value.ToString());
        }

        public float UpperLimitRad
        {
            get => float.TryParse(GetProperty("UpperLimitRad"), out var v) ? v : 0f;
            set => SetProperty("UpperLimitRad", value.ToString());
        }

        public float Displacement
        {
            get => float.TryParse(GetProperty("Displacement"), out var v) ? v : 0f;
            set => SetProperty("Displacement", value.ToString());
        }

        public bool RotorLock
        {
            get => bool.TryParse(GetProperty("RotorLock"), out var v) && v;
            set => SetProperty("RotorLock", value.ToString());
        }

        public void RotateToAngle(MyRotationDirection dir, float angle, float velocity)
            => ExecuteAction("RotateToAngle");

        internal MotorStatorProxy(IPbTestHarness harness, long gridId, BlockDto block) : base(harness, gridId, block) { }
    }
}
