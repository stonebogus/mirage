using System.Numerics;
using Mirage.Common.Events;
using Mirage.Noding;

namespace Mirage.Spatial;

/// <summary>
/// Provides optional values used to initialize a <see cref="SpatialNode"/>.
/// </summary>
public sealed class SpatialNodeOptions : NodeOptions
{
    /// <summary>
    /// Gets the initial local position of the node.
    /// </summary>
    public Vector2 Position { get; init; } = new(0, 0);

    /// <summary>
    /// Gets the initial local rotation of the node, expressed in radians.
    /// </summary>
    public float Rotation { get; init; }

    /// <summary>
    /// Gets the initial local scale of the node.
    /// </summary>
    public Vector2 Scale { get; init; } = new(1, 1);
}

/// <summary>
/// Represents a node positioned in two-dimensional space.
/// </summary>
/// <param name="name">The initial name of the node.</param>
/// <param name="options">
/// The optional values used to initialize the node.
/// </param>
/// <remarks>
/// Position, rotation, and scale are relative to the node's spatial parent.
/// Rotation is expressed in radians.
/// </remarks>
public class SpatialNode(string name = "SpatialNode", SpatialNodeOptions? options = null) : Node(name, options)
{
    /// <summary>
    /// Gets the local position of the node.
    /// </summary>
    public Store<Vector2> Position { get; } = new(options?.Position ?? new Vector2());

    /// <summary>
    /// Gets the local rotation of the node, expressed in radians.
    /// </summary>
    public Store<float> Rotation { get; } = new(options?.Rotation ?? 0);

    /// <summary>
    /// Gets the local scale of the node.
    /// </summary>
    public Store<Vector2> Scale { get; } = new(options?.Scale ?? new Vector2(1, 1));
}
