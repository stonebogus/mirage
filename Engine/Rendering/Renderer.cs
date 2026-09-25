using Mirage.Common;
using Mirage.Common.Collections;
using Mirage.Common.Lifecycle;
using Mirage.Common.Events;
using Mirage.Graphics;
using Mirage.Graphics.Interfaces;
using Mirage.Graphics.Commands;
using Mirage.Graphics.Pipelines;
using Mirage.Graphics.Resources;
using Mirage.Graphics.Shaders;
using Mirage.Graphics.Vertices;
using Mirage.Graphics.Primitives;
using System.Runtime.InteropServices;
using SDL3;
using Mirage.Scheduling.Interfaces;
using Mirage.Windowing;

namespace Mirage.Rendering;

/// <summary>
/// Orders renderable entries and submits them to the SDL3 GPU renderer.
/// </summary>
public class Renderer : Module, IUpdatable
{
    private readonly List<IRenderable> _entries = [];
    private bool _composed;

    /// <summary>
    /// Gets the SDL3 GPU renderer used by this renderer.
    /// </summary>
    private readonly SdlGpuRenderer _gpu;

    /// <summary>
    /// Gets the store containing the frame clear color.
    /// </summary>
    public readonly Store<Color> ClearColor;

    /// <summary>
    /// Gets the layers managed by the renderer.
    /// </summary>
    public readonly ReactiveDictionary<string, RenderLayer> Layers = [];

    /// <summary>
    /// Initializes a new renderer.
    /// </summary>
    /// <param name="window">
    /// The SDL3 window used as the rendering target.
    /// </param>
    /// <param name="clearColor">
    /// The initial frame clear color, or <see langword="null"/> to use black.
    /// </param>
    /// <param name="layers">
    /// The initial rendering layers.
    /// </param>
    public Renderer(
        Window window,
        Color? clearColor = null,
        IEnumerable<RenderLayer>? layers = null
    )
        : base("Renderer")
    {
        ArgumentNullException.ThrowIfNull(window);

        _gpu = new SdlGpuRenderer(window);
        ClearColor = new Store<Color>(clearColor ?? Color.Black);

        foreach (var layer in layers ?? [])
            Layers.Add(layer.Identifier, layer);
    }

    /// <inheritdoc />
    public void Update(double deltaTime)
    {
        Render(deltaTime);
    }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        foreach (var layer in Compose().ToArray())
            Layers.Add(layer.Identifier, layer);

        _composed = true;
    }

    /// <summary>
    /// Composes the layers managed by this renderer.
    /// </summary>
    /// <returns>
    /// The layers to register.
    /// </returns>
    /// <remarks>
    /// Composition occurs once when the renderer starts. Layers supplied to
    /// the constructor are registered before composed layers.
    /// </remarks>
    protected virtual IEnumerable<RenderLayer> Compose()
    {
        yield break;
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        foreach (var (_, layer) in Layers.ToArray())
            layer.Destroy();

        _entries.Clear();

        Layers.Destroy();
        ClearColor.Destroy();
        _gpu.Destroy();
    }

    /// <inheritdoc />
    protected override void OnStart()
    {
        EnsureComposed();
        _gpu.Start();
    }

    /// <inheritdoc />
    protected override void OnStop()
    {
        _gpu.Stop();
    }

    /// <summary>
    /// Renders a frame using SDL3 GPU.
    /// </summary>
    /// <param name="deltaTime">
    /// The elapsed time since the previous frame, in seconds.
    /// </param>
    public void Render(double deltaTime)
    {
        var context = new RenderContext(deltaTime, ClearColor.Get());

        _entries.Clear();

        // Lower-priority layers render first. Higher-priority layers render
        // later and therefore appear above them when blending is enabled.
        foreach (var (_, layer) in Layers.OrderBy(pair => pair.Value.Priority))
        {
            _entries.AddRange(layer.Collect());
        }

        _gpu.RenderFrame(context, _entries);
    }
}
/// <summary>
/// Owns the SDL3 GPU renderer lifecycle.
/// </summary>
/// <remarks>
/// Renderable entries are evaluated and their resources are prepared before
/// the SDL3 GPU frame begins.
/// </remarks>
internal abstract class SdlGpuRendererLifecycle : Destroyable
{
    /// <summary>
    /// Gets whether the SDL3 GPU renderer is currently started.
    /// </summary>
    public bool Started { get; private set; }

    private void EnsureStarted()
    {
        if (!Started)
        {
            throw new InvalidOperationException("The SDL3 GPU renderer is not started.");
        }
    }

