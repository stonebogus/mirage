using Mirage.Common.Events;
using Mirage.Graph;
using Mirage.Math.Vectors;

namespace Mirage.Spatial.Nodes;

/// <summary>
/// Provides optional values used to initialize a <see cref="Node2D"/>.
/// </summary>
public sealed class Node2DOptions : NodeOptions
{
    /// <summary>
    /// Gets the initial local position of the node.
    /// </summary>
    public Vector2D Position { get; init; } = new(0, 0);

    /// <summary>
    /// Gets the initial local rotation of the node, expressed in radians.
    /// </summary>
    public double Rotation { get; init; }

    /// <summary>
    /// Gets the initial local scale of the node.
    /// </summary>
    public Vector2D Scale { get; init; } = new(1, 1);
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
public class Node2D(string name, Node2DOptions? options = null) : Node(name, options)
{
    /// <summary>
    /// Gets the local position of the node.
    /// </summary>
    public Store<Vector2D> Position { get; } = new(options?.Position ?? new Vector2D());

    /// <summary>
    /// Gets the local rotation of the node, expressed in radians.
    /// </summary>
    public Store<double> Rotation { get; } = new(options?.Rotation ?? 0);

    /// <summary>
    /// Gets the local scale of the node.
    /// </summary>
    public Store<Vector2D> Scale { get; } = new(options?.Scale ?? new Vector2D(1, 1));
}
