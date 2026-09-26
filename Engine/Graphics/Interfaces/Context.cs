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
    /// Gets the active camera, or <see langword="null"/> when using screen coordinates.
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
    /// Measures text before camera zoom or drawing transformations are applied.
    /// </summary>
    /// <param name="font">The font used to measure the text.</param>
    /// <param name="text">The text to measure.</param>
    /// <param name="size">The font size in points.</param>
    /// <param name="style">The synthetic font style.</param>
    /// <returns>The untransformed text dimensions in coordinate units.</returns>
    Vector2 MeasureText(Font font, string text, float size, FontStyle style = FontStyle.Normal);

    /// <summary>
    /// Draws text across four specified corners.
    /// </summary>
    /// <param name="font">The font used to draw the text.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="size">The font size in points.</param>
    /// <param name="color">The text color.</param>
    /// <param name="style">The synthetic font style.</param>
    /// <param name="topLeft">The top-left corner in screen or world coordinates.</param>
    /// <param name="topRight">The top-right corner in screen or world coordinates.</param>
    /// <param name="bottomRight">The bottom-right corner in screen or world coordinates.</param>
    /// <param name="bottomLeft">The bottom-left corner in screen or world coordinates.</param>
    void DrawText(
        Font font,
        string text,
        float size,
        Color color,
        FontStyle style,
        Vector2 topLeft,
        Vector2 topRight,
        Vector2 bottomRight,
        Vector2 bottomLeft
    );

    /// <summary>
    /// Draws a line with a specified thickness.
    /// </summary>
    /// <param name="start">The start point in screen or world coordinates.</param>
    /// <param name="end">The end point in screen or world coordinates.</param>
    /// <param name="color">The line color.</param>
    /// <param name="thickness">The line thickness in coordinate units. Defaults to 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the thickness is not finite or is not positive.
    /// </exception>
    void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1f);

    /// <summary>
    /// Draws a textured mesh after transforming its local vertices.
    /// </summary>
    /// <param name="mesh">The geometry, texture, and vertex texture coordinates.</param>
    /// <param name="transform">
    /// The transformation from mesh-local coordinates to world coordinates.
    /// When no camera is active, the result uses screen coordinates.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="mesh"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the graphic mesh or one of its referenced resources has been destroyed.
    /// </exception>
    void DrawMesh(GraphicMesh mesh, Matrix3x2 transform);

    /// <summary>
    /// Draws an entire texture across four specified corners.
    /// </summary>
    /// <param name="texture">The texture to draw.</param>
    /// <param name="topLeft">The image's top-left corner in screen or world coordinates.</param>
    /// <param name="topRight">The image's top-right corner in screen or world coordinates.</param>
    /// <param name="bottomRight">The image's bottom-right corner in screen or world coordinates.</param>
    /// <param name="bottomLeft">The image's bottom-left corner in screen or world coordinates.</param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the texture or its image has been destroyed.
    /// </exception>
    void DrawTexture(
        Texture texture,
        Vector2 topLeft,
        Vector2 topRight,
        Vector2 bottomRight,
        Vector2 bottomLeft
    );

    /// <summary>
    /// Fills a circle with a color.
    /// </summary>
    /// <param name="center">The center in screen or world coordinates.</param>
    /// <param name="radius">The radius in coordinate units.</param>
    /// <param name="color">The fill color.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the radius is negative or not finite.
    /// </exception>
    void FillCircle(Vector2 center, float radius, Color color);

    /// <summary>
    /// Fills a rectangle with a color.
    /// </summary>
    /// <param name="position">The top-left position in screen or world coordinates.</param>
    /// <param name="size">The rectangle size in coordinate units.</param>
    /// <param name="color">The fill color.</param>
    void FillRectangle(Vector2 position, Vector2 size, Color color);

    /// <summary>
    /// Fills a triangle with a color.
    /// </summary>
    /// <param name="a">The first vertex in screen or world coordinates.</param>
    /// <param name="b">The second vertex in screen or world coordinates.</param>
    /// <param name="c">The third vertex in screen or world coordinates.</param>
    /// <param name="color">The fill color.</param>
    void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color);
}