    /// <summary>
    /// Begins an SDL3 GPU rendering frame.
    /// </summary>
    /// <param name="context">
    /// The context describing the current frame.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when rendering can continue; otherwise,
    /// <see langword="false"/>. Rendering may return <see langword="false"/>
    /// when its target is temporarily unavailable,
    /// such as when a window is minimized.
    /// </returns>
    protected virtual bool OnBeginFrame(RenderContext context)
    {
        return true;
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        if (!Started)
            return;

        OnStop();
        Started = false;
    }

    /// <summary>
    /// Ends and presents an SDL3 GPU rendering frame.
    /// </summary>
    /// <param name="context">
    /// The context describing the current frame.
    /// </param>
    protected virtual void OnEndFrame(RenderContext context) { }

    /// <summary>
    /// Prepares the resources referenced by rendering data.
    /// </summary>
    /// <param name="context">
    /// The context describing the current frame.
    /// </param>
    /// <param name="data">
    /// The rendering data that will be executed during the frame.
    /// </param>
    /// <remarks>
    /// This method runs before <see cref="OnBeginFrame"/>. The renderer should
    /// create and upload buffers, textures, shaders and pipelines here
    /// instead of creating them while a render pass is active.
    /// </remarks>
    protected virtual void OnPrepare(RenderContext context, IReadOnlyList<RenderData> data) { }

    /// <summary>
    /// Executes rendering data.
    /// </summary>
    /// <param name="context">
    /// The context describing the current frame.
    /// </param>
    /// <param name="data">
    /// The rendering data to execute.
    /// </param>
    protected abstract void OnRender(RenderContext context, RenderData data);

    /// <summary>
    /// Starts SDL3 GPU rendering resources.
    /// </summary>
    protected virtual void OnStart() { }

    /// <summary>
    /// Stops SDL3 GPU rendering resources.
    /// </summary>
    protected virtual void OnStop() { }

    /// <summary>
    /// Renders and presents one complete frame.
    /// </summary>
    /// <param name="context">
    /// The context describing the frame.
    /// </param>
    /// <param name="entries">
    /// The renderable entries in execution order.
    /// </param>
    /// <remarks>
    /// Rendering data is collected and prepared before the SDL3 GPU frame
    /// begins. Once a frame begins, its end operation is guaranteed to
    /// run even when command execution throws an exception.
    /// </remarks>
    public void RenderFrame(RenderContext context, IReadOnlyList<IRenderable> entries)
    {
        ThrowIfDestroyed();

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(entries);

        EnsureStarted();

        var renderingData = new List<RenderData>(entries.Count);

        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);

            var data = entry.Render(context);

            if (data is null)
            {
                throw new InvalidOperationException(
                    $"Renderable '{entry.GetType().Name}' returned null rendering data."
                );
            }

            renderingData.Add(data);
        }

        OnPrepare(context, renderingData);

        if (!OnBeginFrame(context))
            return;

        try
        {
            foreach (var data in renderingData)
                OnRender(context, data);
        }
        finally
        {
            OnEndFrame(context);
        }
    }

    /// <summary>
    /// Starts the SDL3 GPU renderer.
    /// </summary>
    /// <remarks>
    /// Repeated calls have no effect while the renderer is already started.
    /// </remarks>
    public void Start()
    {
        ThrowIfDestroyed();

        if (Started)
            return;

        OnStart();
        Started = true;
    }

    /// <summary>
    /// Stops the SDL3 GPU renderer.
    /// </summary>
    /// <remarks>
    /// Repeated calls have no effect while the renderer is stopped.
    /// </remarks>
    public void Stop()
    {
        ThrowIfDestroyed();

        if (!Started)
            return;

        OnStop();
        Started = false;
    }
}

/// <summary>
/// Implements GPU rendering using the SDL3 GPU API.
/// </summary>
internal sealed class SdlGpuRenderer : SdlGpuRendererLifecycle
{
    private readonly Dictionary<GraphicsBuffer, nint> _buffers = [];
    private readonly Dictionary<GraphicsPipeline, nint> _pipelines = [];
    private readonly Dictionary<GraphicsSampler, nint> _samplers = [];
    private readonly Dictionary<Shader, nint> _shaders = [];

    private readonly Dictionary<GraphicsTexture, nint> _textures = [];
    private readonly Window _window;

    private nint _commandBuffer;
    private nint _device;
    private nint _renderPass;

    private bool _shaderCrossStarted;

    private bool _swapchainAcquired;
    private nint _swapchainTexture;

    /// <summary>
    /// Initializes the SDL3 GPU renderer.
    /// </summary>
    /// <param name="window">
    /// The SDL3 window used as the rendering target.
    /// </param>
    public SdlGpuRenderer(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        _window = window;
    }

