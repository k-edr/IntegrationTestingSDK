namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for warhead blocks.
    ///     Inherits from IMyTerminalBlock directly (not IMyFunctionalBlock).
    /// </summary>
    public interface IMyWarhead : IMyTerminalBlock
    {
        bool IsCountingDown { get; }
        float DetonationTime { get; set; }
        bool IsArmed { get; set; }

        void StartCountdown();
        void StopCountdown();
        void Detonate();
    }
}
