using Mirage.Loader.Resources;
using StbImageSharp;

namespace Mirage.Loader.Decoders;

public sealed class RasterImageDecoder : Decoder<RasterImage>
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

    protected override RasterImage OnDecode(LoadContext context, Stream stream)
    {
        var result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        return new RasterImage(
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
