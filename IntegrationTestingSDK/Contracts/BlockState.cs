namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Snapshot of a single block's state on a grid.
    /// </summary>
    public class BlockState
    {
        public long EntityId { get; set; }
        public string Type { get; set; }
        public string Subtype { get; set; }
        public bool Enabled { get; set; }
    }
}
