using System;
using VRage.Plugins;

namespace IntegrationTestingSDK.Plugin
{
    /// <summary>
    ///     Plugin entry point.
    ///     HTTP test harness starts in SessionComponent.Init() after world loads.
    ///     Listens on port 9980.
    /// </summary>
    public class Plugin : IPlugin
    {
        public void Init(object gameInstance)
        {
            Infrastructure.Logger.Init();
            Infrastructure.Logger.Info("IntegrationTestingSDK.Plugin loaded. Waiting for world...");
        }

        public void Update() { }
        public void Dispose() { }
    }
}
