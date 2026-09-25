namespace Mirage.Math.Vectors;

/// <summary>
/// Represents a two-dimensional vector with X and Y components.
/// </summary>
public readonly struct Vector(float x = 0, float y = 0) : IEquatable<Vector>
{
    /// <summary>
    /// Gets the X component of the vector.
    /// </summary>
    public readonly float X = x;

    /// <summary>
    /// Gets the Y component of the vector.
    /// </summary>
    public readonly float Y = y;

    /// <summary>Gets the vector length.</summary>
    public float Length => System.MathF.Sqrt(LengthSquared);

    /// <summary>Gets the squared vector length.</summary>
    public float LengthSquared => X * X + Y * Y;

    /// <summary>Gets a normalized copy of the vector.</summary>
    public Vector Normalized => this / Length;

    /// <summary>Adds two vectors.</summary>
    public static Vector operator +(Vector left, Vector right) =>
        new(left.X + right.X, left.Y + right.Y);

    /// <summary>Subtracts two vectors.</summary>
    public static Vector operator -(Vector left, Vector right) =>
        new(left.X - right.X, left.Y - right.Y);

    /// <summary>Negates a vector.</summary>
    public static Vector operator -(Vector value) => new(-value.X, -value.Y);

    /// <summary>Scales a vector.</summary>
    public static Vector operator *(Vector value, float scalar) =>
        new(value.X * scalar, value.Y * scalar);

    /// <summary>
    /// Multiplies a scalar by a vector.
    /// </summary>
    public static Vector operator *(float scalar, Vector value) => value * scalar;

    /// <summary>Divides a vector by a scalar.</summary>
    public static Vector operator /(Vector value, float scalar) =>
        new(value.X / scalar, value.Y / scalar);

    /// <summary>Determines whether two vectors are equal.</summary>
    public static bool operator ==(Vector left, Vector right) => left.Equals(right);

    /// <summary>Determines whether two vectors are not equal.</summary>
    public static bool operator !=(Vector left, Vector right) => !left.Equals(right);

    /// <summary>Calculates the dot product.</summary>
    public static float Dot(Vector left, Vector right) => left.X * right.X + left.Y * right.Y;

    /// <summary>Calculates the distance between two vectors.</summary>
    public static float Distance(Vector left, Vector right) => (left - right).Length;

    /// <summary>Calculates the squared distance between two vectors.</summary>
    public static float DistanceSquared(Vector left, Vector right) =>
        (left - right).LengthSquared;

    /// <summary>Linearly interpolates between two vectors.</summary>
    public static Vector Lerp(Vector start, Vector end, float amount) =>
        start + (end - start) * amount;

    /// <summary>Returns the component-wise minimum.</summary>
    public static Vector Min(Vector left, Vector right) =>
        new(System.MathF.Min(left.X, right.X), System.MathF.Min(left.Y, right.Y));

    /// <summary>Returns the component-wise maximum.</summary>
    public static Vector Max(Vector left, Vector right) =>
        new(System.MathF.Max(left.X, right.X), System.MathF.Max(left.Y, right.Y));

    /// <summary>Clamps each component to a range.</summary>
    public static Vector Clamp(Vector value, Vector minimum, Vector maximum) =>
        new(
            System.Math.Clamp(value.X, minimum.X, maximum.X),
            System.Math.Clamp(value.Y, minimum.Y, maximum.Y)
        );

    /// <summary>
    /// Calculates the scalar cross product of two two-dimensional vectors.
    /// </summary>
    /// <remarks>
    /// The result is the signed area of the parallelogram formed by the vectors.
    /// </remarks>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The signed scalar cross product of <paramref name="left"/> and <paramref name="right"/>.</returns>
    public static float Cross(Vector left, Vector right) => left.X * right.Y - left.Y * right.X;

    /// <summary>Determines whether this vector equals another vector.</summary>
    public bool Equals(Vector other) => X == other.X && Y == other.Y;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Vector other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y);

    /// <summary>
    /// Returns a string representation of the vector.
    /// </summary>
    public override string ToString() => $"Vector({X}, {Y})";
}
