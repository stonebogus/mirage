namespace Mirage.Graphics.Interfaces;

/// <summary>
/// Defines an object that can render itself during a rendering frame.
/// </summary>
/// <remarks>
/// The rendering system calls <see cref="Render"/> when the object should appear
/// in the current frame.
/// </remarks>
public interface IRenderable
{
    /// <summary>
    /// Renders this object using the active rendering context.
    /// </summary>
    /// <param name="context">
    /// The context that provides rendering state and a surface for the current frame.
    /// </param>
    void Render(IRenderContext context);
}
