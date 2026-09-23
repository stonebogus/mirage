namespace Mirage.Graphics.Vertices;

/// <summary>
/// Describes the memory layout of vertices in a buffer.
/// </summary>
public sealed class VertexLayout
{
    /// <summary>
    /// Initializes a new vertex layout.
    /// </summary>
    /// <param name="stride">
    /// The size of one vertex, in bytes.
    /// </param>
    /// <param name="attributes">
    /// The attributes contained in each vertex.
    /// </param>
    public VertexLayout(uint stride, IEnumerable<VertexAttribute> attributes)
    {
        if (stride == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stride),
                "Vertex stride must be greater than zero."
            );
        }

        ArgumentNullException.ThrowIfNull(attributes);

        Stride = stride;
        Attributes = [.. attributes];
    }

    /// <summary>
    /// Gets the attributes contained in each vertex.
    /// </summary>
    public IReadOnlyList<VertexAttribute> Attributes { get; }

    /// <summary>
    /// Gets the size of one vertex, in bytes.
    /// </summary>
    public uint Stride { get; }
}
