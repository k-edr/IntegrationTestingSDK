using System;
using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Enums;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class PistonBaseProxy : FunctionalBlockProxy, IMyPistonBase
    {
        public float Velocity
        {
            get => float.TryParse(GetProperty("Velocity"), out var v) ? v : 0f;
            set => SetProperty("Velocity", value.ToString());
        }

        public float MinLimit
        {
            get => float.TryParse(GetProperty("LowerLimit"), out var v) ? v : 0f;
            set => SetProperty("LowerLimit", value.ToString());
        }

        public float MaxLimit
        {
            get => float.TryParse(GetProperty("UpperLimit"), out var v) ? v : 10f;
            set => SetProperty("UpperLimit", value.ToString());
        }

        public float CurrentPosition
        {
            get => float.TryParse(GetProperty("CurrentPosition"), out var v) ? v : 0f;
        }

        public PistonStatus Status
        {
            get
            {
                var raw = GetProperty("Status");
                if (Enum.TryParse<PistonStatus>(raw, out var s)) return s;
                return PistonStatus.Stopped;
            }
        }

        public void Extend() => ExecuteAction("Extend");
        public void Retract() => ExecuteAction("Retract");
        public void Reverse() => ExecuteAction("Reverse");

        internal PistonBaseProxy(IPbTestHarness harness, long gridId, BlockDto block) : base(harness, gridId, block) { }
    }
}
