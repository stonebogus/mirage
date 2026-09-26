using Mirage.Common;

namespace Mirage.Graphics.Resources;

/// <summary>
/// Specifies synthetic styling applied when drawing text.
/// </summary>
/// <remarks>
/// Use a separate font file for a designed bold or italic variant when available.
/// </remarks>
[Flags]
public enum FontStyle
{
    /// <summary>
    /// Draws text without synthetic styling.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Draws text with synthetic bold styling.
    /// </summary>
    Bold = 1 << 0,

    /// <summary>
    /// Draws text with synthetic italic styling.
    /// </summary>
    Italic = 1 << 1,

    /// <summary>
    /// Draws a line beneath the text.
    /// </summary>
    Underline = 1 << 2,

    /// <summary>
    /// Draws a line through the text.
    /// </summary>
    Strikethrough = 1 << 3,
}

/// <summary>
/// Stores reusable font file data.
/// </summary>
/// <remarks>
/// The resource owns a copy of the file bytes. The renderer uses these bytes
/// to open native font instances at the sizes needed for drawing.
/// </remarks>
public sealed class Font : Resource
{
    private ReadOnlyMemory<byte> _data;

    /// <summary>
    /// Initializes a font from encoded font file data.
    /// </summary>
    /// <param name="data">The font file bytes to copy.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="data"/> is empty.
    /// </exception>
    public Font(ReadOnlyMemory<byte> data)
    {
        if (data.IsEmpty)
            throw new ArgumentException("Font data cannot be empty.", nameof(data));

        _data = data.ToArray();
    }

    /// <summary>
    /// Gets the encoded font file data.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the font has been destroyed.
    /// </exception>
    public ReadOnlyMemory<byte> Data
    {
        get
        {
            ThrowIfDestroyed();
            return _data;
        }
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        _data = ReadOnlyMemory<byte>.Empty;
    }
}
