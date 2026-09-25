using Mirage.Graphics.Resources;
using Mirage.Loading;
using StbImageSharp;

namespace Mirage.Graphics.Decoders;

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

    public override bool Supports(string extension)
    {
        return Extensions.Contains(extension);
    }
}
