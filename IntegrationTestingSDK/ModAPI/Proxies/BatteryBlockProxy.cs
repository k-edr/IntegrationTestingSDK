using System;
using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Enums;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class BatteryBlockProxy : FunctionalBlockProxy, IMyBatteryBlock
    {
        public bool HasCapacityRemaining
        {
            get => bool.TryParse(GetProperty("HasCapacityRemaining"), out var v) && v;
        }

        public float CurrentStoredPower
        {
            get => float.TryParse(GetProperty("CurrentStoredPower"), out var v) ? v : 0f;
        }

        public float MaxStoredPower
        {
            get => float.TryParse(GetProperty("MaxStoredPower"), out var v) ? v : 0f;
        }

        public float CurrentInput
        {
            get => float.TryParse(GetProperty("CurrentInput"), out var v) ? v : 0f;
        }

        public float MaxInput
        {
            get => float.TryParse(GetProperty("MaxInput"), out var v) ? v : 0f;
        }

        public bool IsCharging
        {
            get => bool.TryParse(GetProperty("IsCharging"), out var v) && v;
        }

        public ChargeMode ChargeMode
        {
            get => long.TryParse(GetProperty("ChargeMode"), out var l) ? (ChargeMode)l : ChargeMode.Auto;
            set => SetProperty("ChargeMode", ((int)value).ToString());
        }

        public bool OnlyRecharge
        {
            get => bool.TryParse(GetProperty("OnlyRecharge"), out var v) && v;
            set => SetProperty("OnlyRecharge", value.ToString());
        }

        public bool OnlyDischarge
        {
            get => bool.TryParse(GetProperty("OnlyDischarge"), out var v) && v;
            set => SetProperty("OnlyDischarge", value.ToString());
        }

        public bool SemiautoEnabled
        {
            get => bool.TryParse(GetProperty("SemiautoEnabled"), out var v) && v;
            set => SetProperty("SemiautoEnabled", value.ToString());
        }

        internal BatteryBlockProxy(IPbTestHarness harness, long gridId, BlockDto block) : base(harness, gridId, block) { }
    }
}
