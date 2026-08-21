using System;
using System.Runtime.CompilerServices;

namespace FloatingOffset.Runtime
{
    [System.Serializable]
    public readonly struct Vector3d : IEquatable<Vector3d>
    {
        public readonly double x;
        public readonly double y;
        public readonly double z;
        public Vector3d(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3d(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public static Vector3d zero => new Vector3d(0d, 0d, 0d);
        public static Vector3d one => new Vector3d(1d, 1d, 1d);
        public static Vector3d forward => new Vector3d(0d, 0d, 1d);
        public static Vector3d up => new Vector3d(0d, 1d, 0d);
        public static Vector3d right => new Vector3d(1d, 0d, 0d);
        internal double squaredMagnitude => SquaredMagnitude(this);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator +(in Vector3d a, in Vector3d b) => new Vector3d(a.x + b.x, a.y + b.y, a.z + b.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator -(in Vector3d a, in Vector3d b) => new Vector3d(a.x - b.x, a.y - b.y, a.z - b.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator -(in Vector3d a) => new Vector3d(-a.x, -a.y, -a.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator *(in Vector3d a, double d) => new Vector3d(a.x * d, a.y * d, a.z * d);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator *(double d, in Vector3d a) => a * d;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d operator /(in Vector3d a, double d) => new Vector3d(a.x / d, a.y / d, a.z / d);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Vector3d lhs, in Vector3d rhs) => lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Vector3d lhs, in Vector3d rhs) => !(lhs == rhs);
        public override int GetHashCode() => HashCode.Combine(x, y, z);
        public override bool Equals(object obj) => obj is Vector3d other && Equals(other);
        public bool Equals(Vector3d other) => x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z);
        public static double SquaredMagnitude(Vector3d v) => v.x * v.x + v.y * v.y + v.z * v.z;
        public static double Distance(Vector3d a, Vector3d b) => Magnitude(b - a);
        public static double Magnitude(Vector3d v) => Math.Sqrt(Vector3d.SquaredMagnitude(v));
        public override string ToString() => $"({Math.Round(x)}, {Math.Round(y)}, {Math.Round(z)})";
    }
}
