using System;
using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Enums;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class DoorProxy : FunctionalBlockProxy, IMyDoor
    {
        public bool Open
        {
            get => bool.TryParse(GetProperty("Open"), out var v) && v;
        }

        public DoorStatus Status
        {
            get
            {
                // Door terminal has no "Status" property — derive from Open
                if (Open) return DoorStatus.Open;
                return DoorStatus.Closed;
            }
        }

        public float OpenRatio
        {
            get => float.TryParse(GetProperty("OpenRatio"), out var v) ? v : 0f;
        }

        public void OpenDoor() => ExecuteAction("Open_On");
        public void CloseDoor() => ExecuteAction("Open_Off");
        public void ToggleDoor() => ExecuteAction("Open");

        internal DoorProxy(IPbTestHarness harness, long gridId, BlockDto block) : base(harness, gridId, block) { }
    }
}
