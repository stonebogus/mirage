using Mirage.Common;

namespace Mirage.Graphics.Resources;

/// <summary>
/// Specifies the storage format of texture pixels.
/// </summary>
public enum TextureFormat
{
    /// <summary>
    /// Four normalized 8-bit red, green, blue and alpha channels.
    /// </summary>
    Rgba8,
}

/// <summary>
/// Represents immutable backend-agnostic two-dimensional texture data.
/// </summary>
public sealed class GraphicsTexture : Resource
{
    /// <summary>
    /// Initializes a new two-dimensional graphics texture.
    /// </summary>
    /// <param name="width">
    /// The texture width, in pixels.
    /// </param>
    /// <param name="height">
    /// The texture height, in pixels.
    /// </param>
    /// <param name="data">
    /// The texture pixels ordered by rows.
    /// </param>
    /// <param name="format">
    /// The format of each texture pixel.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either texture dimension is zero.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the data size does not match the dimensions and format.
    /// </exception>
    public GraphicsTexture(
        uint width,
        uint height,
        ReadOnlyMemory<byte> data,
        TextureFormat format = TextureFormat.Rgba8
    )
    {
        if (width == 0)
            throw new ArgumentOutOfRangeException(nameof(width));

        if (height == 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        var expectedSize = checked((int)(width * height * GetBytesPerPixel(format)));

        if (data.Length != expectedSize)
        {
            throw new ArgumentException(
                $"Texture data contains {data.Length} bytes, but "
                    + $"{expectedSize} bytes were expected.",
                nameof(data)
            );
        }

        Width = width;
        Height = height;

        // Preserve the immutable-resource contract even when the caller
        // supplied memory backed by a mutable array.
        Data = data.ToArray();

        Format = format;
    }

    /// <summary>
    /// Gets the texture pixel data.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>
    /// Gets the texture pixel format.
    /// </summary>
    public TextureFormat Format { get; }

    /// <summary>
    /// Gets the texture height, in pixels.
    /// </summary>
    public uint Height { get; }

    /// <summary>
    /// Gets the texture width, in pixels.
    /// </summary>
    public uint Width { get; }

    /// <summary>
    /// Gets the number of bytes occupied by one pixel of a texture format.
    /// </summary>
    /// <param name="format">
    /// The texture format to inspect.
    /// </param>
    /// <returns>
    /// The number of bytes occupied by one pixel.
    /// </returns>
    public static uint GetBytesPerPixel(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.Rgba8 => 4,

            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
    }
}
