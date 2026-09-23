namespace Mirage.Math.Vectors;

/// <summary>
/// Represents a three-dimensional vector with X, Y, and Z components.
/// </summary>
public readonly struct Vector3D(float x = 0, float y = 0, float z = 0) : IVector<Vector3D>
{
    /// <summary>
    /// Gets the X component of the vector.
    /// </summary>
    public readonly float X = x;

    /// <summary>
    /// Gets the Y component of the vector.
    /// </summary>
    public readonly float Y = y;

    /// <summary>
    /// Gets the Z component of the vector.
    /// </summary>
    public readonly float Z = z;

    /// <inheritdoc />
    public float Length => System.MathF.Sqrt(LengthSquared);

    /// <inheritdoc />
    public float LengthSquared => X * X + Y * Y + Z * Z;

    /// <inheritdoc />
    public Vector3D Normalized => this / Length;

    /// <inheritdoc />
    public static Vector3D operator +(Vector3D left, Vector3D right) =>
        new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    /// <inheritdoc />
    public static Vector3D operator -(Vector3D left, Vector3D right) =>
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    /// <inheritdoc />
    public static Vector3D operator -(Vector3D value) => new(-value.X, -value.Y, -value.Z);

    /// <inheritdoc />
    public static Vector3D operator *(Vector3D value, float scalar) =>
        new(value.X * scalar, value.Y * scalar, value.Z * scalar);

    /// <summary>
    /// Multiplies a scalar by a vector.
    /// </summary>
    public static Vector3D operator *(float scalar, Vector3D value) => value * scalar;

    /// <inheritdoc />
    public static Vector3D operator /(Vector3D value, float scalar) =>
        new(value.X / scalar, value.Y / scalar, value.Z / scalar);

    /// <inheritdoc />
    public static bool operator ==(Vector3D left, Vector3D right) => left.Equals(right);

    /// <inheritdoc />
    public static bool operator !=(Vector3D left, Vector3D right) => !left.Equals(right);

    /// <inheritdoc />
    public static float Dot(Vector3D left, Vector3D right) =>
        left.X * right.X + left.Y * right.Y + left.Z * right.Z;

    /// <inheritdoc />
    public static float Distance(Vector3D left, Vector3D right) => (left - right).Length;

    /// <inheritdoc />
    public static float DistanceSquared(Vector3D left, Vector3D right) =>
        (left - right).LengthSquared;

    /// <inheritdoc />
    public static Vector3D Lerp(Vector3D start, Vector3D end, float amount) =>
        start + (end - start) * amount;

    /// <inheritdoc />
    public static Vector3D Min(Vector3D left, Vector3D right) =>
        new(
            System.MathF.Min(left.X, right.X),
            System.MathF.Min(left.Y, right.Y),
            System.MathF.Min(left.Z, right.Z)
        );

    /// <inheritdoc />
    public static Vector3D Max(Vector3D left, Vector3D right) =>
        new(
            System.MathF.Max(left.X, right.X),
            System.MathF.Max(left.Y, right.Y),
            System.MathF.Max(left.Z, right.Z)
        );

    /// <inheritdoc />
    public static Vector3D Clamp(Vector3D value, Vector3D minimum, Vector3D maximum) =>
        new(
            System.MathF.Clamp(value.X, minimum.X, maximum.X),
            System.MathF.Clamp(value.Y, minimum.Y, maximum.Y),
            System.MathF.Clamp(value.Z, minimum.Z, maximum.Z)
        );

    /// <summary>
    /// Calculates the vector cross product of two three-dimensional vectors.
    /// </summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>A vector perpendicular to <paramref name="left"/> and <paramref name="right"/>.</returns>
    public static Vector3D Cross(Vector3D left, Vector3D right) =>
        new(
            left.Y * right.Z - left.Z * right.Y,
            left.Z * right.X - left.X * right.Z,
            left.X * right.Y - left.Y * right.X
        );

    /// <inheritdoc />
    public bool Equals(Vector3D other) => X == other.X && Y == other.Y && Z == other.Z;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Vector3D other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    /// <summary>
    /// Returns a string representation of the vector.
    /// </summary>
    public override string ToString() => $"Vector3D({X}, {Y}, {Z})";
}
