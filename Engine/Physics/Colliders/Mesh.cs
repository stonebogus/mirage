using System.Numerics;
using Mirage.Physics.Resources;
using Mirage.Spatial.Resources;

namespace Mirage.Physics.Colliders;

/// <summary>
/// Initializes a new instance of the <see cref="MeshCollider"/> class.
/// </summary>
/// <remarks>
/// <para>
/// Represents reusable two-dimensional collision geometry backed by a mesh.
/// </para>
/// The collider owns its spatial mesh and exposes its vertices and indices
/// through that geometry. The physical material is borrowed and is not
/// destroyed with the collider.
/// </remarks>
/// <param name="vertices">
/// The local vertex positions used to construct the collision mesh.
/// </param>
/// <param name="material">
/// The physical material applied to the collider.
/// </param>
/// <param name="indices">
/// The triangle indices, or <see langword="null"/> to use the vertices
/// sequentially in groups of three.
/// </param>
public class MeshCollider(
    IEnumerable<Vector2> vertices,
    PhysicsMaterial material,
    IEnumerable<int>? indices = null
) : Collider(material)
{
    /// <summary>
    /// Gets the spatial geometry used by this collider.
    /// </summary>
    public Mesh Mesh { get; } = new(vertices, indices);

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        Mesh.Destroy();

        base.OnDestroy();
    }
}
