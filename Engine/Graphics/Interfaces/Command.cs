namespace Mirage.Graphics.Interfaces;

/// <summary>
/// Represents a graphical operation that can be submitted to a drawing surface.
/// </summary>
/// <remarks>
/// A draw command describes the data required to perform a graphical operation
/// without defining how that operation is drawn by a particular backend.
/// </remarks>
public interface IDrawCommand;
