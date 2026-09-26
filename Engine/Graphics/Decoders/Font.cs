using Mirage.Graphics.Resources;
using Mirage.Loading;

namespace Mirage.Graphics.Decoders;

/// <summary>
/// Loads supported font files as reusable font data.
/// </summary>
public sealed class FontDecoder : Decoder<Font>
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ttf",
        ".otf",
    };

    /// <inheritdoc />
    protected override Font OnDecode(LoadContext context, Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);

        return new Font(buffer.ToArray());
    }

    /// <inheritdoc />
    public override bool Supports(string extension)
    {
        return Extensions.Contains(extension);
    }
}
