using System.Collections.Generic;
using System;
using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Enums;
using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class RemoteControlProxy : FunctionalBlockProxy, IMyRemoteControl
    {
        internal RemoteControlProxy(IPbTestHarness harness, long gridId, BlockDto block) : base(harness, gridId, block) { }

        public bool IsAutoPilotEnabled
        {
            get => bool.TryParse(GetProperty("IsAutoPilotEnabled"), out var v) && v;
            set => SetProperty("IsAutoPilotEnabled", value.ToString());
        }

        public float SpeedLimit
        {
            get => float.TryParse(GetProperty("SpeedLimit"), out var v) ? v : 100f;
            set => SetProperty("SpeedLimit", value.ToString());
        }

        public FlightMode FlightMode
        {
            get
            {
                var raw = GetProperty("FlightMode");
                if (Enum.TryParse<FlightMode>(raw, out var m)) return m;
                return FlightMode.OneWay;
            }
            set => SetProperty("FlightMode", value.ToString());
        }

        public Base6Directions.Direction Direction
        {
            get
            {
                var raw = GetProperty("Direction");
                if (Enum.TryParse<Base6Directions.Direction>(raw, out var d)) return d;
                return Base6Directions.Direction.Forward;
            }
            set => SetProperty("Direction", value.ToString());
        }

        public MyWaypointInfo CurrentWaypoint
        {
            get
            {
                var raw = GetProperty("CurrentWaypoint");
                if (string.IsNullOrEmpty(raw)) return new MyWaypointInfo(string.Empty, Vector3D.Zero);
                return new MyWaypointInfo(raw, Vector3D.Zero);
            }
        }

        public bool WaitForFreeWay
        {
            get => bool.TryParse(GetProperty("WaitForFreeWay"), out var v) && v;
            set => SetProperty("WaitForFreeWay", value.ToString());
        }

        public void ClearWaypoints() { /* Not yet supported via HTTP */ }
        public void GetWaypointInfo(List<MyWaypointInfo> waypoints) { /* Not yet supported via HTTP */ }
        public void AddWaypoint(Vector3D coords, string name) { /* Not yet supported via HTTP */ }
        public void SetAutoPilotEnabled(bool enabled) => ExecuteAction("AutoPilotEnabled");
        public void SetCollisionAvoidance(bool enabled) => ExecuteAction("CollisionAvoidance");
        public void SetDockingMode(bool enabled) => ExecuteAction("DockingMode");
    }
}
