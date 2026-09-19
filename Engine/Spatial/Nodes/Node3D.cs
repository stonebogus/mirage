using Mirage.Common.Events;
using Mirage.Graph;
using Mirage.Math;
using Mirage.Math.Vectors;

namespace Mirage.Spatial.Nodes;

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
public class Node3D(string name, NodeOptions? options = null) : Node(name, options)
{
    /// <summary>
    /// Gets the local position of the node.
    /// </summary>
    public Store<Vector3D> Position { get; } = new(new Vector3D(0, 0, 0));

    /// <summary>
    /// Gets the local rotation of the node.
    /// </summary>
    /// <remarks>
    /// The identity quaternion represents a node without rotation.
    /// </remarks>
    public Store<Quaternion> Rotation { get; } = new(Quaternion.Identity);

    /// <summary>
    /// Gets the local scale of the node.
    /// </summary>
    /// <remarks>
    /// A value of <c>(1, 1, 1)</c> represents the original scale.
    /// </remarks>
    public Store<Vector3D> Scale { get; } = new(new Vector3D(1, 1, 1));
}
