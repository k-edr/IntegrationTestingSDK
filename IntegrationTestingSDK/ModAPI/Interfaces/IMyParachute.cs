namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Parachute hatch interface. Mirrors Sandbox.ModAPI.Ingame.IMyParachute.
    /// </summary>
    public interface IMyParachute : IMyFunctionalBlock
    {
        bool AutoDeploy { get; set; }
        float AutoDeployHeight { get; set; }

        void OpenDoor();
        void CloseDoor();
        void ToggleDoor();
    }
}
