using System;

namespace IntegrationTestingSDK.ModAPI.Types
{
    /// <summary>
    ///     RGB color matching VRageMath.Color. Serializes to/from the packed uint32
    ///     format used by the Space Engineers terminal "Color" property (ARGB, little-endian).
    /// </summary>
    public struct Color
    {
        public byte R, G, B, A;

        public Color(int r, int g, int b, int a = 255)
        {
            R = (byte)Clamp(r, 0, 255);
            G = (byte)Clamp(g, 0, 255);
            B = (byte)Clamp(b, 0, 255);
            A = (byte)Clamp(a, 0, 255);
        }

        public Color(float r, float g, float b, float a = 1f)
        {
            R = (byte)Clamp((int)(r * 255), 0, 255);
            G = (byte)Clamp((int)(g * 255), 0, 255);
            B = (byte)Clamp((int)(b * 255), 0, 255);
            A = (byte)Clamp((int)(a * 255), 0, 255);
        }

        public uint PackedValue => ((uint)R) | ((uint)G << 8) | ((uint)B << 16) | ((uint)A << 24);

        public string ToPackedString() => $"#{R:X2}{G:X2}{B:X2}";

        public static Color FromPacked(uint packed) => new(
            (int)(packed & 0xFF),
            (int)((packed >> 8) & 0xFF),
            (int)((packed >> 16) & 0xFF),
            (int)((packed >> 24) & 0xFF));

        public static bool TryParse(string s, out Color color)
        {
            color = White;
            if (string.IsNullOrEmpty(s)) return false;

            // Try "#RRGGBB" hex format (both client and server use this)
            if (s.StartsWith("#") && s.Length == 7)
            {
                try
                {
                    color = new Color(
                        Convert.ToInt32(s.Substring(1, 2), 16),
                        Convert.ToInt32(s.Substring(3, 2), 16),
                        Convert.ToInt32(s.Substring(5, 2), 16));
                    return true;
                }
                catch { return false; }
            }

            // Try packed uint format (legacy)
            if (!uint.TryParse(s, out var val)) return false;
            color = FromPacked(val);
            return true;
        }

        // Named presets (matching game)
        public static Color Red => new(255, 0, 0);
        public static Color Green => new(0, 255, 0);
        public static Color Blue => new(0, 0, 255);
        public static Color Yellow => new(255, 255, 0);
        public static Color White => new(255, 255, 255);
        public static Color Black => new(0, 0, 0);
        public static Color Transparent => new(0, 0, 0, 0);
        public static Color Orange => new(255, 128, 0);
        public static Color Cyan => new(0, 255, 255);
        public static Color Magenta => new(255, 0, 255);
        public static Color Gray => new(128, 128, 128);
        public static Color DarkGray => new(64, 64, 64);

        public Vector3 ToVector3() => new(R / 255f, G / 255f, B / 255f);

        public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
        public override bool Equals(object obj) => obj is Color c && c.PackedValue == PackedValue;
        public override int GetHashCode() => (int)PackedValue;
        public static bool operator ==(Color a, Color b) => a.PackedValue == b.PackedValue;
        public static bool operator !=(Color a, Color b) => a.PackedValue != b.PackedValue;
        public static Color operator *(Color c, float f) => new((int)(c.R * f), (int)(c.G * f), (int)(c.B * f), c.A);

        private static int Clamp(int v, int min, int max) => Math.Max(min, Math.Min(max, v));
    }
}
