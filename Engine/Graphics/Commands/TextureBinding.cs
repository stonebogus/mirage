using Mirage.Graphics.Resources;
using Mirage.Graphics.Shaders;

namespace Mirage.Graphics.Commands;

/// <summary>
/// Binds a texture and sampler to a programmable shader stage.
/// </summary>
/// <param name="Slot">
/// The shader resource slot receiving the binding.
/// </param>
/// <param name="Stage">
/// The programmable shader stage that consumes the texture.
/// </param>
/// <param name="Texture">
/// The texture containing the sampled image data.
/// </param>
/// <param name="Sampler">
/// The sampler describing filtering and addressing behavior.
/// </param>
public readonly record struct TextureBinding(
    uint Slot,
    ShaderStage Stage,
    GraphicsTexture Texture,
    GraphicsSampler Sampler
);
