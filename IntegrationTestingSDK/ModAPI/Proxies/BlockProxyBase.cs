using IntegrationTestingSDK.Contracts;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    /// <summary>
    ///     Base class for all block proxies. Provides access to the PB test harness
    ///     and common helpers for getting/setting terminal properties and executing actions.
    /// </summary>
    internal abstract class BlockProxyBase
    {
        protected readonly IPbTestHarness _harness;
        protected readonly long _gridId;
        protected readonly BlockDto _block;

        protected int _x => _block.GridPosition.X;
        protected int _y => _block.GridPosition.Y;
        protected int _z => _block.GridPosition.Z;

        protected BlockProxyBase(IPbTestHarness harness, long gridId, BlockDto block)
        {
            _harness = harness;
            _gridId = gridId;
            _block = block;
        }

        protected string GetProperty(string propertyId)
            => _harness.GetBlockProperty(_gridId, _x, _y, _z, propertyId);

        protected void SetProperty(string propertyId, string value)
            => _harness.SetBlockProperty(_gridId, _x, _y, _z, propertyId, value);

        protected void ExecuteAction(string actionId)
            => _harness.ExecuteBlockAction(_gridId, _x, _y, _z, actionId);
    }
}
