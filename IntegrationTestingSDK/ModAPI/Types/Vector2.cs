using System;

namespace IntegrationTestingSDK.ModAPI.Types
{
    /// <summary>
    ///     Single-precision 2D vector matching VRageMath.Vector2.
    /// </summary>
    public struct Vector2 : IEquatable<Vector2>
    {
        public float X, Y;

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 Zero => new(0, 0);
        public static Vector2 One => new(1, 1);
        public static Vector2 UnitX => new(1, 0);
        public static Vector2 UnitY => new(0, 1);

        public float Length() => (float)Math.Sqrt(X * X + Y * Y);
        public float LengthSquared() => X * X + Y * Y;

        public Vector2 Normalized()
        {
            var len = Length();
            if (len < float.Epsilon)
                return Zero;
            return new Vector2(X / len, Y / len);
        }

        public static float Dot(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;
        public float Dot(Vector2 other) => Dot(this, other);

        public static float Distance(Vector2 a, Vector2 b) => (a - b).Length();
        public float Distance(Vector2 other) => Distance(this, other);

        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator *(Vector2 a, float s) => new(a.X * s, a.Y * s);
        public static Vector2 operator *(float s, Vector2 a) => new(a.X * s, a.Y * s);
        public static Vector2 operator /(Vector2 a, float s) => new(a.X / s, a.Y / s);
        public static Vector2 operator -(Vector2 a) => new(-a.X, -a.Y);

        public static bool operator ==(Vector2 a, Vector2 b) =>
            Math.Abs(a.X - b.X) < float.Epsilon &&
            Math.Abs(a.Y - b.Y) < float.Epsilon;

        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);

        public override string ToString() => $"({X}, {Y})";
        public override bool Equals(object obj) => obj is Vector2 other && this == other;
        public bool Equals(Vector2 other) => this == other;
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + X.GetHashCode();
                hash = hash * 31 + Y.GetHashCode();
                return hash;
            }
        }
    }
}
