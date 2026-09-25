using System.Numerics;
using Mirage.Graphics.Primitives;
using Mirage.Graphics.Resources;

namespace Mirage.Graphics;

/// <summary>
/// Provides drawing operations for a single rendering frame.
/// </summary>
/// <remarks>
/// Renderable objects use this context to draw without depending on SDL.
/// A renderer-specific implementation performs the actual drawing.
/// </remarks>
public abstract class RenderContext
{
    /// <summary>
    /// Initializes a rendering context for a frame.
    /// </summary>
    /// <param name="deltaTime">
    /// The elapsed time since the previous frame, in seconds.
    /// </param>
    protected RenderContext(double deltaTime)
    {
        DeltaTime = deltaTime;
    }

    /// <summary>
    /// Gets the elapsed time since the previous frame, in seconds.
    /// </summary>
    public double DeltaTime { get; }

    /// <summary>
    /// Draws an entire texture at the specified position and size.
    /// </summary>
    /// <param name="texture">The texture to draw.</param>
    /// <param name="position">The destination's top-left position.</param>
    /// <param name="size">The destination's width and height.</param>
    public abstract void DrawTexture(Texture texture, Vector2 position, Vector2 size);

    /// <summary>
    /// Fills a rectangle at the specified position and size.
    /// </summary>
    /// <param name="position">The rectangle's top-left position.</param>
    /// <param name="size">The rectangle's width and height.</param>
    /// <param name="color">The fill color.</param>
    public abstract void FillRectangle(Vector2 position, Vector2 size, Color color);
}
