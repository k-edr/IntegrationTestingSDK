namespace IntegrationTestingSDK.ModAPI.Types
{
    /// <summary>
    ///     Detected entity info matching Sandbox.ModAPI.Ingame.MyDetectedEntityInfo.
    /// </summary>
    public struct MyDetectedEntityInfo
    {
        public long EntityId { get; set; }
        public string Name { get; set; }
        public Enums.MyDetectedEntityType Type { get; set; }
        public Vector3D Position { get; set; }

        public bool IsEmpty => EntityId == 0;
    }
}
