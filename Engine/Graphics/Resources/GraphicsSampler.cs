using Mirage.Common;

namespace Mirage.Graphics.Resources;

/// <summary>
/// Specifies how texture pixels are filtered.
/// </summary>
public enum TextureFilter
{
    /// <summary>
    /// Selects the nearest texture pixel.
    /// </summary>
    Nearest,

    /// <summary>
    /// Interpolates between neighboring texture pixels.
    /// </summary>
    Linear,
}

/// <summary>
/// Specifies how texture coordinates outside the normalized range are handled.
/// </summary>
public enum TextureAddressMode
{
    /// <summary>
    /// Repeats the texture periodically.
    /// </summary>
    Repeat,

    /// <summary>
    /// Repeats and mirrors the texture periodically.
    /// </summary>
    MirroredRepeat,

    /// <summary>
    /// Clamps coordinates to the texture edge.
    /// </summary>
    ClampToEdge,
}

/// <summary>
/// Describes how a texture is sampled.
/// </summary>
public sealed class GraphicsSampler : Resource
{
    /// <summary>
    /// Initializes a new graphics sampler.
    /// </summary>
    /// <param name="minFilter">
    /// The filter used when the texture is minified.
    /// </param>
    /// <param name="magFilter">
    /// The filter used when the texture is magnified.
    /// </param>
    /// <param name="addressModeU">
    /// The addressing mode for horizontal texture coordinates.
    /// </param>
    /// <param name="addressModeV">
    /// The addressing mode for vertical texture coordinates.
    /// </param>
    public GraphicsSampler(
        TextureFilter minFilter = TextureFilter.Linear,
        TextureFilter magFilter = TextureFilter.Linear,
        TextureAddressMode addressModeU = TextureAddressMode.ClampToEdge,
        TextureAddressMode addressModeV = TextureAddressMode.ClampToEdge
    )
    {
        MinFilter = minFilter;
        MagFilter = magFilter;
        AddressModeU = addressModeU;
        AddressModeV = addressModeV;
    }

    /// <summary>
    /// Gets the horizontal texture addressing mode.
    /// </summary>
    public TextureAddressMode AddressModeU { get; }

    /// <summary>
    /// Gets the vertical texture addressing mode.
    /// </summary>
    public TextureAddressMode AddressModeV { get; }

    /// <summary>
    /// Gets the texture magnification filter.
    /// </summary>
    public TextureFilter MagFilter { get; }

    /// <summary>
    /// Gets the texture minification filter.
    /// </summary>
    public TextureFilter MinFilter { get; }
}
