using Mirage.Graphics.Resources;
using Mirage.Loading;
using StbImageSharp;

namespace Mirage.Graphics.Decoders;

/// <summary>
/// Decodes supported image files into RGBA8 <see cref="Image"/> resources.
/// </summary>
/// <remarks>
/// The decoder accepts PNG, JPEG, BMP, TGA, PSD, GIF and HDR files.
/// </remarks>
public sealed class ImageDecoder : Decoder<Image>
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".bmp",
        ".tga",
        ".psd",
        ".gif",
        ".hdr",
    };

    /// <inheritdoc />
    protected override Image OnDecode(LoadContext context, Stream stream)
    {
        var result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        return new Image(
            checked((uint)result.Width),
            checked((uint)result.Height),
            result.Data,
            RasterImageFormat.Rgba8
        );
    }

    /// <inheritdoc />
    public override bool Supports(string extension)
    {
        return Extensions.Contains(extension);
    }
}
