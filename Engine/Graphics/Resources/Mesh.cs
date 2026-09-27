using System.Numerics;
using Mirage.Graphics.Primitives;
using Mirage.Spatial.Resources;

namespace Mirage.Graphics.Resources;

/// <summary>
/// Represents reusable two-dimensional triangle geometry with texture mapping.
/// </summary>
/// <remarks>
/// A graphic mesh extends spatial triangle geometry with a texture and one
/// normalized texture coordinate for each vertex.
///
/// The mesh copies its vertices, indices, and texture coordinates. It borrows
/// the texture and does not destroy it.
/// </remarks>
public class GraphicMesh : Mesh
{
    private ReadOnlyMemory<Vector2> _textureCoordinates;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicMesh"/> class.
    /// </summary>
    /// <param name="vertices">The local vertex positions to copy.</param>
    /// <param name="texture">The texture mapped onto the mesh.</param>
    /// <param name="textureCoordinates">
    /// One normalized texture coordinate for each vertex, in the same order.
    /// </param>
    /// <param name="indices">
    /// The triangle indices to copy, or <see langword="null"/> to use the
    /// vertices sequentially in groups of three.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the texture coordinate count differs from the vertex count
    /// or a texture coordinate contains a nonfinite value.
    /// </exception>
    public GraphicMesh(
        IEnumerable<Vector2> vertices,
        Texture texture,
        IEnumerable<Vector2> textureCoordinates,
        IEnumerable<int>? indices = null
    )
        : base(vertices, indices)
    {
        var coordinates = textureCoordinates.ToArray();

        if (coordinates.Length != Vertices.Length)
        {
            throw new ArgumentException(
                "The number of texture coordinates must match the number of mesh vertices.",
                nameof(textureCoordinates)
            );
        }

        for (var index = 0; index < coordinates.Length; index++)
        {
            var coordinate = coordinates[index];

            if (!float.IsFinite(coordinate.X) || !float.IsFinite(coordinate.Y))
            {
                throw new ArgumentException(
                    $"Texture coordinate {index} contains nonfinite values.",
                    nameof(textureCoordinates)
                );
            }
        }

        Texture = texture;
        _textureCoordinates = coordinates;
    }

    /// <summary>
    /// Gets the normalized texture coordinates, ordered like the mesh vertices.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when this mesh has been destroyed.
    /// </exception>
    public ReadOnlyMemory<Vector2> TexCoords
    {
        get
        {
            ThrowIfDestroyed();
            return _textureCoordinates;
        }
    }

    /// <summary>
    /// Gets the texture mapped onto the mesh.
    /// </summary>
    public Texture Texture { get; }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        _textureCoordinates = ReadOnlyMemory<Vector2>.Empty;

        base.OnDestroy();
    }
}
