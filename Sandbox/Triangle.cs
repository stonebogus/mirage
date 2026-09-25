using System.Numerics;
using System.Runtime.InteropServices;
using Mirage.Graphics;
using Mirage.Graphics.Commands;
using Mirage.Graphics.Interfaces;
using Mirage.Graphics.Pipelines;
using Mirage.Graphics.Resources;
using Mirage.Graphics.Shaders;
using Mirage.Graphics.Vertices;
using Mirage.Scheduling.Interfaces;
using Mirage.Spatial;

namespace Mirage.Sandbox;

internal sealed class Triangle : SpatialNode, IRenderable, IUpdatable
{
    private readonly GraphicsPipeline _pipeline;
    private readonly GraphicsBuffer _vertexBuffer;
    private readonly uint _vertexCount;

    public Triangle()
        : base("Triangle")
    {
        Vertex[] vertices =
        [
            new(new Vector2(0.0f, 0.5f), 1.0f, 0.2f, 0.3f),
            new(new Vector2(0.5f, -0.5f), 0.2f, 1.0f, 0.3f),
            new(new Vector2(-0.5f, -0.5f), 0.2f, 0.3f, 1.0f),
        ];

        var bytes = MemoryMarshal.AsBytes(vertices.AsSpan()).ToArray();

        _vertexBuffer = new GraphicsBuffer(bytes, BufferUsage.Vertex);
        _vertexCount = (uint)vertices.Length;

        var vertexShader = new Shader(LoadShader("Triangle.vert.hlsl"), ShaderStage.Vertex);

        var fragmentShader = new Shader(LoadShader("Triangle.frag.hlsl"), ShaderStage.Fragment);

        var vertexLayout = new VertexLayout(
            stride: (uint)Marshal.SizeOf<Vertex>(),
            attributes:
            [
                new VertexAttribute(
                    0,
                    VertexFormat.Vector2,
                    (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.Position))
                ),
                new VertexAttribute(
                    1,
                    VertexFormat.Vector3,
                    (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.Red))
                ),
            ]
        );

        _pipeline = new GraphicsPipeline(
            vertexShader,
            fragmentShader,
            vertexLayout,
            PrimitiveTopology.TriangleList
        );
    }

    public RenderData Render(RenderContext context)
    {
        var position = Position.Get();
        var scale = Scale.Get();

        var transform = new TransformUniform(
            position.X,
            position.Y,
            Rotation.Get(),
            scale.X,
            scale.Y
        );

        var uniformBytes = MemoryMarshal
            .AsBytes(MemoryMarshal.CreateReadOnlySpan(ref transform, 1))
            .ToArray();

        var command = new DrawCommand
        {
            Pipeline = _pipeline,
            VertexBuffer = _vertexBuffer,
            VertexCount = _vertexCount,
            Uniforms = [new ShaderUniform(Slot: 0, Stage: ShaderStage.Vertex, Data: uniformBytes)],
        };

        var data = new RenderData();
        data.Add(command);

        return data;
    }

    private static string LoadShader(string filename)
    {
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", filename);

        return File.ReadAllText(path);
    }

    public void Update(double deltaTime)
    {
        Rotation.Set(Rotation.Get() + (float)deltaTime);
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
    private readonly struct Vertex(Vector2 position, float red, float green, float blue)
    {
        public readonly Vector2 Position = position;
        public readonly float Red = red;
        public readonly float Green = green;
        public readonly float Blue = blue;
    }
}
