using Mirage.Common;

namespace Mirage.Graphics.Resources;

/// <summary>
/// Represents an image used as a texture for drawing.
/// </summary>
/// <remarks>
/// The image contains the pixels in memory. The renderer creates and owns
/// the corresponding SDL texture.
/// </remarks>
public sealed class Texture : Resource
{
    /// <summary>
    /// Initializes a texture from a decoded image.
    /// </summary>
    /// <param name="image">The image containing the texture pixels.</param>
    public Texture(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);

        if (image.Destroyed)
            throw new ObjectDisposedException(nameof(image));

        Image = image;
    }

    /// <summary>
    /// Gets the texture height, in pixels.
    /// </summary>
    public uint Height => Image.Height;

    /// <summary>
    /// Gets the image used to create this texture.
    /// </summary>
    public Image Image { get; }

    /// <summary>
    /// Gets the texture width, in pixels.
    /// </summary>
    public uint Width => Image.Width;
}
