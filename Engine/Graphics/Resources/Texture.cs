using Mirage.Common;

namespace Mirage.Graphics.Resources;

/// <summary>
/// Initializes a new instance of the <see cref="Texture"/> class.
/// </summary>
/// <remarks>
/// <para>
/// Represents an image used as a texture for drawing.
/// </para>
/// The image contains the pixels in memory. The renderer creates and owns
/// the corresponding SDL texture.
/// </remarks>
/// <param name="image">The image containing the texture pixels.</param>
public sealed class Texture(Image image) : Resource
{
    /// <summary>
    /// Gets the texture height, in pixels.
    /// </summary>
    public uint Height => Image.Height;

    /// <summary>
    /// Gets the image used to create this texture.
    /// </summary>
    public Image Image { get; } = image;

    /// <summary>
    /// Gets the texture width, in pixels.
    /// </summary>
    public uint Width => Image.Width;
}
