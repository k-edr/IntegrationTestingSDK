using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class MotorSuspensionProxy : FunctionalBlockProxy, IMyMotorSuspension
    {
        public bool Steering
        {
            get => bool.TryParse(GetProperty("Steering"), out var v) && v;
            set => SetProperty("Steering", value.ToString());
        }

        public bool Propulsion
        {
            get => bool.TryParse(GetProperty("Propulsion"), out var v) && v;
            set => SetProperty("Propulsion", value.ToString());
        }

        public float Power
        {
            get => float.TryParse(GetProperty("Power"), out var v) ? v : 100f;
            set => SetProperty("Power", value.ToString());
        }

        public float Strength
        {
            get => float.TryParse(GetProperty("Strength"), out var v) ? v : 100f;
            set => SetProperty("Strength", value.ToString());
        }

        public float Height
        {
            get => float.TryParse(GetProperty("Height"), out var v) ? v : 0f;
            set => SetProperty("Height", value.ToString());
        }

        public float MaxSteerAngle
        {
            get => float.TryParse(GetProperty("MaxSteerAngle"), out var v) ? v : 0f;
            set => SetProperty("MaxSteerAngle", value.ToString());
        }

        public float SteerAngle
        {
            get => float.TryParse(GetProperty("SteerAngle"), out var v) ? v : 0f;
        }

        public float PropulsionOverride
        {
            get => float.TryParse(GetProperty("PropulsionOverride"), out var v) ? v : 0f;
            set => SetProperty("PropulsionOverride", value.ToString());
        }

        public float SteeringOverride
        {
            get => float.TryParse(GetProperty("SteeringOverride"), out var v) ? v : 0f;
            set => SetProperty("SteeringOverride", value.ToString());
        }

        public bool AirShockEnabled
        {
            get => bool.TryParse(GetProperty("AirShockEnabled"), out var v) && v;
            set => SetProperty("AirShockEnabled", value.ToString());
        }

        public float Friction
        {
            get => float.TryParse(GetProperty("Friction"), out var v) ? v : 50f;
            set => SetProperty("Friction", value.ToString());
        }

        public float Damping
        {
            get => float.TryParse(GetProperty("Damping"), out var v) ? v : 0f;
        }

        public bool Brake
        {
            get => bool.TryParse(GetProperty("Brake"), out var v) && v;
            set => SetProperty("Brake", value.ToString());
        }

        internal MotorSuspensionProxy(IPbTestHarness harness, long gridId, BlockDto block) : base(harness, gridId, block) { }
    }
}
