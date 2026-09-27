using System.Numerics;

namespace Mirage.Math.Geometry;

/// <summary>
/// Initializes a new instance of the <see cref="Rect"/> struct.
/// </summary>
/// <remarks>
/// Represents an axis-aligned rectangle in two-dimensional space.
/// </remarks>
/// <param name="minimum">The minimum point in the rectangle's coordinate space.</param>
/// <param name="maximum">The maximum point in the rectangle's coordinate space.</param>
public readonly struct Rect(Vector2 minimum, Vector2 maximum) : IEquatable<Rect>
{
    /// <summary>
    /// Gets the minimum point of the rectangle in application-defined coordinate units.
    /// </summary>
    public readonly Vector2 Minimum = minimum;

    /// <summary>
    /// Gets the maximum point of the rectangle in application-defined coordinate units.
    /// </summary>
    public readonly Vector2 Maximum = maximum;

    /// <summary>
    /// Gets the width in coordinate units.
    /// </summary>
    public float Width => Maximum.X - Minimum.X;

    /// <summary>
    /// Gets the height in coordinate units.
    /// </summary>
    public float Height => Maximum.Y - Minimum.Y;

    /// <summary>
    /// Gets the center of the rectangle.
    /// </summary>
    public Vector2 Center => (Minimum + Maximum) / 2;

    /// <summary>
    /// Gets the area in squared coordinate units.
    /// </summary>
    public float Area => Width * Height;

    /// <summary>
    /// Determines whether a point is inside or on the rectangle.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <returns><see langword="true"/> when the point lies inside or on the boundary.</returns>
    public bool Contains(Vector2 point) =>
        point.X >= Minimum.X
        && point.X <= Maximum.X
        && point.Y >= Minimum.Y
        && point.Y <= Maximum.Y;

    /// <summary>
    /// Determines whether this rectangle intersects another rectangle.
    /// </summary>
    /// <param name="other">The rectangle to test against.</param>
    /// <returns><see langword="true"/> when the rectangles overlap or touch.</returns>
    public bool Intersects(Rect other) =>
        Minimum.X <= other.Maximum.X
        && Maximum.X >= other.Minimum.X
        && Minimum.Y <= other.Maximum.Y
        && Maximum.Y >= other.Minimum.Y;

    /// <inheritdoc />
    public bool Equals(Rect other) => Minimum == other.Minimum && Maximum == other.Maximum;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Rect other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Minimum, Maximum);

    /// <summary>
    /// Returns a string representation of the rectangle.
    /// </summary>
    public override string ToString() => $"Rect({Minimum}, {Maximum})";

    /// <summary>
    /// Determines whether two rectangles are equal.
    /// </summary>
    public static bool operator ==(Rect left, Rect right) => left.Equals(right);

    /// <summary>
    /// Determines whether two rectangles are not equal.
    /// </summary>
    public static bool operator !=(Rect left, Rect right) => !left.Equals(right);
}
