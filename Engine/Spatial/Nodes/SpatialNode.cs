using System.Numerics;
using Mirage.Common.Events;
using Mirage.Noding;

namespace Mirage.Spatial.Nodes;

/// <summary>
/// Provides optional values used to initialize a <see cref="SpatialNode"/>.
/// </summary>
public class SpatialNodeOptions : NodeOptions
{
    /// <summary>
    /// Gets the initial position relative to the node's spatial parent.
    /// </summary>
    public Vector2 Position { get; init; } = Vector2.Zero;

    /// <summary>
    /// Gets the initial local rotation, expressed in radians.
    /// </summary>
    public float Rotation { get; init; }

    /// <summary>
    /// Gets the initial local scale.
    /// </summary>
    public Vector2 Scale { get; init; } = Vector2.One;
}

/// <summary>
/// Represents a node positioned in two-dimensional space.
/// </summary>
/// <remarks>
/// Position, rotation, and scale are local to the node. Ordinary nodes between
/// spatial nodes organize the tree without adding a transformation.
/// </remarks>
public class SpatialNode(string name = "SpatialNode", SpatialNodeOptions? options = null)
    : Node(name, options)
{
    /// <summary>
    /// Gets the world position of this node's origin.
    /// </summary>
    public Vector2 GlobalPosition => Vector2.Transform(Vector2.Zero, GlobalTransform);

    /// <summary>
    /// Gets this node's transformation after applying its spatial ancestors.
    /// </summary>
    /// <remarks>
    /// Non-spatial ancestors are skipped. If there is no spatial ancestor,
    /// this node's local transformation is also its global transformation.
    /// </remarks>
    public Matrix3x2 GlobalTransform
    {
        get
        {
            var transform = LocalTransform;
            var ancestor = Parent.Get();

            while (ancestor is not null)
            {
                if (ancestor is SpatialNode spatial)
                    transform *= spatial.LocalTransform;

                ancestor = ancestor.Parent.Get();
            }

            return transform;
        }
    }

    /// <summary>
    /// Gets the transformation defined by this node's local values.
    /// </summary>
    public Matrix3x2 LocalTransform =>
        Matrix3x2.CreateScale(Scale.Get())
        * Matrix3x2.CreateRotation(Rotation.Get())
        * Matrix3x2.CreateTranslation(Position.Get());

    /// <summary>
    /// Gets the position relative to the node's spatial parent.
    /// </summary>
    public Store<Vector2> Position { get; } = new(options?.Position ?? Vector2.Zero);

    /// <summary>
    /// Gets the local rotation, expressed in radians.
    /// </summary>
    public Store<float> Rotation { get; } = new(options?.Rotation ?? 0f);

    /// <summary>
    /// Gets the local scale.
    /// </summary>
    public Store<Vector2> Scale { get; } = new(options?.Scale ?? Vector2.One);
}
