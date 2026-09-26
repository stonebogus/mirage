using System.Numerics;
using Mirage.Physics.Resources;
using Mirage.Spatial.Resources;

namespace Mirage.Physics.Colliders;

/// <summary>
/// Represents reusable two-dimensional collision geometry backed by a mesh.
/// </summary>
/// <remarks>
/// The collider owns its spatial mesh and exposes its vertices and indices
/// through that geometry. The physical material is borrowed and is not
/// destroyed with the collider.
/// </remarks>
public class MeshCollider : Collider
{
    /// <summary>
    /// Initializes a mesh collider from vertex positions, a physical material,
    /// and optional triangle indices.
    /// </summary>
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
    public MeshCollider(
        IEnumerable<Vector2> vertices,
        PhysicsMaterial material,
        IEnumerable<int>? indices = null
    )
        : base(material)
    {
        Mesh = new Mesh(vertices, indices);
    }

    /// <summary>
    /// Gets the spatial geometry used by this collider.
    /// </summary>
    public Mesh Mesh { get; }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        Mesh.Destroy();

        base.OnDestroy();
    }
}
