using IntegrationTestingSDK.ModAPI.Enums;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Landing gear interface. Mirrors Sandbox.ModAPI.Ingame.IMyLandingGear.
    /// </summary>
    public interface IMyLandingGear : IMyFunctionalBlock
    {
        bool IsLocked { get; }
        LandingGearMode LockMode { get; set; }

        void Lock();
        void Unlock();
        void ToggleLock();
    }
}
