using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Enums;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class JumpDriveProxy : FunctionalBlockProxy, IMyJumpDrive
    {
        public float CurrentStoredPower
            => float.TryParse(GetProperty("CurrentStoredPower"), out var v) ? v : 0f;

        public float MaxStoredPower
            => float.TryParse(GetProperty("MaxStoredPower"), out var v) ? v : 0f;

        public float JumpDistanceMeters
        {
            get => float.TryParse(GetProperty("JumpDistanceMeters"), out var v) ? v : 0f;
            set => SetProperty("JumpDistanceMeters", value.ToString("G"));
        }

        public float MaxJumpDistanceMeters
            => float.TryParse(GetProperty("MaxJumpDistanceMeters"), out var v) ? v : 0f;

        public float MinJumpDistanceMeters
            => float.TryParse(GetProperty("MinJumpDistanceMeters"), out var v) ? v : 0f;

        public float JumpDistanceRatio
        {
            get => float.TryParse(GetProperty("JumpDistanceRatio"), out var v) ? v : 0f;
            set => SetProperty("JumpDistanceRatio", value.ToString("G"));
        }

        public bool Recharge
        {
            get => bool.TryParse(GetProperty("Recharge"), out var v) && v;
            set => SetProperty("Recharge", value.ToString().ToLowerInvariant());
        }

        public MyJumpDriveStatus Status
            => System.Enum.TryParse<MyJumpDriveStatus>(GetProperty("Status"), out var e) ? e : default;

        internal JumpDriveProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
