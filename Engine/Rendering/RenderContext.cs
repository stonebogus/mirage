using System.Numerics;
using Mirage.Graphics.Interfaces;
using Mirage.Graphics.Primitives;
using Mirage.Graphics.Resources;

namespace Mirage.Rendering;

/// <summary>
/// Provides drawing operations for a single rendering frame.
/// </summary>
/// <remarks>
/// Converts world coordinates to screen coordinates when a camera is active,
/// then forwards drawing operations to the rendering surface. This context
/// is valid only while its frame is active.
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
    /// <param name="camera">
    /// The active camera, or <see langword="null"/> to use screen coordinates.
    /// </param>
    /// <param name="viewportSize">The size of the rendering viewport.</param>
    internal RenderContext(
        RenderSurface surface,
        double deltaTime,
        ICamera? camera,
        Vector2 viewportSize
    )
    {
        _surface = surface;
        DeltaTime = deltaTime;
        Camera = camera;
        ViewportSize = viewportSize;
    }

    /// <inheritdoc />
    public ICamera? Camera { get; }

    /// <inheritdoc />
    public double DeltaTime { get; }

    /// <inheritdoc />
    public void DrawTexture(Texture texture, Vector2 position, Vector2 size)
    {
        var screenPosition = ToScreen(position);
        var screenSize = ToScreenSize(size);

        if (!IsVisible(screenPosition, screenSize))
            return;

        _surface.DrawTexture(texture, screenPosition, screenSize);
    }

    /// <inheritdoc />
    public void FillRectangle(Vector2 position, Vector2 size, Color color)
    {
        var screenPosition = ToScreen(position);
        var screenSize = ToScreenSize(size);

        if (!IsVisible(screenPosition, screenSize))
            return;

        _surface.FillRectangle(screenPosition, screenSize, color);
    }

    /// <inheritdoc />
    public Vector2 ViewportSize { get; }

    private bool IsVisible(Vector2 position, Vector2 size) =>
        position.X < ViewportSize.X
        && position.Y < ViewportSize.Y
        && position.X + size.X > 0f
        && position.Y + size.Y > 0f;

    private Vector2 ToScreen(Vector2 position)
    {
        if (Camera is null)
            return position;

        return (position - Camera.Position) * Camera.Zoom + ViewportSize / 2f;
    }

    private Vector2 ToScreenSize(Vector2 size) => Camera is null ? size : size * Camera.Zoom;
}
