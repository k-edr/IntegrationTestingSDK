namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Base class for typed block wrappers.
    ///     Holds a reference to the parent grid and block position,
    ///     delegating API calls to <see cref="SpawnedGrid"/>.
    /// </summary>
    public abstract class BlockWrapper
    {
        /// <summary>Parent grid that handles API calls.</summary>
        protected SpawnedGrid Grid { get; }

        /// <summary>Block metadata (name, position, type).</summary>
        protected BlockDto Block { get; }

        /// <summary>Block name from the blueprint.</summary>
        public string Name => Block.Name;

        internal BlockWrapper(SpawnedGrid grid, BlockDto block)
        {
            Grid = grid;
            Block = block;
        }
    }
}
