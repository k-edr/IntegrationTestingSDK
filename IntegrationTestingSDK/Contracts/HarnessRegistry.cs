using System;

namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Static registry for the in-game harness.
    ///     Set by the Plugin's <c>SessionComponent</c> after world load.
    ///     Read by <c>PbTestHarnessInGame</c> from the SDK client side.
    /// </summary>
    public static class HarnessRegistry
    {
        /// <summary>
        ///     The active in-game harness, or null if the game plugin isn't loaded.
        /// </summary>
        public static IInGameHarness Current { get; set; }
    }
}
