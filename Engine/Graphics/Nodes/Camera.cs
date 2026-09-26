using System.Numerics;
using Mirage.Common.Events;
using Mirage.Graphics.Interfaces;
using Mirage.Spatial.Nodes;

namespace Mirage.Graphics.Nodes;

/// <summary>
/// Provides optional values used to initialize a <see cref="Camera"/>.
/// </summary>
public sealed class CameraNodeOptions : SpatialNodeOptions
{
    /// <summary>
    /// Gets the initial zoom factor. The default is <c>1</c>, which means no zoom.
    /// </summary>
    public float Zoom { get; init; } = 1f;
}

/// <summary>
/// Represents a camera positioned in the scene.
/// </summary>
public class Camera : SpatialNode, ICamera
{
    /// <summary>
    /// Gets the zoom factor applied to world coordinates. A value of <c>1</c> means no zoom.
    /// </summary>
    public readonly Store<float> Zoom;

    /// <summary>
    /// Initializes a camera node.
    /// </summary>
    /// <param name="name">The node name.</param>
    /// <param name="options">The initial camera and spatial values, or <see langword="null"/> for defaults.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="options"/> specifies a zoom that is not finite
    /// or is less than or equal to zero.
    /// </exception>
    public Camera(string name = "Camera", CameraNodeOptions? options = null)
        : base(name, options)
    {
        options ??= new CameraNodeOptions();

        if (!float.IsFinite(options.Zoom) || options.Zoom <= 0f)
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.Zoom,
                "Camera zoom must be finite and greater than zero."
            );

        Zoom = new Store<float>(options.Zoom);
    }

    Vector2 ICamera.Position => GlobalPosition;

    float ICamera.Zoom => Zoom.Get();

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        Zoom.Destroy();
    }
}
