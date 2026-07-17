using IntegrationTestingSDK.ModAPI.Enums;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for door blocks.
    /// </summary>
    public interface IMyDoor : IMyFunctionalBlock
    {
        bool Open { get; }
        DoorStatus Status { get; }
        float OpenRatio { get; }

        void OpenDoor();
        void CloseDoor();
        void ToggleDoor();
    }
}
