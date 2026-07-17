using System;

namespace IntegrationTestingSDK.ModAPI.Types
{
    /// <summary>
    ///     Integer 3D vector matching VRageMath.Vector3I.
    /// </summary>
    public struct Vector3I : IEquatable<Vector3I>
    {
        public int X, Y, Z;

        public Vector3I(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3I Zero => new(0, 0, 0);
        public static Vector3I One => new(1, 1, 1);
        public static Vector3I UnitX => new(1, 0, 0);
        public static Vector3I UnitY => new(0, 1, 0);
        public static Vector3I UnitZ => new(0, 0, 1);

        public static Vector3I operator +(Vector3I a, Vector3I b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3I operator -(Vector3I a, Vector3I b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3I operator *(Vector3I a, int s) => new(a.X * s, a.Y * s, a.Z * s);
        public static Vector3I operator *(int s, Vector3I a) => new(a.X * s, a.Y * s, a.Z * s);
        public static Vector3I operator -(Vector3I a) => new(-a.X, -a.Y, -a.Z);

        public static bool operator ==(Vector3I a, Vector3I b) =>
            a.X == b.X && a.Y == b.Y && a.Z == b.Z;

        public static bool operator !=(Vector3I a, Vector3I b) => !(a == b);

        public override string ToString() => $"({X}, {Y}, {Z})";
        public override bool Equals(object obj) => obj is Vector3I other && this == other;
        public bool Equals(Vector3I other) => this == other;
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + X;
                hash = hash * 31 + Y;
                hash = hash * 31 + Z;
                return hash;
            }
        }
    }
}
