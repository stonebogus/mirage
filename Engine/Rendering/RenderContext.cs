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
    public void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1f)
    {
        ValidatePoint(start, nameof(start));
        ValidatePoint(end, nameof(end));

        if (!float.IsFinite(thickness) || thickness <= 0f)
            throw new ArgumentOutOfRangeException(nameof(thickness));

        var screenStart = ToScreen(start);
        var screenEnd = ToScreen(end);
        var screenThickness = thickness * Camera?.Zoom ?? thickness;

        if (!float.IsFinite(screenThickness) || screenThickness <= 0f)
            throw new ArgumentOutOfRangeException(nameof(thickness));

        var extent = new Vector2(screenThickness / 2f);
        var minimum = Vector2.Min(screenStart, screenEnd) - extent;
        var maximum = Vector2.Max(screenStart, screenEnd) + extent;

        if (
            maximum.X < 0f
            || maximum.Y < 0f
            || minimum.X > ViewportSize.X
            || minimum.Y > ViewportSize.Y
        )
            return;

        _surface.DrawLine(screenStart, screenEnd, color, screenThickness);
    }

    /// <inheritdoc />
    public void DrawMesh(GraphicMesh mesh, Matrix3x2 transform)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        if (mesh.Destroyed)
            throw new ObjectDisposedException(nameof(mesh));

        if (mesh.Mesh.Destroyed)
            throw new ObjectDisposedException(nameof(mesh.Mesh));

        if (mesh.Texture.Destroyed || mesh.Texture.Image.Destroyed)
            throw new ObjectDisposedException(nameof(mesh.Texture));

        var localVertices = mesh.Mesh.Vertices.Span;
        var screenVertices = new Vector2[localVertices.Length];

        var minimum = new Vector2(float.PositiveInfinity);
        var maximum = new Vector2(float.NegativeInfinity);

        for (var index = 0; index < localVertices.Length; index++)
        {
            var worldPosition = Vector2.Transform(localVertices[index], transform);
            var screenPosition = ToScreen(worldPosition);

            ValidatePoint(screenPosition, nameof(transform));

            screenVertices[index] = screenPosition;
            minimum = Vector2.Min(minimum, screenPosition);
            maximum = Vector2.Max(maximum, screenPosition);
        }

        if (!IsVisible(minimum, maximum - minimum))
            return;

        _surface.DrawMesh(mesh, screenVertices);
    }

    /// <inheritdoc />
    public void DrawTexture(
        Texture texture,
        Vector2 topLeft,
        Vector2 topRight,
        Vector2 bottomRight,
        Vector2 bottomLeft
    )
    {
        ValidatePoint(topLeft, nameof(topLeft));
        ValidatePoint(topRight, nameof(topRight));
        ValidatePoint(bottomRight, nameof(bottomRight));
        ValidatePoint(bottomLeft, nameof(bottomLeft));

        var screenTopLeft = ToScreen(topLeft);
        var screenTopRight = ToScreen(topRight);
        var screenBottomRight = ToScreen(bottomRight);
        var screenBottomLeft = ToScreen(bottomLeft);

        ValidatePoint(screenTopLeft, nameof(topLeft));
        ValidatePoint(screenTopRight, nameof(topRight));
        ValidatePoint(screenBottomRight, nameof(bottomRight));
        ValidatePoint(screenBottomLeft, nameof(bottomLeft));

        var minimum = Vector2.Min(
            Vector2.Min(screenTopLeft, screenTopRight),
            Vector2.Min(screenBottomRight, screenBottomLeft)
        );

        var maximum = Vector2.Max(
            Vector2.Max(screenTopLeft, screenTopRight),
            Vector2.Max(screenBottomRight, screenBottomLeft)
        );

        if (!IsVisible(minimum, maximum - minimum))
            return;

        _surface.DrawTexture(
            texture,
            screenTopLeft,
            screenTopRight,
            screenBottomRight,
            screenBottomLeft
        );
    }

    /// <inheritdoc />
    public void FillCircle(Vector2 center, float radius, Color color)
    {
        ValidatePoint(center, nameof(center));

        if (!float.IsFinite(radius) || radius < 0f)
            throw new ArgumentOutOfRangeException(nameof(radius));

        if (radius == 0f)
            return;

        var screenCenter = ToScreen(center);
        var screenRadius = radius * Camera?.Zoom ?? radius;

        if (!float.IsFinite(screenRadius) || screenRadius <= 0f)
            return;

        var extent = new Vector2(screenRadius);

        if (!IsVisible(screenCenter - extent, extent * 2f))
            return;

        _surface.FillCircle(screenCenter, screenRadius, color);
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
    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        ValidatePoint(a, nameof(a));
        ValidatePoint(b, nameof(b));
        ValidatePoint(c, nameof(c));

        var screenA = ToScreen(a);
        var screenB = ToScreen(b);
        var screenC = ToScreen(c);

        var minimum = Vector2.Min(screenA, Vector2.Min(screenB, screenC));
        var maximum = Vector2.Max(screenA, Vector2.Max(screenB, screenC));

        if (!IsVisible(minimum, maximum - minimum))
            return;

        _surface.FillTriangle(screenA, screenB, screenC, color);
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

    /// <inheritdoc />
    public Vector2 MeasureText(
        Font font,
        string text,
        float size,
        FontStyle style = FontStyle.Normal
    )
    {
        return _surface.MeasureText(font, text, size, style);
    }

    /// <inheritdoc />
    public void DrawText(
        Font font,
        string text,
        float size,
        Color color,
        FontStyle style,
        Vector2 topLeft,
        Vector2 topRight,
        Vector2 bottomRight,
        Vector2 bottomLeft
    )
    {
        ValidatePoint(topLeft, nameof(topLeft));
        ValidatePoint(topRight, nameof(topRight));
        ValidatePoint(bottomRight, nameof(bottomRight));
        ValidatePoint(bottomLeft, nameof(bottomLeft));

        var screenTopLeft = ToScreen(topLeft);
        var screenTopRight = ToScreen(topRight);
        var screenBottomRight = ToScreen(bottomRight);
        var screenBottomLeft = ToScreen(bottomLeft);

        ValidatePoint(screenTopLeft, nameof(topLeft));
        ValidatePoint(screenTopRight, nameof(topRight));
        ValidatePoint(screenBottomRight, nameof(bottomRight));
        ValidatePoint(screenBottomLeft, nameof(bottomLeft));

        var minimum = Vector2.Min(
            Vector2.Min(screenTopLeft, screenTopRight),
            Vector2.Min(screenBottomRight, screenBottomLeft)
        );

        var maximum = Vector2.Max(
            Vector2.Max(screenTopLeft, screenTopRight),
            Vector2.Max(screenBottomRight, screenBottomLeft)
        );

        if (!IsVisible(minimum, maximum - minimum))
            return;

        _surface.DrawText(
            font,
            text,
            size,
            color,
            style,
            screenTopLeft,
            screenTopRight,
            screenBottomRight,
            screenBottomLeft
        );
    }

    private Vector2 ToScreenSize(Vector2 size) => Camera is null ? size : size * Camera.Zoom;

    private static void ValidatePoint(Vector2 point, string parameterName)
    {
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
            throw new ArgumentOutOfRangeException(parameterName);
    }
}
