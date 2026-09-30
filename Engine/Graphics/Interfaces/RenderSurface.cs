using Mirage.Graphics.Primitives;

namespace Mirage.Graphics.Interfaces;

/// <summary>
/// Represents a surface to which graphical operations can be submitted.
/// </summary>
/// <remarks>
/// A surface provides a backend-independent destination for render commands and
/// describes the viewport used for rendering.
/// </remarks>
public interface IRenderSurface
{
    /// <summary>
    /// Gets the viewport associated with this surface.
    /// </summary>
    Viewport Viewport { get; }

    /// <summary>
    /// Submits a graphical operation to the surface.
    /// </summary>
    /// <typeparam name="TCommand">
    /// The type of render command to submit.
    /// </typeparam>
    /// <param name="command">
    /// The render command containing the data required for the graphical operation.
    /// </param>
    void Render<TCommand>(TCommand command)
        where TCommand : IRenderCommand;
}
