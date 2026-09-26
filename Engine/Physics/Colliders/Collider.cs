using Mirage.Common;
using Mirage.Physics.Resources;

namespace Mirage.Physics.Colliders;

/// <summary>
/// Represents reusable collision geometry with physical surface properties.
/// </summary>
public abstract class Collider : Resource
{
    /// <summary>
    /// Initializes a collider with a physical material.
    /// </summary>
    /// <param name="material">
    /// The physical material applied to the collider.
    /// </param>
    protected Collider(PhysicsMaterial material)
    {
        Material = material;
    }

    /// <summary>
    /// Gets the physical material applied to this collider.
    /// </summary>
    public PhysicsMaterial Material { get; }
}
