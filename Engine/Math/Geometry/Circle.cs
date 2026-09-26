using System.Numerics;

namespace Mirage.Math.Geometry;

/// <summary>
/// Represents a circle in two-dimensional space.
/// </summary>
public readonly struct Circle : IEquatable<Circle>
{
    /// <summary>
    /// Gets the center point in application-defined coordinate units.
    /// </summary>
    public readonly Vector2 Center;

    /// <summary>
    /// Gets the radius in coordinate units.
    /// </summary>
    public readonly float Radius;

    /// <summary>
    /// Creates a circle from its center and radius.
    /// </summary>
    /// <param name="center">The center point in the circle's coordinate space.</param>
    /// <param name="radius">The radius in coordinate units.</param>
    public Circle(Vector2 center, float radius)
    {
        Center = center;
        Radius = radius;
    }

    /// <summary>
    /// Gets the diameter in coordinate units.
    /// </summary>
    public float Diameter => Radius * 2;

    /// <summary>
    /// Gets the circumference in coordinate units.
    /// </summary>
    public float Circumference => 2 * System.MathF.PI * Radius;

    /// <summary>
    /// Gets the area in squared coordinate units.
    /// </summary>
    public float Area => System.MathF.PI * Radius * Radius;

    /// <summary>
    /// Determines whether a point is inside or on the circle.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <returns><see langword="true"/> when the point lies inside or on the circumference.</returns>
    public bool Contains(Vector2 point) =>
        Vector2.DistanceSquared(Center, point) <= Radius * Radius;

    /// <inheritdoc />
    public bool Equals(Circle other) => Center == other.Center && Radius == other.Radius;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Circle other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Center, Radius);

    /// <summary>
    /// Returns a string representation of the circle.
    /// </summary>
    public override string ToString() => $"Circle({Center}, {Radius})";

    /// <summary>
    /// Determines whether two circles are equal.
    /// </summary>
    public static bool operator ==(Circle left, Circle right) => left.Equals(right);

    /// <summary>
    /// Determines whether two circles are not equal.
    /// </summary>
    public static bool operator !=(Circle left, Circle right) => !left.Equals(right);
}
