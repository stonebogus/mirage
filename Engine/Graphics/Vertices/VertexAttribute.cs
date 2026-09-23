namespace Mirage.Graphics.Vertices;

/// <summary>
/// Describes one attribute within a vertex.
/// </summary>
/// <param name="location">
/// The shader input location of the attribute.
/// </param>
/// <param name="format">
/// The storage format of the attribute.
/// </param>
/// <param name="offset">
/// The byte offset of the attribute within a vertex.
/// </param>
public readonly record struct VertexAttribute(uint Location, VertexFormat Format, uint Offset);
