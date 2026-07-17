using System.Collections.Generic;
using IntegrationTestingSDK.ModAPI.Enums;
using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for remote control blocks.
    ///     In the proxy layer, proxies that implement this should also implement IMyShipController.
    /// </summary>
    public interface IMyRemoteControl
    {
        bool IsAutoPilotEnabled { get; set; }
        float SpeedLimit { get; set; }
        FlightMode FlightMode { get; set; }
        Base6Directions.Direction Direction { get; set; }
        MyWaypointInfo CurrentWaypoint { get; }
        bool WaitForFreeWay { get; set; }

        void ClearWaypoints();
        void GetWaypointInfo(List<MyWaypointInfo> waypoints);
        void AddWaypoint(Vector3D coords, string name);
        void SetAutoPilotEnabled(bool enabled);
        void SetCollisionAvoidance(bool enabled);
        void SetDockingMode(bool enabled);
    }
}
