using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.Plugin.Application;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace IntegrationTestingSDK.Plugin.Infrastructure
{
    /// <summary>
    ///     Session component: composition root.
    ///     Initialises <see cref="PbTestService"/> and registers it
    ///     in <see cref="HarnessRegistry"/> for SDK-side access.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, 1000)]
    public class SessionComponent : MySessionComponentBase
    {
        private PbTestService _testService;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            Logger.Info("SessionComponent.Init — starting PbTestService");

            _testService = new PbTestService();
            HarnessRegistry.Current = _testService;

            Logger.Info("Test harness registered (direct ModAPI)");
        }

        public override void UpdateAfterSimulation()
        {
            _testService?.ProcessQueue();
        }

        protected override void UnloadData()
        {
            HarnessRegistry.Current = null;
            _testService?.Dispose();
            Logger.Info("Test harness unregistered");
        }
    }
}
