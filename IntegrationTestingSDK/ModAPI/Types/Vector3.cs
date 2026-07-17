using System;

namespace IntegrationTestingSDK.ModAPI.Types
{
    /// <summary>
    ///     Single-precision 3D vector matching VRageMath.Vector3.
    /// </summary>
    public struct Vector3 : IEquatable<Vector3>
    {
        public float X, Y, Z;

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3(float value)
        {
            X = value;
            Y = value;
            Z = value;
        }

        public static Vector3 Zero => new(0);
        public static Vector3 One => new(1);
        public static Vector3 UnitX => new(1, 0, 0);
        public static Vector3 UnitY => new(0, 1, 0);
        public static Vector3 UnitZ => new(0, 0, 1);

        public float Length() => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        public float LengthSquared() => X * X + Y * Y + Z * Z;

        public Vector3 Normalized()
        {
            var len = Length();
            if (len < float.Epsilon)
                return Zero;
            return new Vector3(X / len, Y / len, Z / len);
        }

        public static float Dot(Vector3 a, Vector3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public float Dot(Vector3 other) => Dot(this, other);

        public static Vector3 Cross(Vector3 a, Vector3 b) => new(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);

        public Vector3 Cross(Vector3 other) => Cross(this, other);

        public static float Distance(Vector3 a, Vector3 b) => (a - b).Length();
        public float Distance(Vector3 other) => Distance(this, other);

        public static implicit operator Vector3D(Vector3 v) => new(v.X, v.Y, v.Z);

        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3 operator *(Vector3 a, float s) => new(a.X * s, a.Y * s, a.Z * s);
        public static Vector3 operator *(float s, Vector3 a) => new(a.X * s, a.Y * s, a.Z * s);
        public static Vector3 operator /(Vector3 a, float s) => new(a.X / s, a.Y / s, a.Z / s);
        public static Vector3 operator -(Vector3 a) => new(-a.X, -a.Y, -a.Z);

        public static bool operator ==(Vector3 a, Vector3 b) =>
            Math.Abs(a.X - b.X) < float.Epsilon &&
            Math.Abs(a.Y - b.Y) < float.Epsilon &&
            Math.Abs(a.Z - b.Z) < float.Epsilon;

        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);

        public override string ToString() => $"({X}, {Y}, {Z})";
        public override bool Equals(object obj) => obj is Vector3 other && this == other;
        public bool Equals(Vector3 other) => this == other;
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + X.GetHashCode();
                hash = hash * 31 + Y.GetHashCode();
                hash = hash * 31 + Z.GetHashCode();
                return hash;
            }
        }
    }
}
