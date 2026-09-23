namespace Mirage.Math.Vectors;

/// <summary>
/// Represents a two-dimensional vector with X and Y components.
/// </summary>
public readonly struct Vector2D(float x = 0, float y = 0) : IVector<Vector2D>
{
    /// <summary>
    /// Gets the X component of the vector.
    /// </summary>
    public readonly float X = x;

    /// <summary>
    /// Gets the Y component of the vector.
    /// </summary>
    public readonly float Y = y;

    /// <inheritdoc />
    public float Length => System.MathF.Sqrt(LengthSquared);

    /// <inheritdoc />
    public float LengthSquared => X * X + Y * Y;

    /// <inheritdoc />
    public Vector2D Normalized => this / Length;

    /// <inheritdoc />
    public static Vector2D operator +(Vector2D left, Vector2D right) =>
        new(left.X + right.X, left.Y + right.Y);

    /// <inheritdoc />
    public static Vector2D operator -(Vector2D left, Vector2D right) =>
        new(left.X - right.X, left.Y - right.Y);

    /// <inheritdoc />
    public static Vector2D operator -(Vector2D value) => new(-value.X, -value.Y);

    /// <inheritdoc />
    public static Vector2D operator *(Vector2D value, float scalar) =>
        new(value.X * scalar, value.Y * scalar);

    /// <summary>
    /// Multiplies a scalar by a vector.
    /// </summary>
    public static Vector2D operator *(float scalar, Vector2D value) => value * scalar;

    /// <inheritdoc />
    public static Vector2D operator /(Vector2D value, float scalar) =>
        new(value.X / scalar, value.Y / scalar);

    /// <inheritdoc />
    public static bool operator ==(Vector2D left, Vector2D right) => left.Equals(right);

    /// <inheritdoc />
    public static bool operator !=(Vector2D left, Vector2D right) => !left.Equals(right);

    /// <inheritdoc />
    public static float Dot(Vector2D left, Vector2D right) => left.X * right.X + left.Y * right.Y;

    /// <inheritdoc />
    public static float Distance(Vector2D left, Vector2D right) => (left - right).Length;

    /// <inheritdoc />
    public static float DistanceSquared(Vector2D left, Vector2D right) =>
        (left - right).LengthSquared;

    /// <inheritdoc />
    public static Vector2D Lerp(Vector2D start, Vector2D end, float amount) =>
        start + (end - start) * amount;

    /// <inheritdoc />
    public static Vector2D Min(Vector2D left, Vector2D right) =>
        new(System.MathF.Min(left.X, right.X), System.MathF.Min(left.Y, right.Y));

    /// <inheritdoc />
    public static Vector2D Max(Vector2D left, Vector2D right) =>
        new(System.MathF.Max(left.X, right.X), System.MathF.Max(left.Y, right.Y));

    /// <inheritdoc />
    public static Vector2D Clamp(Vector2D value, Vector2D minimum, Vector2D maximum) =>
        new(
            System.Math.Clamp(value.X, minimum.X, maximum.X),
            System.Math.Clamp(value.Y, minimum.Y, maximum.Y)
        );

    /// <summary>
    /// Calculates the scalar cross product of two two-dimensional vectors.
    /// </summary>
    /// <remarks>
    /// The result is the Z component of the three-dimensional cross product
    /// obtained by treating both vectors as lying in the XY plane.
    /// </remarks>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The signed scalar cross product of <paramref name="left"/> and <paramref name="right"/>.</returns>
    public static float Cross(Vector2D left, Vector2D right) =>
        left.X * right.Y - left.Y * right.X;

    /// <inheritdoc />
    public bool Equals(Vector2D other) => X == other.X && Y == other.Y;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Vector2D other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y);

    /// <summary>
    /// Returns a string representation of the vector.
    /// </summary>
    public override string ToString() => $"Vector2D({X}, {Y})";
}
