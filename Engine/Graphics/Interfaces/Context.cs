using System.Numerics;
using Mirage.Graphics.Primitives;
using Mirage.Graphics.Resources;

namespace Mirage.Graphics.Interfaces;

/// <summary>
/// Defines the drawing operations available during a rendering frame.
/// </summary>
/// <remarks>
/// Drawable objects use this interface without depending on the renderer
/// that executes the operations.
/// </remarks>
public interface IDrawContext
{
    /// <summary>
    /// Gets the active camera, or null when using screen coordinates.
    /// </summary>
    ICamera? Camera { get; }

    /// <summary>
    /// Gets the elapsed time since the previous frame, in seconds.
    /// </summary>
    double DeltaTime { get; }

    /// <summary>
    /// Gets the size of the rendering viewport.
    /// </summary>
    Vector2 ViewportSize { get; }

    /// <summary>
    /// Draws an entire texture at the specified position and size.
    /// </summary>
    void DrawTexture(Texture texture, Vector2 position, Vector2 size);

    /// <summary>
    /// Fills a rectangle with a color.
    /// </summary>
    void FillRectangle(Vector2 position, Vector2 size, Color color);
}
