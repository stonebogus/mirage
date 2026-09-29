using Mirage.Graphics.Primitives;

namespace Mirage.Graphics.Interfaces;

/// <summary>
/// Represents a surface to which graphical operations can be submitted.
/// </summary>
/// <remarks>
/// A surface provides a backend-independent destination for draw commands and
/// describes the viewport used for drawing.
/// </remarks>
public interface ISurface
{
    /// <summary>
    /// Gets the viewport associated with this surface.
    /// </summary>
    Viewport Viewport { get; }

    /// <summary>
    /// Submits a graphical operation to the surface.
    /// </summary>
    /// <typeparam name="TCommand">
    /// The type of draw command to submit.
    /// </typeparam>
    /// <param name="command">
    /// The draw command containing the data required for the graphical operation.
    /// </param>
    void Draw<TCommand>(TCommand command)
        where TCommand : IDrawCommand;
}
