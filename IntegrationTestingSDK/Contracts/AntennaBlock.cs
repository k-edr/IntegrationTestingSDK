namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Typed wrapper around an antenna block.
    /// </summary>
    public class AntennaBlock : BlockWrapper
    {
        internal AntennaBlock(SpawnedGrid grid, BlockDto block)
            : base(grid, block) { }

        /// <summary>Turn the antenna on.</summary>
        public bool TurnOn() => Grid.ExecuteBlockAction(Block, "OnOff_On");

        /// <summary>Turn the antenna off.</summary>
        public bool TurnOff() => Grid.ExecuteBlockAction(Block, "OnOff_Off");
    }
}
