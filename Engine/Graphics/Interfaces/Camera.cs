using System.Numerics;

namespace Mirage.Graphics.Interfaces;

/// <summary>
/// Provides the transformations required to render a scene from a camera view.
/// </summary>
public interface ICamera
{
    /// <summary>
    /// Gets the transformation from world space into camera view space.
    /// </summary>
    Matrix4x4 View { get; }

    /// <summary>
    /// Creates the projection transformation for the specified viewport size.
    /// </summary>
    /// <param name="viewportSize">The size of the rendering viewport.</param>
    /// <returns>The projection transformation.</returns>
    Matrix4x4 GetProjection(Vector2 viewportSize);
}
