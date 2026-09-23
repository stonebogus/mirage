using Mirage.Common;
using Mirage.Graphics.Shaders;
using Mirage.Graphics.Vertices;

namespace Mirage.Graphics.Pipelines;

/// <summary>
/// Describes the shaders and fixed state used by a drawing operation.
/// </summary>
public sealed class GraphicsPipeline : Resource
{
    public GraphicsPipeline(
        Shader vertexShader,
        Shader fragmentShader,
        VertexLayout vertexLayout,
        PrimitiveTopology topology = PrimitiveTopology.TriangleList,
        BlendState? blendState = null
    )
    {
        ArgumentNullException.ThrowIfNull(vertexShader);
        ArgumentNullException.ThrowIfNull(fragmentShader);
        ArgumentNullException.ThrowIfNull(vertexLayout);

        if (vertexShader.Stage != ShaderStage.Vertex)
        {
            throw new ArgumentException(
                "The vertex shader must use the vertex stage.",
                nameof(vertexShader)
            );
        }

        if (fragmentShader.Stage != ShaderStage.Fragment)
        {
            throw new ArgumentException(
                "The fragment shader must use the fragment stage.",
                nameof(fragmentShader)
            );
        }

        VertexShader = vertexShader;
        FragmentShader = fragmentShader;
        VertexLayout = vertexLayout;
        Topology = topology;
        BlendState = blendState ?? Pipelines.BlendState.Opaque;
    }

    public BlendState BlendState { get; }

    public Shader FragmentShader { get; }

    public PrimitiveTopology Topology { get; }

    public VertexLayout VertexLayout { get; }

    public Shader VertexShader { get; }
}
