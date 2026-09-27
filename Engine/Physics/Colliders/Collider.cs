using Mirage.Common;
using Mirage.Physics.Resources;

namespace Mirage.Physics.Colliders;

/// <summary>
/// Initializes a new instance of the <see cref="Collider"/> class.
/// </summary>
/// <remarks>
/// Represents reusable collision geometry with physical surface properties.
/// </remarks>
/// <param name="material">
/// The physical material applied to the collider.
/// </param>
public abstract class Collider(PhysicsMaterial material) : Resource
{
    /// <summary>
    /// Gets the physical material applied to this collider.
    /// </summary>
    public PhysicsMaterial Material { get; } = material;
}
