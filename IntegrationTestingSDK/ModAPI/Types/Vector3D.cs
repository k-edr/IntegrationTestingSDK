using System;

namespace IntegrationTestingSDK.ModAPI.Types
{
    /// <summary>
    ///     Double-precision 3D vector matching VRageMath.Vector3D.
    /// </summary>
    public struct Vector3D : IEquatable<Vector3D>
    {
        public double X, Y, Z;

        public Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3D(double value)
        {
            X = value;
            Y = value;
            Z = value;
        }

        public static Vector3D Zero => new(0);
        public static Vector3D One => new(1);
        public static Vector3D UnitX => new(1, 0, 0);
        public static Vector3D UnitY => new(0, 1, 0);
        public static Vector3D UnitZ => new(0, 0, 1);

        public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);
        public double LengthSquared() => X * X + Y * Y + Z * Z;

        public Vector3D Normalized()
        {
            var len = Length();
            if (len < double.Epsilon)
                return Zero;
            return new Vector3D(X / len, Y / len, Z / len);
        }

        public static double Dot(Vector3D a, Vector3D b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public double Dot(Vector3D other) => Dot(this, other);

        public static Vector3D Cross(Vector3D a, Vector3D b) => new(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);

        public Vector3D Cross(Vector3D other) => Cross(this, other);

        public static double Distance(Vector3D a, Vector3D b) => (a - b).Length();
        public double Distance(Vector3D other) => Distance(this, other);

        public static implicit operator Vector3D(Vector3 v) => new(v.X, v.Y, v.Z);

        public static Vector3D operator +(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3D operator -(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3D operator *(Vector3D a, double s) => new(a.X * s, a.Y * s, a.Z * s);
        public static Vector3D operator *(double s, Vector3D a) => new(a.X * s, a.Y * s, a.Z * s);
        public static Vector3D operator /(Vector3D a, double s) => new(a.X / s, a.Y / s, a.Z / s);
        public static Vector3D operator -(Vector3D a) => new(-a.X, -a.Y, -a.Z);

        public static bool operator ==(Vector3D a, Vector3D b) =>
            Math.Abs(a.X - b.X) < double.Epsilon &&
            Math.Abs(a.Y - b.Y) < double.Epsilon &&
            Math.Abs(a.Z - b.Z) < double.Epsilon;

        public static bool operator !=(Vector3D a, Vector3D b) => !(a == b);

        public override string ToString() => $"({X}, {Y}, {Z})";
        public override bool Equals(object obj) => obj is Vector3D other && this == other;
        public bool Equals(Vector3D other) => this == other;
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
