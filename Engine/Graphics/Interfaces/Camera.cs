using System.Numerics;

namespace Mirage.Graphics.Interfaces;

/// <summary>
/// Provides the transformations used to convert world coordinates
/// into view and projection space.
/// </summary>
public interface ICamera
{
    /// <summary>
    /// Gets the transformation from world space into camera view space.
    /// </summary>
    Matrix4x4 View { get; }

    /// <summary>
    /// Gets the transformation from view space into projection space.
    /// </summary>
    Matrix4x4 Projection { get; }
}
