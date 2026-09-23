using Mirage.Graphics.Pipelines;
using Mirage.Graphics.Resources;
using Mirage.Graphics.Shaders;

namespace Mirage.Graphics.Commands;

/// <summary>
/// Describes a non-indexed drawing operation.
/// </summary>
public sealed record DrawCommand : RenderCommand
{
    /// <summary>
    /// Gets the first instance identifier supplied to the vertex shader.
    /// </summary>
    public uint FirstInstance { get; init; }

    /// <summary>
    /// Gets the first vertex in the bound vertex buffer.
    /// </summary>
    public uint FirstVertex { get; init; }

    /// <summary>
    /// Gets the number of instances to draw.
    /// </summary>
    public uint InstanceCount { get; init; } = 1;

    /// <summary>
    /// Gets the graphics pipeline used by the operation.
    /// </summary>
    public required GraphicsPipeline Pipeline { get; init; }

    /// <summary>
    /// Gets the texture and sampler bindings used by the operation.
    /// </summary>
    public IReadOnlyList<TextureBinding> Textures { get; init; } = [];

    /// <summary>
    /// Gets the uniform values supplied to programmable shader stages.
    /// </summary>
    public IReadOnlyList<ShaderUniform> Uniforms { get; init; } = [];

    /// <summary>
    /// Gets the buffer containing the vertices to draw.
    /// </summary>
    public required GraphicsBuffer VertexBuffer { get; init; }

    /// <summary>
    /// Gets the number of vertices to draw.
    /// </summary>
    public required uint VertexCount { get; init; }
}
