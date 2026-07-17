namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Safe zone block interface. Mirrors Sandbox.ModAPI.Ingame.IMySafeZoneBlock.
    /// </summary>
    public interface IMySafeZoneBlock : IMyFunctionalBlock
    {
        bool IsSafeZoneEnabled { get; }

        void EnableSafeZone(bool enable);
    }
}
