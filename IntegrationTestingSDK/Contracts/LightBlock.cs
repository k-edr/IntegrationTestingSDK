namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     Typed wrapper around a light block. Exposes terminal actions (OnOff)
    ///     and the <c>Color</c> property with <see cref="LightColor"/>.
    /// </summary>
    public class LightBlock : BlockWrapper
    {
        internal LightBlock(SpawnedGrid grid, BlockDto block)
            : base(grid, block) { }

        /// <summary>Turn the light on.</summary>
        public bool TurnOn() => Grid.ExecuteBlockAction(Block, "OnOff_On");

        /// <summary>Turn the light off.</summary>
        public bool TurnOff() => Grid.ExecuteBlockAction(Block, "OnOff_Off");

        /// <summary>
        ///     The current color of the light.
        ///     Setting it immediately writes to the terminal <c>Color</c> property.
        /// </summary>
        public LightColor Color
        {
            get
            {
                var raw = Grid.GetBlockProperty(Block, "Color");
                return LightColor.TryParse(raw, out var c) ? c : LightColor.White;
            }
            set => Grid.SetBlockProperty(Block, "Color", value.ToPackedString());
        }

        /// <summary>Set the light color from RGB components (0–255).</summary>
        public bool SetColor(int r, int g, int b)
        {
            Color = new LightColor(r, g, b);
            return true;
        }
    }
}
