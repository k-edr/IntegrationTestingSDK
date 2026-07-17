using System.Collections.Generic;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Sound block interface. Mirrors Sandbox.ModAPI.Ingame.IMySoundBlock.
    /// </summary>
    public interface IMySoundBlock : IMyFunctionalBlock
    {
        float Volume { get; set; }
        float Range { get; set; }
        float LoopPeriod { get; set; }
        string SelectedSound { get; set; }

        void Play();
        void Stop();
        List<string> GetSounds();
    }
}
