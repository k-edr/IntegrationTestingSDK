using System;

namespace IntegrationTestingSDK.Contracts
{
    /// <summary>
    ///     RGB color for light blocks. Serializes to the packed format
    ///     expected by the Space Engineers <c>Color</c> terminal property.
    /// </summary>
    public struct LightColor
    {
        public int R, G, B;

        public LightColor(int r, int g, int b)
        {
            R = Clamp(r);
            G = Clamp(g);
            B = Clamp(b);
        }

        public static LightColor Red    => new(255, 0, 0);
        public static LightColor Green  => new(0, 255, 0);
        public static LightColor Blue   => new(0, 0, 255);
        public static LightColor Yellow => new(255, 255, 0);
        public static LightColor White  => new(255, 255, 255);
        public static LightColor Black  => new(0, 0, 0);

        /// <summary>
        ///     Packed color value matching the game's terminal format.
        ///     The <c>Color</c> property accepts this as a string.
        /// </summary>
        public string ToPackedString()
        {
            // Terminal "Color" is a packed integer (little-endian RGB with alpha = 255).
            // Note: the exact format may vary; this is the most common SE format.
            uint packed = ((uint)R) | ((uint)G << 8) | ((uint)B << 16) | (0xFFu << 24);
            return packed.ToString();
        }

        public static bool TryParse(string packed, out LightColor color)
        {
            color = White;
            if (string.IsNullOrEmpty(packed)) return false;
            if (!uint.TryParse(packed, out var val)) return false;

            color = new LightColor(
                (int)(val & 0xFF),
                (int)((val >> 8) & 0xFF),
                (int)((val >> 16) & 0xFF));
            return true;
        }

        public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";

        private static int Clamp(int v) => Math.Max(0, Math.Min(255, v));
    }
}
