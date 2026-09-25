namespace Mirage.Graphics.Interfaces;

/// <summary>
/// Defines an object that can draw itself during a rendering frame.
/// </summary>
/// <remarks>
/// The renderer calls <see cref="Draw"/> when the object should appear
/// in the current frame.
/// </remarks>
public interface IDrawable
{
    /// <summary>
    /// Draws this object using the active drawing context.
    /// </summary>
    /// <param name="context">
    /// The context that provides drawing operations for the current frame.
    /// </param>
    void Draw(IDrawContext context);
}
