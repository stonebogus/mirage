using Mirage.Common;

namespace Mirage.Graphics.Resources;

/// <summary>
/// Specifies how a graphics buffer is used.
/// </summary>
[Flags]
public enum BufferUsage
{
    /// <summary>
    /// The buffer has no specified usage.
    /// </summary>
    None = 0,

    /// <summary>
    /// The buffer contains vertex data.
    /// </summary>
    Vertex = 1 << 0,

    /// <summary>
    /// The buffer contains index data.
    /// </summary>
    Index = 1 << 1,

    /// <summary>
    /// The buffer contains uniform data.
    /// </summary>
    Uniform = 1 << 2,

    /// <summary>
    /// The buffer contains storage data.
    /// </summary>
    Storage = 1 << 3,
}

/// <summary>
/// Represents backend-agnostic graphics buffer data.
/// </summary>
public sealed class GraphicsBuffer : Resource
{
    /// <summary>
    /// Initializes a new graphics buffer.
    /// </summary>
    /// <param name="data">
    /// The initial contents of the buffer.
    /// </param>
    /// <param name="usage">
    /// How the buffer will be used.
    /// </param>
    public GraphicsBuffer(ReadOnlyMemory<byte> data, BufferUsage usage)
    {
        if (usage == BufferUsage.None)
        {
            throw new ArgumentException(
                "A graphics buffer must have at least one usage.",
                nameof(usage)
            );
        }

        Data = data;
        Usage = usage;
    }

    /// <summary>
    /// Gets the contents of the buffer.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>
    /// Gets how the buffer is used.
    /// </summary>
    public BufferUsage Usage { get; }
}
