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
    /// Gets the elapsed time since the previous frame, in seconds.
    /// </summary>
    double DeltaTime { get; }

    /// <summary>
    /// Draws an entire texture at the specified position and size.
    /// </summary>
    /// <param name="texture">The texture to draw.</param>
    /// <param name="position">The destination's top-left position.</param>
    /// <param name="size">The destination's width and height.</param>
    void DrawTexture(Texture texture, Vector2 position, Vector2 size);

    /// <summary>
    /// Fills a rectangle with a color.
    /// </summary>
    /// <param name="position">The rectangle's top-left position.</param>
    /// <param name="size">The rectangle's width and height.</param>
    /// <param name="color">The fill color.</param>
    void FillRectangle(Vector2 position, Vector2 size, Color color);
}
