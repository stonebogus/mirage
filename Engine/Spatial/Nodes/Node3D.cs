using Mirage.Common.Events;
using Mirage.Graph;
using Mirage.Math;
using Mirage.Math.Vectors;

namespace Mirage.Spatial.Nodes;

/// <summary>
/// Provides optional values used to initialize a <see cref="Node3D"/>.
/// </summary>
public sealed class Node3DOptions : NodeOptions
{
    /// <summary>
    /// Gets the initial local position of the node.
    /// </summary>
    public Vector3D Position { get; init; } = new(0, 0, 0);

    /// <summary>
    /// Gets the initial local rotation of the node.
    /// </summary>
    public Quaternion Rotation { get; init; } = Quaternion.Identity;

    /// <summary>
    /// Gets the initial local scale of the node.
    /// </summary>
    public Vector3D Scale { get; init; } = new(1, 1, 1);
}

/// <summary>
/// Represents a node positioned in three-dimensional space.
/// </summary>
/// <param name="name">The initial name of the node.</param>
/// <param name="options">
/// The optional values used to initialize the node.
/// </param>
/// <remarks>
/// Position, rotation, and scale are relative to the node's spatial parent.
/// Rotation is represented by a quaternion.
/// </remarks>
public class Node3D(string name, Node3DOptions? options = null) : Node(name, options)
{
    /// <summary>
    /// Gets the local position of the node.
    /// </summary>
    public Store<Vector3D> Position { get; } = new(options?.Position ?? new Vector3D());

    /// <summary>
    /// Gets the local rotation of the node.
    /// </summary>
    public Store<Quaternion> Rotation { get; } = new(options?.Rotation ?? Quaternion.Identity);

    /// <summary>
    /// Gets the local scale of the node.
    /// </summary>
    public Store<Vector3D> Scale { get; } = new(options?.Scale ?? new Vector3D(1, 1, 1));
}
