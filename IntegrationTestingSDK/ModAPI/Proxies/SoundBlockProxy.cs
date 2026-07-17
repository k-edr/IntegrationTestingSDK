using System.Collections.Generic;
using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class SoundBlockProxy : FunctionalBlockProxy, IMySoundBlock
    {
        public float Volume
        {
            get => float.TryParse(GetProperty("Volume"), out var v) ? v : 0f;
            set => SetProperty("Volume", value.ToString("G"));
        }

        public float Range
        {
            get => float.TryParse(GetProperty("Range"), out var v) ? v : 0f;
            set => SetProperty("Range", value.ToString("G"));
        }

        public float LoopPeriod
        {
            get => float.TryParse(GetProperty("LoopPeriod"), out var v) ? v : 0f;
            set => SetProperty("LoopPeriod", value.ToString("G"));
        }

        public string SelectedSound
        {
            get => GetProperty("SelectedSound");
            set => SetProperty("SelectedSound", value);
        }

        public void Play() => ExecuteAction("Play");
        public void Stop() => ExecuteAction("Stop");

        public List<string> GetSounds()
            => new List<string>();

        internal SoundBlockProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