    private void BindTextures(IReadOnlyList<TextureBinding> textures)
    {
        foreach (var binding in textures)
        {
            SDL.GPUTextureSamplerBinding[] bindings =
            [
                new()
                {
                    Texture = GetOrCreateTexture(binding.Texture),
                    Sampler = GetOrCreateSampler(binding.Sampler),
                },
            ];

            switch (binding.Stage)
            {
                case ShaderStage.Vertex:
                    SDL.BindGPUVertexSamplers(_renderPass, binding.Slot, bindings, 1);
                    break;

                case ShaderStage.Fragment:
                    SDL.BindGPUFragmentSamplers(_renderPass, binding.Slot, bindings, 1);
                    break;

                default:
                    throw new NotSupportedException(
                        $"Texture stage '{binding.Stage}' is unsupported."
                    );
            }
        }
    }

    /// <summary>
    /// Cancels the current command buffer before a swapchain texture has been
    /// acquired.
    /// </summary>
    private void CancelCurrentCommandBuffer()
    {
        var commandBuffer = _commandBuffer;

        ResetFrame();

        if (commandBuffer != nint.Zero)
            SDL.CancelGPUCommandBuffer(commandBuffer);
    }

    private static SDL.GPUSamplerAddressMode ConvertAddressMode(TextureAddressMode mode)
    {
        return mode switch
        {
            TextureAddressMode.Repeat => SDL.GPUSamplerAddressMode.Repeat,

            TextureAddressMode.MirroredRepeat => SDL.GPUSamplerAddressMode.MirroredRepeat,

            TextureAddressMode.ClampToEdge => SDL.GPUSamplerAddressMode.ClampToEdge,

            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
    }

    private static SDL.GPUBufferUsageFlags ConvertBufferUsage(BufferUsage usage)
    {
        var result = default(SDL.GPUBufferUsageFlags);

        if (usage.HasFlag(BufferUsage.Vertex))
            result |= SDL.GPUBufferUsageFlags.Vertex;

        if (usage.HasFlag(BufferUsage.Index))
            result |= SDL.GPUBufferUsageFlags.Index;

        if (usage.HasFlag(BufferUsage.Storage))
            result |= SDL.GPUBufferUsageFlags.GraphicsStorageRead;

        if (result == 0)
        {
            throw new NotSupportedException(
                $"The buffer usage '{usage}' cannot be represented by SDL GPU."
            );
        }

        return result;
    }

    private static ShaderCross.ShaderStage ConvertShaderStage(ShaderStage stage)
    {
        return stage switch
        {
            ShaderStage.Vertex => ShaderCross.ShaderStage.Vertex,
            ShaderStage.Fragment => ShaderCross.ShaderStage.Fragment,

            _ => throw new NotSupportedException(
                $"The shader stage '{stage}' is not supported by a graphics pipeline."
            ),
        };
    }

    private static SDL.GPUFilter ConvertTextureFilter(TextureFilter filter)
    {
        return filter switch
        {
            TextureFilter.Nearest => SDL.GPUFilter.Nearest,
            TextureFilter.Linear => SDL.GPUFilter.Linear,
            _ => throw new ArgumentOutOfRangeException(nameof(filter)),
        };
    }

    private static SDL.GPUTextureFormat ConvertTextureFormat(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.Rgba8 => SDL.GPUTextureFormat.R8G8B8A8Unorm,
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
    }

    private static SDL.GPUPrimitiveType ConvertTopology(PrimitiveTopology topology)
    {
        return topology switch
        {
            PrimitiveTopology.TriangleList => SDL.GPUPrimitiveType.TriangleList,
            PrimitiveTopology.TriangleStrip => SDL.GPUPrimitiveType.TriangleStrip,
            PrimitiveTopology.LineList => SDL.GPUPrimitiveType.LineList,
            PrimitiveTopology.LineStrip => SDL.GPUPrimitiveType.LineStrip,
            PrimitiveTopology.PointList => SDL.GPUPrimitiveType.PointList,

            _ => throw new ArgumentOutOfRangeException(
                nameof(topology),
                topology,
                "Unsupported primitive topology."
            ),
        };
    }

    private static SDL.GPUVertexElementFormat ConvertVertexFormat(VertexFormat format)
    {
        return format switch
        {
            VertexFormat.Float => SDL.GPUVertexElementFormat.Float,
            VertexFormat.Vector2 => SDL.GPUVertexElementFormat.Float2,
            VertexFormat.Vector3 => SDL.GPUVertexElementFormat.Float3,
            VertexFormat.Vector4 => SDL.GPUVertexElementFormat.Float4,

            _ => throw new ArgumentOutOfRangeException(
                nameof(format),
                format,
                "Unsupported vertex format."
            ),
        };
    }

    private static InvalidOperationException CreateException(string operation)
    {
        return new InvalidOperationException($"{operation}: {SDL.GetError()}");
    }

    private nint GetOrCreateBuffer(GraphicsBuffer buffer)
    {
        if (_buffers.TryGetValue(buffer, out var existing))
            return existing;

        if (buffer.Data.IsEmpty)
        {
            throw new InvalidOperationException(
                "A graphics buffer cannot be created from empty data."
            );
        }

        var size = checked((uint)buffer.Data.Length);

        var bufferInfo = new SDL.GPUBufferCreateInfo
        {
            Usage = ConvertBufferUsage(buffer.Usage),
            Size = size,
            Props = 0,
        };

        var nativeBuffer = SDL.CreateGPUBuffer(_device, in bufferInfo);

        if (nativeBuffer == nint.Zero)
            throw CreateException("Could not create an SDL GPU buffer");

        try
        {
            UploadBuffer(nativeBuffer, buffer.Data);
            _buffers.Add(buffer, nativeBuffer);

            return nativeBuffer;
        }
        catch
        {
            SDL.ReleaseGPUBuffer(_device, nativeBuffer);
            throw;
        }
    }

    private nint GetOrCreatePipeline(GraphicsPipeline pipeline)
    {
        if (_pipelines.TryGetValue(pipeline, out var existing))
            return existing;

        var vertexShader = GetOrCreateShader(pipeline.VertexShader);
        var fragmentShader = GetOrCreateShader(pipeline.FragmentShader);

        var layout = pipeline.VertexLayout;

        SDL.GPUVertexAttribute[] attributes =
        [
            .. layout.Attributes.Select(attribute => new SDL.GPUVertexAttribute
            {
                Location = attribute.Location,
                BufferSlot = 0,
                Format = ConvertVertexFormat(attribute.Format),
                Offset = attribute.Offset,
            }),
        ];

        var blendState = pipeline.BlendState.Mode switch
        {
            BlendMode.Opaque => default,

            BlendMode.Alpha => new SDL.GPUColorTargetBlendState
            {
                SrcColorBlendFactor = SDL.GPUBlendFactor.SrcAlpha,
                DstColorBlendFactor = SDL.GPUBlendFactor.OneMinusSrcAlpha,
                ColorBlendOp = SDL.GPUBlendOp.Add,

                SrcAlphaBlendFactor = SDL.GPUBlendFactor.One,
                DstAlphaBlendFactor = SDL.GPUBlendFactor.OneMinusSrcAlpha,
                AlphaBlendOp = SDL.GPUBlendOp.Add,

                EnableBlend = true,
            },

            _ => throw new ArgumentOutOfRangeException(),
        };

        SDL.GPUVertexBufferDescription[] vertexBuffers =
        [
            new()
            {
                Slot = 0,
                Pitch = layout.Stride,
                InputRate = SDL.GPUVertexInputRate.Vertex,
                InstanceStepRate = 0,
            },
        ];

        SDL.GPUColorTargetDescription[] colorTargets =
        [
            new()
            {
                Format = SDL.GetGPUSwapchainTextureFormat(_device, _window.Native),
                BlendState = blendState,
            },
        ];

        var pipelineInfo = new SDL.GPUGraphicsPipelineCreateInfo
        {
            VertexShader = vertexShader,
            FragmentShader = fragmentShader,

            PrimitiveType = ConvertTopology(pipeline.Topology),

            RasterizerState = new SDL.GPURasterizerState
            {
                FillMode = SDL.GPUFillMode.Fill,
                CullMode = SDL.GPUCullMode.None,
                FrontFace = SDL.GPUFrontFace.CounterClockwise,
                EnableDepthClip = true,
            },

            MultisampleState = new SDL.GPUMultisampleState
            {
                SampleCount = SDL.GPUSampleCount.SampleCount1,
            },

            DepthStencilState = default,

            TargetInfo = new SDL.GPUGraphicsPipelineTargetInfo { HasDepthStencilTarget = false },

            Props = 0,
        };

        var nativePipeline = SDL.CreateGPUGraphicsPipeline(
            _device,
            in pipelineInfo,
            vertexBuffers,
            attributes,
            colorTargets
        );

        if (nativePipeline == nint.Zero)
            throw CreateException("Could not create an SDL GPU graphics pipeline");

        _pipelines.Add(pipeline, nativePipeline);

        return nativePipeline;
    }

    private nint GetOrCreateSampler(GraphicsSampler sampler)
    {
        if (_samplers.TryGetValue(sampler, out var existing))
            return existing;

        var info = new SDL.GPUSamplerCreateInfo
        {
            MinFilter = ConvertTextureFilter(sampler.MinFilter),
            MagFilter = ConvertTextureFilter(sampler.MagFilter),
            MipmapMode = SDL.GPUSamplerMipmapMode.Nearest,

            AddressModeU = ConvertAddressMode(sampler.AddressModeU),
            AddressModeV = ConvertAddressMode(sampler.AddressModeV),
            AddressModeW = SDL.GPUSamplerAddressMode.ClampToEdge,

            MinLod = 0,
            MaxLod = 0,
        };

        var nativeSampler = SDL.CreateGPUSampler(_device, in info);

        if (nativeSampler == nint.Zero)
            throw CreateException("Could not create an SDL GPU sampler");

        _samplers.Add(sampler, nativeSampler);

        return nativeSampler;
    }

    private nint GetOrCreateShader(Shader shader)
    {
        if (_shaders.TryGetValue(shader, out var existing))
            return existing;

        if (shader.Language != ShaderLanguage.Hlsl)
        {
            throw new NotSupportedException(
                $"SdlGpuRenderer cannot compile the shader language '{shader.Language}'."
            );
        }

        var stage = ConvertShaderStage(shader.Stage);

        var spirv = ShaderCross.CompileSPIRVFromHLSL(
            shader.Source,
            shader.EntryPoint,
            stage,
            out var spirvSize,
            includeDir: null,
            props: 0
        );

        if (spirv == nint.Zero)
            throw CreateException("Could not compile HLSL to SPIR-V");

        var metadataPointer = nint.Zero;

        try
        {
            metadataPointer = ShaderCross.ReflectGraphicsSPIRV(spirv, spirvSize, 0);

            if (metadataPointer == nint.Zero)
                throw CreateException("Could not reflect SPIR-V shader metadata");

            var metadata = Marshal.PtrToStructure<ShaderCross.GraphicsShaderMetadata>(
                metadataPointer
            );

            var nativeShader = ShaderCross.CompileGraphicsShaderFromSPIRV(
                _device,
                spirv,
                spirvSize,
                shader.EntryPoint,
                stage,
                in metadata.ResourceInfo,
                infoProps: 0,
                shaderProps: 0
            );

            if (nativeShader == nint.Zero)
                throw CreateException("Could not create an SDL GPU shader");

            _shaders.Add(shader, nativeShader);

            return nativeShader;
        }
        finally
        {
            if (metadataPointer != nint.Zero)
                SDL.Free(metadataPointer);

            SDL.Free(spirv);
        }
    }

    private nint GetOrCreateTexture(GraphicsTexture texture)
    {
        if (_textures.TryGetValue(texture, out var existing))
            return existing;

        var info = new SDL.GPUTextureCreateInfo
        {
            Type = SDL.GPUTextureType.TextureType2D,
            Format = ConvertTextureFormat(texture.Format),
            Usage = SDL.GPUTextureUsageFlags.Sampler,

            Width = texture.Width,
            Height = texture.Height,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            SampleCount = SDL.GPUSampleCount.SampleCount1,
            Props = 0,
        };

        var nativeTexture = SDL.CreateGPUTexture(_device, in info);

        if (nativeTexture == nint.Zero)
            throw CreateException("Could not create an SDL GPU texture");

        try
        {
            UploadTexture(nativeTexture, texture);
            _textures.Add(texture, nativeTexture);

            return nativeTexture;
        }
        catch
        {
            SDL.ReleaseGPUTexture(_device, nativeTexture);
            throw;
        }
    }

    /// <summary>
    /// Creates and uploads every resource referenced by a drawing command.
    /// </summary>
    /// <param name="command">
    /// The drawing command whose resources should be prepared.
    /// </param>
    private void Prepare(DrawCommand command)
    {
        ArgumentNullException.ThrowIfNull(command.Pipeline);
        ArgumentNullException.ThrowIfNull(command.VertexBuffer);

        GetOrCreatePipeline(command.Pipeline);
        GetOrCreateBuffer(command.VertexBuffer);

        foreach (var binding in command.Textures)
        {
            ArgumentNullException.ThrowIfNull(binding.Texture);
            ArgumentNullException.ThrowIfNull(binding.Sampler);

            GetOrCreateTexture(binding.Texture);
            GetOrCreateSampler(binding.Sampler);
        }
    }

    private unsafe void PushUniforms(IReadOnlyList<ShaderUniform> uniforms)
    {
        foreach (var uniform in uniforms)
        {
            var bytes = uniform.Data.Span;

            if (bytes.IsEmpty)
                continue;

            fixed (byte* pointer = bytes)
            {
                switch (uniform.Stage)
                {
                    case ShaderStage.Vertex:
                        SDL.PushGPUVertexUniformData(
                            _commandBuffer,
                            uniform.Slot,
                            (nint)pointer,
                            (uint)bytes.Length
                        );
                        break;

                    case ShaderStage.Fragment:
                        SDL.PushGPUFragmentUniformData(
                            _commandBuffer,
                            uniform.Slot,
                            (nint)pointer,
                            (uint)bytes.Length
                        );
                        break;

                    default:
                        throw new NotSupportedException(
                            $"Uniform stage '{uniform.Stage}' is unsupported."
                        );
                }
            }
        }
    }

    private void Render(DrawCommand command)
    {
        var pipeline = GetOrCreatePipeline(command.Pipeline);
        var vertexBuffer = GetOrCreateBuffer(command.VertexBuffer);

        SDL.BindGPUGraphicsPipeline(_renderPass, pipeline);

        SDL.GPUBufferBinding[] bindings = [new() { Buffer = vertexBuffer, Offset = 0 }];

        SDL.BindGPUVertexBuffers(_renderPass, firstSlot: 0, bindings, (uint)bindings.Length);

        BindTextures(command.Textures);
        PushUniforms(command.Uniforms);

        SDL.DrawGPUPrimitives(
            _renderPass,
            command.VertexCount,
            command.InstanceCount,
            command.FirstVertex,
            command.FirstInstance
        );
    }

    private void ResetFrame()
    {
        _commandBuffer = nint.Zero;
        _renderPass = nint.Zero;
        _swapchainTexture = nint.Zero;
        _swapchainAcquired = false;
    }

    private void StopShaderCross()
    {
        if (!_shaderCrossStarted)
            return;

        ShaderCross.Quit();
        _shaderCrossStarted = false;
    }

    /// <summary>
    /// Submits the current command buffer and invalidates the stored frame state.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when SDL cannot submit the command buffer.
    /// </exception>
    private void SubmitCurrentCommandBuffer()
    {
        var commandBuffer = _commandBuffer;

        ResetFrame();

        if (commandBuffer != nint.Zero && !SDL.SubmitGPUCommandBuffer(commandBuffer))
        {
            throw CreateException("Could not submit the SDL GPU command buffer");
        }
    }

    /// <summary>
    /// Uploads managed data into a native SDL GPU buffer.
    /// </summary>
    /// <param name="buffer">
    /// The destination SDL GPU buffer.
    /// </param>
    /// <param name="data">
    /// The bytes to upload.
    /// </param>
    private void UploadBuffer(nint buffer, ReadOnlyMemory<byte> data)
    {
        var size = checked((uint)data.Length);

        var transferInfo = new SDL.GPUTransferBufferCreateInfo
        {
            Usage = SDL.GPUTransferBufferUsage.Upload,
            Size = size,
            Props = 0,
        };

        var transferBuffer = SDL.CreateGPUTransferBuffer(_device, in transferInfo);

        if (transferBuffer == nint.Zero)
        {
            throw CreateException("Could not create an SDL GPU transfer buffer");
        }

        try
        {
            var mapped = SDL.MapGPUTransferBuffer(_device, transferBuffer, cycle: false);

            if (mapped == nint.Zero)
            {
                throw CreateException("Could not map an SDL GPU transfer buffer");
            }

            try
            {
                Marshal.Copy(data.ToArray(), 0, mapped, data.Length);
            }
            finally
            {
                SDL.UnmapGPUTransferBuffer(_device, transferBuffer);
            }

            var commandBuffer = SDL.AcquireGPUCommandBuffer(_device);

            if (commandBuffer == nint.Zero)
            {
                throw CreateException("Could not acquire a command buffer for a GPU upload");
            }

            var commandBufferValid = true;
            var copyPass = nint.Zero;

            try
            {
                copyPass = SDL.BeginGPUCopyPass(commandBuffer);

                if (copyPass == nint.Zero)
                {
                    throw CreateException("Could not begin an SDL GPU copy pass");
                }

                var source = new SDL.GPUTransferBufferLocation
                {
                    TransferBuffer = transferBuffer,
                    Offset = 0,
                };

                var destination = new SDL.GPUBufferRegion
                {
                    Buffer = buffer,
                    Offset = 0,
                    Size = size,
                };

                SDL.UploadToGPUBuffer(copyPass, in source, in destination, cycle: false);

                SDL.EndGPUCopyPass(copyPass);
                copyPass = nint.Zero;

                // Submit consumes the command buffer regardless of whether SDL
                // reports success.
                commandBufferValid = false;

                if (!SDL.SubmitGPUCommandBuffer(commandBuffer))
                {
                    throw CreateException("Could not submit an SDL GPU upload command buffer");
                }
            }
            finally
            {
                if (copyPass != nint.Zero)
                    SDL.EndGPUCopyPass(copyPass);

                if (commandBufferValid)
                    SDL.CancelGPUCommandBuffer(commandBuffer);
            }
        }
        finally
        {
            SDL.ReleaseGPUTransferBuffer(_device, transferBuffer);
        }
    }

    /// <summary>
    /// Uploads the pixels of a graphics texture into an SDL GPU texture.
    /// </summary>
    /// <param name="nativeTexture">
    /// The destination SDL GPU texture.
    /// </param>
    /// <param name="texture">
    /// The graphics texture containing the source pixels.
    /// </param>
    private void UploadTexture(nint nativeTexture, GraphicsTexture texture)
    {
        var size = checked((uint)texture.Data.Length);

        var transferInfo = new SDL.GPUTransferBufferCreateInfo
        {
            Usage = SDL.GPUTransferBufferUsage.Upload,
            Size = size,
            Props = 0,
        };

        var transferBuffer = SDL.CreateGPUTransferBuffer(_device, in transferInfo);

        if (transferBuffer == nint.Zero)
        {
            throw CreateException("Could not create a texture transfer buffer");
        }

        try
        {
            var mapped = SDL.MapGPUTransferBuffer(_device, transferBuffer, cycle: false);

            if (mapped == nint.Zero)
            {
                throw CreateException("Could not map a texture transfer buffer");
            }

            try
            {
                Marshal.Copy(texture.Data.ToArray(), 0, mapped, texture.Data.Length);
            }
            finally
            {
                SDL.UnmapGPUTransferBuffer(_device, transferBuffer);
            }

            var commandBuffer = SDL.AcquireGPUCommandBuffer(_device);

            if (commandBuffer == nint.Zero)
            {
                throw CreateException("Could not acquire a texture upload command buffer");
            }

            var commandBufferValid = true;
            var copyPass = nint.Zero;

            try
            {
                copyPass = SDL.BeginGPUCopyPass(commandBuffer);

                if (copyPass == nint.Zero)
                {
                    throw CreateException("Could not begin a texture copy pass");
                }

                var source = new SDL.GPUTextureTransferInfo
                {
                    TransferBuffer = transferBuffer,
                    Offset = 0,
                    PixelsPerRow = texture.Width,
                    RowsPerLayer = texture.Height,
                };

                var destination = new SDL.GPUTextureRegion
                {
                    Texture = nativeTexture,
                    MipLevel = 0,
                    Layer = 0,
                    X = 0,
                    Y = 0,
                    Z = 0,
                    W = texture.Width,
                    H = texture.Height,
                    D = 1,
                };

                SDL.UploadToGPUTexture(copyPass, in source, in destination, cycle: false);

                SDL.EndGPUCopyPass(copyPass);
                copyPass = nint.Zero;

                commandBufferValid = false;

                if (!SDL.SubmitGPUCommandBuffer(commandBuffer))
                {
                    throw CreateException("Could not submit the texture upload command buffer");
                }
            }
            finally
            {
                if (copyPass != nint.Zero)
                    SDL.EndGPUCopyPass(copyPass);

                if (commandBufferValid)
                    SDL.CancelGPUCommandBuffer(commandBuffer);
            }
        }
        finally
        {
            SDL.ReleaseGPUTransferBuffer(_device, transferBuffer);
        }
    }

    /// <inheritdoc />
    protected override bool OnBeginFrame(RenderContext context)
    {
        _commandBuffer = SDL.AcquireGPUCommandBuffer(_device);

        if (_commandBuffer == nint.Zero)
        {
            throw CreateException("Could not acquire an SDL GPU command buffer");
        }

        if (
            !SDL.WaitAndAcquireGPUSwapchainTexture(
                _commandBuffer,
                _window.Native,
                out _swapchainTexture,
                out _,
                out _
            )
        )
        {
            var message = $"Could not acquire the SDL GPU swapchain texture: " + SDL.GetError();

            CancelCurrentCommandBuffer();

            throw new InvalidOperationException(message);
        }

        // After a successful acquire call, the command buffer must be submitted
        // instead of cancelled, even when no swapchain texture is available.
        _swapchainAcquired = true;

        if (_swapchainTexture == nint.Zero)
        {
            SubmitCurrentCommandBuffer();
            return false;
        }

        var color = context.ClearColor.Clamped();

        SDL.GPUColorTargetInfo[] colorTargets =
        [
            new()
            {
                Texture = _swapchainTexture,

                ClearColor = new SDL.FColor
                {
                    R = color.R,
                    G = color.G,
                    B = color.B,
                    A = color.A,
                },

                LoadOp = SDL.GPULoadOp.Clear,
                StoreOp = SDL.GPUStoreOp.Store,
                Cycle = false,
            },
        ];

        _renderPass = SDL.BeginGPURenderPass(
            _commandBuffer,
            in colorTargets,
            (uint)colorTargets.Length,
            nint.Zero
        );

        if (_renderPass != nint.Zero)
            return true;

        var error = $"Could not begin the SDL GPU render pass: " + SDL.GetError();

        // The swapchain has already been acquired, so this buffer cannot be
        // cancelled.
        SubmitCurrentCommandBuffer();

        throw new InvalidOperationException(error);
    }

    /// <inheritdoc />
    protected override void OnEndFrame(RenderContext context)
    {
        if (_renderPass != nint.Zero)
        {
            SDL.EndGPURenderPass(_renderPass);
            _renderPass = nint.Zero;
        }

        SubmitCurrentCommandBuffer();
    }

    /// <inheritdoc />
    protected override void OnPrepare(
        RenderContext context,
        IReadOnlyList<RenderData> renderingData
    )
    {
        foreach (var data in renderingData)
        {
            foreach (var command in data.Commands)
            {
                switch (command)
                {
                    case DrawCommand draw:
                        Prepare(draw);
                        break;

                    default:
                        throw new NotSupportedException(
                            $"SdlGpuRenderer does not support the command "
                                + $"'{command.GetType().Name}'."
                        );
                }
            }
        }
    }

    /// <inheritdoc />
    protected override void OnRender(RenderContext context, RenderData data)
    {
        foreach (var command in data.Commands)
        {
            switch (command)
            {
                case DrawCommand draw:
                    Render(draw);
                    break;

                default:
                    throw new NotSupportedException(
                        $"SdlGpuRenderer does not support the command "
                            + $"'{command.GetType().Name}'."
                    );
            }
        }
    }

    /// <inheritdoc />
    protected override void OnStart()
    {
        if (!_window.Opened.Get())
        {
            throw new InvalidOperationException(
                "The SDL3 window must be open before starting the GPU renderer."
            );
        }

        if (!ShaderCross.Init())
            throw CreateException("Could not initialize SDL_shadercross");

        _shaderCrossStarted = true;

        const SDL.GPUShaderFormat shaderFormats =
            SDL.GPUShaderFormat.SPIRV | SDL.GPUShaderFormat.DXIL | SDL.GPUShaderFormat.MSL;

        _device = SDL.CreateGPUDevice(shaderFormats, debugMode: true, name: null);

        if (_device == nint.Zero)
        {
            ShaderCross.Quit();
            _shaderCrossStarted = false;

            throw CreateException("Could not create the SDL GPU device");
        }

        if (SDL.ClaimWindowForGPUDevice(_device, _window.Native))
        {
            _window.AttachGPUDevice(_device);
            return;
        }

        SDL.DestroyGPUDevice(_device);
        _device = nint.Zero;

        ShaderCross.Quit();
        _shaderCrossStarted = false;

        throw CreateException("Could not claim the SDL window for the GPU device");
    }

    /// <inheritdoc />
    protected override void OnStop()
    {
        if (_device == nint.Zero)
        {
            StopShaderCross();
            return;
        }

        if (_renderPass != nint.Zero)
        {
            SDL.EndGPURenderPass(_renderPass);
            _renderPass = nint.Zero;
        }

        if (_commandBuffer != nint.Zero)
        {
            var commandBuffer = _commandBuffer;
            var mustSubmit = _swapchainAcquired;

            ResetFrame();

            if (mustSubmit)
                SDL.SubmitGPUCommandBuffer(commandBuffer);
            else
                SDL.CancelGPUCommandBuffer(commandBuffer);
        }
        else
        {
            ResetFrame();
        }

        _swapchainTexture = nint.Zero;

        SDL.WaitForGPUIdle(_device);

        foreach (var pipeline in _pipelines.Values)
            SDL.ReleaseGPUGraphicsPipeline(_device, pipeline);

        foreach (var shader in _shaders.Values)
            SDL.ReleaseGPUShader(_device, shader);

        foreach (var buffer in _buffers.Values)
            SDL.ReleaseGPUBuffer(_device, buffer);

        foreach (var sampler in _samplers.Values)
            SDL.ReleaseGPUSampler(_device, sampler);

        foreach (var texture in _textures.Values)
            SDL.ReleaseGPUTexture(_device, texture);

        _samplers.Clear();
        _textures.Clear();

        _pipelines.Clear();
        _shaders.Clear();
        _buffers.Clear();

        _window.DetachGPUDevice(_device);
        SDL.ReleaseWindowFromGPUDevice(_device, _window.Native);
        SDL.DestroyGPUDevice(_device);

        _device = nint.Zero;

        StopShaderCross();
    }
}
