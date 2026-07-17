namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Base entity interface mirroring VRage.Game.ModAPI.Ingame.IMyEntity.
    /// </summary>
    public interface IMyEntity
    {
        long EntityId { get; }
        string DisplayName { get; }
    }
}
