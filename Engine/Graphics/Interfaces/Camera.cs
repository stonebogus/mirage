using System.Numerics;

namespace Mirage.Graphics.Interfaces;

/// <summary>
/// Provides the view used to convert world coordinates into screen coordinates.
/// </summary>
public interface ICamera
{
    /// <summary>
    /// Gets the camera's position in world coordinates.
    /// </summary>
    Vector2 Position { get; }

    /// <summary>
    /// Gets the camera's zoom factor. A value of 1 means no zoom.
    /// </summary>
    float Zoom { get; }
}
