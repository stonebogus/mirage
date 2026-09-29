using System.Numerics;

namespace Mirage.Graphics.Interfaces;

/// <summary>
/// Provides the state and drawing surface available during a drawing frame.
/// </summary>
public interface IDrawContext
{
    /// <summary>
    /// Gets the elapsed time since the previous drawing frame, in seconds.
    /// </summary>
    double DeltaTime { get; }

    /// <summary>
    /// Gets the transformation from view space into projection space.
    /// </summary>
    Matrix4x4 Projection { get; }

    /// <summary>
    /// Gets the surface to which graphical operations are submitted.
    /// </summary>
    ISurface Surface { get; }

    /// <summary>
    /// Gets the transformation from world space into view space.
    /// </summary>
    Matrix4x4 View { get; }
}
