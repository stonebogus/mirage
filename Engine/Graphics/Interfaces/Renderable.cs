namespace Mirage.Graphics.Interfaces;

/// <summary>
/// Defines an object capable of producing graphical data for rendering.
/// </summary>
public interface IRenderable
{
    /// <summary>
    /// Produces the graphical data required to render this object.
    /// </summary>
    /// <param name="context">
    /// The context describing the current rendering frame.
    /// </param>
    /// <returns>
    /// The graphics data produced by this object.
    /// </returns>
    RenderData Render(RenderContext context);
}
