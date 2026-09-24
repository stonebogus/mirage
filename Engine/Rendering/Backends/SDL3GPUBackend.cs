using System.Runtime.InteropServices;
using Mirage.Graphics;
using Mirage.Graphics.Commands;
using Mirage.Graphics.Pipelines;
using Mirage.Graphics.Resources;
using Mirage.Graphics.Shaders;
using Mirage.Graphics.Vertices;
using Mirage.Windowing.Windows;
using SDL3;

namespace Mirage.Rendering.Backends;

/// <summary>
/// Implements GPU rendering using the SDL3 GPU API.
/// </summary>
public sealed class SDL3GPUBackend : RendererBackend
{
    private readonly Dictionary<GraphicsBuffer, nint> _buffers = [];
    private readonly Dictionary<GraphicsPipeline, nint> _pipelines = [];
    private readonly Dictionary<GraphicsSampler, nint> _samplers = [];
    private readonly Dictionary<Shader, nint> _shaders = [];

    private readonly Dictionary<GraphicsTexture, nint> _textures = [];
    private readonly SDL3Window _window;

    private nint _commandBuffer;
    private nint _device;
    private nint _renderPass;

    private bool _shaderCrossStarted;

    private bool _swapchainAcquired;
    private nint _swapchainTexture;

    /// <summary>
    /// Initializes a new SDL3 GPU rendering backend.
    /// </summary>
    /// <param name="window">
    /// The SDL3 window used as the rendering target.
    /// </param>
    public SDL3GPUBackend(SDL3Window window)
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
                $"SDL3GPUBackend cannot compile the shader language '{shader.Language}'."
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
    /// The backend-agnostic source texture.
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
                            $"SDL3GPUBackend does not support the command "
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
                        $"SDL3GPUBackend does not support the command "
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
                "The SDL3 window must be open before starting the GPU backend."
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
