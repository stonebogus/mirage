using Mirage.Graphics.Primitives;

namespace Mirage.Graphics;

/// <summary>
/// Describes the current rendering frame.
/// </summary>
/// <param name="deltaTime">
/// The elapsed time since the previous frame, in seconds.
/// </param>
/// <param name="clearColor">
/// The color used to clear the frame.
/// </param>
public sealed class RenderContext(double deltaTime, Color clearColor)
{
    /// <summary>
    /// Gets the color used to clear the frame.
    /// </summary>
    public Color ClearColor { get; } = clearColor;

    /// <summary>
    /// Gets the elapsed time since the previous frame, in seconds.
    /// </summary>
    public double DeltaTime { get; } = deltaTime;
}
