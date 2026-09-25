using System.Numerics;
using System.Runtime.InteropServices;
using Mirage.Graphics;
using Mirage.Graphics.Commands;
using Mirage.Graphics.Interfaces;
using Mirage.Graphics.Pipelines;
using Mirage.Graphics.Resources;
using Mirage.Graphics.Shaders;
using Mirage.Graphics.Vertices;
using Mirage.Spatial;

namespace Mirage.Sandbox;

internal sealed class TexturedQuad : SpatialNode, IRenderable
{
    private readonly GraphicsPipeline _pipeline;
    private readonly GraphicsSampler _sampler;
    private readonly GraphicsTexture _texture;
    private readonly GraphicsBuffer _vertexBuffer;

    public TexturedQuad()
        : base("TexturedQuad")
    {
        Vertex[] vertices =
        [
            new(new Vector2(-0.5f, 0.5f), new Vector2(0.0f, 0.0f)),
            new(new Vector2(0.5f, 0.5f), new Vector2(1.0f, 0.0f)),
            new(new Vector2(0.5f, -0.5f), new Vector2(1.0f, 1.0f)),
            new(new Vector2(-0.5f, 0.5f), new Vector2(0.0f, 0.0f)),
            new(new Vector2(0.5f, -0.5f), new Vector2(1.0f, 1.0f)),
            new(new Vector2(-0.5f, -0.5f), new Vector2(0.0f, 1.0f)),
        ];

        _vertexBuffer = new GraphicsBuffer(
            MemoryMarshal.AsBytes(vertices.AsSpan()).ToArray(),
            BufferUsage.Vertex
        );

        // Textura 2x2: rojo, verde, azul y transparente.
        byte[] pixels = [255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 255, 80];

        _texture = new GraphicsTexture(2, 2, pixels);

        _sampler = new GraphicsSampler(
            minFilter: TextureFilter.Nearest,
            magFilter: TextureFilter.Nearest
        );

        var layout = new VertexLayout(
            (uint)Marshal.SizeOf<Vertex>(),
            [
                new VertexAttribute(
                    0,
                    VertexFormat.Vector2,
                    (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.Position))
                ),
                new VertexAttribute(
                    1,
                    VertexFormat.Vector2,
                    (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.TextureCoordinate))
                ),
            ]
        );

        _pipeline = new GraphicsPipeline(
            new Shader(LoadShader("Sprite.vert.hlsl"), ShaderStage.Vertex),
            new Shader(LoadShader("Sprite.frag.hlsl"), ShaderStage.Fragment),
            layout,
            blendState: BlendState.Alpha
        );
    }

    public RenderData Render(RenderContext context)
    {
        var position = Position.Get();
        var scale = Scale.Get();

        var transform = new TransformUniform(
            position.X,
            position.Y,
            (float)Rotation.Get(),
            scale.X,
            scale.Y
        );

        var uniformData = MemoryMarshal
            .AsBytes(MemoryMarshal.CreateReadOnlySpan(ref transform, 1))
            .ToArray();

        var data = new RenderData();

        data.Add(
            new DrawCommand
            {
                Pipeline = _pipeline,
                VertexBuffer = _vertexBuffer,
                VertexCount = 6,

                Textures = [new TextureBinding(0, ShaderStage.Fragment, _texture, _sampler)],

                Uniforms = [new ShaderUniform(0, ShaderStage.Vertex, uniformData)],
            }
        );

        return data;
    }

    private static string LoadShader(string filename)
    {
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", filename);

        return File.ReadAllText(path);
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct TransformUniform(
        float positionX,
        float positionY,
        float rotation,
        float scaleX,
        float scaleY
    )
    {
        public readonly float PositionX = positionX;
        public readonly float PositionY = positionY;

        public readonly float Rotation = rotation;
        private readonly float _padding0;

        public readonly float ScaleX = scaleX;
        public readonly float ScaleY = scaleY;

        private readonly float _padding1;
        private readonly float _padding2;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Vertex(Vector2 position, Vector2 textureCoordinate)
    {
        public readonly Vector2 Position = position;
        public readonly Vector2 TextureCoordinate = textureCoordinate;
    }
}
