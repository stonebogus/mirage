namespace Mirage.Graphics.Pipelines;

/// <summary>
/// Specifies how source pixels are combined with destination pixels.
/// </summary>
public enum BlendMode
{
    /// <summary>
    /// Replaces the destination with the source color.
    /// </summary>
    Opaque,

    /// <summary>
    /// Combines source and destination colors using source alpha.
    /// </summary>
    Alpha,
}

/// <summary>
/// Describes the color blending behavior of a graphics pipeline.
/// </summary>
/// <param name="Mode">
/// The blending mode used by the pipeline.
/// </param>
public readonly record struct BlendState(BlendMode Mode)
{
    /// <summary>
    /// Gets the standard source-alpha blending state.
    /// </summary>
    public static BlendState Alpha => new(BlendMode.Alpha);

    /// <summary>
    /// Gets the opaque blending state.
    /// </summary>
    public static BlendState Opaque => new(BlendMode.Opaque);
}
