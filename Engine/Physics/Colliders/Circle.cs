using Mirage.Physics.Resources;

namespace Mirage.Physics.Colliders;

/// <summary>
/// Represents a reusable circular two-dimensional collider.
/// </summary>
/// <remarks>
/// A circle collider is represented by its radius and is treated as native
/// circular collision geometry by the simulation system.
/// </remarks>
public class CircleCollider : Collider
{
    /// <summary>
    /// Initializes a circular collider.
    /// </summary>
    /// <param name="radius">
    /// The radius of the circle in local units.
    /// </param>
    /// <param name="material">
    /// The physical material applied to the collider.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="radius"/> is not finite or is less than
    /// or equal to zero.
    /// </exception>
    public CircleCollider(float radius, PhysicsMaterial material)
        : base(material)
    {
        if (!float.IsFinite(radius) || radius <= 0f)
            throw new ArgumentOutOfRangeException(nameof(radius));

        Radius = radius;
    }

    /// <summary>
    /// Gets the radius of the circle in local units.
    /// </summary>
    public float Radius { get; }
}
