using System.Text;
using Mirage.Graphics.Resources;
using SDL = global::SDL3.SDL;

namespace Mirage.Rendering.SDL3.Resources;

/// <summary>Owns a validated SPIR-V shader compatible with the Render2D shader ABI.</summary>
internal sealed class SDL3Shader : IDisposable
{
    private readonly IntPtr _device;

    /// <summary>Reflects the resource bindings, validates the ABI, and creates the native shader.</summary>
    /// <param name="device">The borrowed GPU device.</param>
    /// <param name="source">The borrowed, compiled shader.</param>
    public unsafe SDL3Shader(IntPtr device, Shader source)
    {
        _device = device;
        Source = source;
        Layout = SDL3SpirV.Inspect(source);
        var entryPoint = Encoding.UTF8.GetBytes(source.EntryPoint + '\0');
        fixed (byte* code = source.Code.Span)
        fixed (byte* entry = entryPoint)
        {
            Native = SDL.CreateGPUShader(
                device,
                new SDL.GPUShaderCreateInfo
                {
                    Code = (IntPtr)code,
                    CodeSize = checked((nuint)source.Code.Length),
                    _entrypoint = (IntPtr)entry,
                    Format = SDL.GPUShaderFormat.SPIRV,
                    Stage =
                        source.Stage == ShaderStage.Vertex
                            ? SDL.GPUShaderStage.Vertex
                            : SDL.GPUShaderStage.Fragment,
                    NumSamplers = Layout.Samplers,
                    NumUniformBuffers = Layout.UniformBuffers,
                    NumStorageBuffers = 0,
                    NumStorageTextures = 0,
                }
            );
        }
        if (Native == IntPtr.Zero)
            throw SDL3Gpu.Error("Creating a GPU shader");
    }

    /// <summary>Gets the reflected sampler and uniform-buffer counts.</summary>
    public SDL3ShaderLayout Layout { get; }

    /// <summary>Gets the native handle, or zero after disposal.</summary>
    public IntPtr Native { get; private set; }

    /// <summary>Gets the borrowed source used for cache lifetime checks.</summary>
    public Shader Source { get; }

    /// <summary>Schedules release without destroying the source shader.</summary>
    public void Dispose()
    {
        if (Native == IntPtr.Zero)
            return;
        SDL.ReleaseGPUShader(_device, Native);
        Native = IntPtr.Zero;
    }
}
