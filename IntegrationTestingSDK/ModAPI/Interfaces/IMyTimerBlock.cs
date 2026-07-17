namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Timer block interface. Mirrors Sandbox.ModAPI.Ingame.IMyTimerBlock.
    /// </summary>
    public interface IMyTimerBlock : IMyFunctionalBlock
    {
        bool Silent { get; set; }

        void Trigger();
        void StartCountdown();
        void StopCountdown();
    }
}
