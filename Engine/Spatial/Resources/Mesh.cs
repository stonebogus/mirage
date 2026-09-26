using System.Numerics;
using Mirage.Common;

namespace Mirage.Spatial.Resources;

/// <summary>
/// Represents reusable two-dimensional triangle geometry.
/// </summary>
/// <remarks>
/// The mesh copies its vertices and indices. Vertex positions are local coordinates;
/// the caller determines how to place the mesh.
/// </remarks>
public sealed class Mesh : Resource
{
    private ReadOnlyMemory<int> _indices;
    private ReadOnlyMemory<Vector2> _vertices;

    /// <summary>
    /// Initializes a mesh from vertex positions and optional triangle indices.
    /// </summary>
    /// <param name="vertices">The local vertex positions to copy.</param>
    /// <param name="indices">
    /// The triangle indices to copy, or <see langword="null"/> to use the
    /// vertices sequentially in groups of three.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="vertices"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when there are no complete triangles, a vertex is not finite,
    /// or an index refers to a vertex outside the mesh.
    /// </exception>
    public Mesh(IEnumerable<Vector2> vertices, IEnumerable<int>? indices = null)
    {
        ArgumentNullException.ThrowIfNull(vertices);

        var vertexArray = vertices.ToArray();
        var indexArray = indices?.ToArray() ?? Enumerable.Range(0, vertexArray.Length).ToArray();

        if (indexArray.Length == 0 || indexArray.Length % 3 != 0)
        {
            throw new ArgumentException(
                "A mesh must contain at least one complete triangle.",
                indices is null ? nameof(vertices) : nameof(indices)
            );
        }

        for (var index = 0; index < vertexArray.Length; index++)
        {
            var position = vertexArray[index];

            if (!float.IsFinite(position.X) || !float.IsFinite(position.Y))
            {
                throw new ArgumentException(
                    $"Vertex {index} contains nonfinite coordinates.",
                    nameof(vertices)
                );
            }
        }

        for (var index = 0; index < indexArray.Length; index++)
        {
            if ((uint)indexArray[index] >= (uint)vertexArray.Length)
            {
                throw new ArgumentException(
                    $"Index {index} refers to a vertex outside the mesh.",
                    nameof(indices)
                );
            }
        }

        _vertices = vertexArray;
        _indices = indexArray;
    }

    /// <summary>
    /// Gets the vertex indices ordered in groups of three.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the mesh has been destroyed.
    /// </exception>
    public ReadOnlyMemory<int> Indices
    {
        get
        {
            ThrowIfDestroyed();
            return _indices;
        }
    }

    /// <summary>
    /// Gets the number of triangles in the mesh.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the mesh has been destroyed.
    /// </exception>
    public int TriangleCount
    {
        get
        {
            ThrowIfDestroyed();
            return _indices.Length / 3;
        }
    }

    /// <summary>
    /// Gets the vertex positions in local coordinates.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the mesh has been destroyed.
    /// </exception>
    public ReadOnlyMemory<Vector2> Vertices
    {
        get
        {
            ThrowIfDestroyed();
            return _vertices;
        }
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        _vertices = ReadOnlyMemory<Vector2>.Empty;
        _indices = ReadOnlyMemory<int>.Empty;
    }
}
