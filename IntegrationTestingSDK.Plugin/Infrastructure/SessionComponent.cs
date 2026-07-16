using IntegrationTestingSDK.Plugin.Application;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace IntegrationTestingSDK.Plugin.Infrastructure
{
    /// <summary>
    ///     Session component: composition root.
    ///     Creates <see cref="PbTestService"/> and starts <see cref="HttpApiServer"/>
    ///     for SDK-side HTTP access.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, 1000)]
    public class SessionComponent : MySessionComponentBase
    {
        private PbTestService _testService;
        private HttpApiServer _apiServer;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            Logger.Info("SessionComponent.Init — starting PbTestService + HTTP API");

            _testService = new PbTestService();
            _apiServer = new HttpApiServer(_testService);
            _apiServer.Start();

            Logger.Info("HTTP test harness started on port 9980");
        }

        public override void UpdateAfterSimulation()
        {
            _testService?.ProcessQueue();
        }

        protected override void UnloadData()
        {
            _apiServer?.Dispose();
            _testService?.Dispose();
            Logger.Info("HTTP test harness stopped");
        }
    }
}
