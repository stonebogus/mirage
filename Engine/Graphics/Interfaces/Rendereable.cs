namespace Mirage.Graphics.Interfaces;

/// <summary>
/// Defines an object capable of producing graphical content for rendering.
/// </summary>
public interface IRenderable
{
    /// <summary>
    /// Creates the rendering context that describes how this object should
    /// be rendered.
    /// </summary>
    /// <returns>
    /// A rendering context containing the graphical data required to render
    /// this object.
    /// </returns>
    RenderContext Render(RenderContext context);
}
