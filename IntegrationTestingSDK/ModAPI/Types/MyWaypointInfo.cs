namespace IntegrationTestingSDK.ModAPI.Types
{
    /// <summary>
    ///     Waypoint info matching Sandbox.ModAPI.Ingame.MyWaypointInfo.
    /// </summary>
    public struct MyWaypointInfo
    {
        public string Name { get; }
        public Vector3D Coords { get; }

        public MyWaypointInfo(string name, Vector3D coords)
        {
            Name = name;
            Coords = coords;
        }

        public override string ToString() => $"{Name}: {Coords}";
    }
}
