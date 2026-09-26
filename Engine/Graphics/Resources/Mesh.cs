using System.Numerics;
using Mirage.Common;
using Mirage.Spatial.Resources;

namespace Mirage.Graphics.Resources;

/// <summary>
/// Associates triangle geometry with a texture and its vertex coordinates.
/// </summary>
/// <remarks>
/// Texture coordinates correspond to vertices by index and use normalized
/// coordinates. This resource copies its texture coordinates. It borrows the
/// mesh and texture; it does not destroy them.
/// </remarks>
public sealed class GraphicMesh : Resource
{
    private ReadOnlyMemory<Vector2> _texCoords;

    /// <summary>
    /// Initializes the visual data for a textured mesh.
    /// </summary>
    /// <param name="mesh">The geometry whose vertices receive texture coordinates.</param>
    /// <param name="texture">The texture to draw on the mesh.</param>
    /// <param name="textureCoordinates">
    /// One normalized texture coordinate for each vertex in
    /// <paramref name="mesh"/>, in the same order.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the mesh, texture, or texture image has been destroyed.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the coordinate count differs from the vertex count or a
    /// coordinate contains a nonfinite value.
    /// </exception>
    public GraphicMesh(Mesh mesh, Texture texture, IEnumerable<Vector2> textureCoordinates)
    {
        var coordinates = textureCoordinates.ToArray();

        if (coordinates.Length != mesh.Vertices.Length)
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

        Mesh = mesh;
        Texture = texture;
        _texCoords = coordinates;
    }

    /// <summary>
    /// Gets the geometry used by this visual.
    /// </summary>
    public Mesh Mesh { get; }

    /// <summary>
    /// Gets the normalized texture coordinates, ordered like the mesh vertices.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when this visual has been destroyed.
    /// </exception>
    public ReadOnlyMemory<Vector2> TexCoords
    {
        get
        {
            ThrowIfDestroyed();
            return _texCoords;
        }
    }

    /// <summary>
    /// Gets the texture mapped onto the geometry.
    /// </summary>
    public Texture Texture { get; }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        _texCoords = ReadOnlyMemory<Vector2>.Empty;
        base.OnDestroy();
    }
}
