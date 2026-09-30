using System.Numerics;

namespace Mirage.Graphics.Interfaces;

/// <summary>
/// Provides the state and rendering surface available during a rendering frame.
/// </summary>
public interface IRenderContext
{
    /// <summary>
    /// Gets the elapsed time since the previous rendering frame, in seconds.
    /// </summary>
    double DeltaTime { get; }

    /// <summary>
    /// Gets the transformation from view space into projection space.
    /// </summary>
    Matrix4x4 Projection { get; }

    /// <summary>
    /// Gets the surface to which graphical operations are submitted.
    /// </summary>
    IRenderSurface Surface { get; }

    /// <summary>
    /// Gets the transformation from world space into view space.
    /// </summary>
    Matrix4x4 View { get; }
}
