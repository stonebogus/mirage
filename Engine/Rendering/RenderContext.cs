using System.Numerics;
using Mirage.Graphics.Interfaces;
using Mirage.Graphics.Primitives;
using Mirage.Graphics.Resources;

namespace Mirage.Rendering;

/// <summary>
/// Provides drawing operations for a single rendering frame.
/// </summary>
/// <remarks>
/// Forwards drawing operations to the active rendering surface.
/// This context is valid only while its frame is active.
/// </remarks>
public sealed class RenderContext : IDrawContext
{
    private readonly RenderSurface _surface;

    /// <summary>
    /// Initializes a context for the active rendering frame.
    /// </summary>
    /// <param name="surface">The surface receiving drawing operations.</param>
    /// <param name="deltaTime">
    /// The elapsed time since the previous frame, in seconds.
    /// </param>
    internal RenderContext(RenderSurface surface, double deltaTime)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _surface = surface;
        DeltaTime = deltaTime;
    }

    /// <inheritdoc />
    public double DeltaTime { get; }

    /// <inheritdoc />
    public void DrawTexture(Texture texture, Vector2 position, Vector2 size)
    {
        _surface.DrawTexture(texture, position, size);
    }

    /// <inheritdoc />
    public void FillRectangle(Vector2 position, Vector2 size, Color color)
    {
        _surface.FillRectangle(position, size, color);
    }
}
