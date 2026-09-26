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
    /// Gets the initial local origin used as the center of scale and rotation.
    /// The default is <see cref="Vector2.Zero"/>.
    /// </summary>
    /// <remarks>
    /// Coordinates use the same application-defined units as <see cref="Position"/>.
    /// </remarks>
    public Vector2 Origin { get; init; } = Vector2.Zero;

    /// <summary>
    /// Gets the initial local origin position relative to the nearest spatial ancestor.
    /// The default is <see cref="Vector2.Zero"/>.
    /// </summary>
    public Vector2 Position { get; init; } = Vector2.Zero;

    /// <summary>
    /// Gets the initial local rotation in radians. The default is <c>0</c>.
    /// </summary>
    public float Rotation { get; init; } = 0f;

    /// <summary>
    /// Gets the initial local scale factor. The default is <see cref="Vector2.One"/>.
    /// </summary>
    public Vector2 Scale { get; init; } = Vector2.One;
}

/// <summary>
/// Represents a node positioned in two-dimensional space.
/// </summary>
/// <remarks>
/// Origin, position, rotation, and scale determine the transformation of this
/// node and its spatial descendants. Ordinary nodes between spatial nodes
/// organize the tree without adding a transformation.
/// </remarks>
public class SpatialNode : Node
{
    private Vector2 _cachedOrigin;
    private Vector2 _cachedPosition;
    private float _cachedRotation;
    private Vector2 _cachedScale;
    private bool _hasCachedLocalTransform;

    /// <summary>
    /// Gets the local origin around which this node and its descendants
    /// are scaled and rotated.
    /// </summary>
    public readonly Store<Vector2> Origin;

    /// <summary>
    /// Gets the position of the origin relative to the node's spatial parent.
    /// </summary>
    public readonly Store<Vector2> Position;

    /// <summary>
    /// Gets the local rotation, expressed in radians.
    /// </summary>
    public readonly Store<float> Rotation;

    /// <summary>
    /// Gets the local scale.
    /// </summary>
    public readonly Store<Vector2> Scale;

    /// <summary>
    /// Initializes a spatial node.
    /// </summary>
    /// <param name="name">The initial name of the node.</param>
    /// <param name="options">
    /// The initial spatial and node values, or <see langword="null"/> for defaults.
    /// </param>
    public SpatialNode(string name = "SpatialNode", SpatialNodeOptions? options = null)
        : base(name, options)
    {
        options ??= new SpatialNodeOptions();

        Origin = new Store<Vector2>(options.Origin);
        Position = new Store<Vector2>(options.Position);
        Rotation = new Store<float>(options.Rotation);
        Scale = new Store<Vector2>(options.Scale);
    }

    /// <summary>
    /// Gets the world position of this node's origin.
    /// </summary>
    public Vector2 GlobalPosition => Vector2.Transform(Origin.Get(), GlobalTransform);

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
    /// <remarks>
    /// The matrix is recalculated only when origin, position, rotation, or scale
    /// has changed since the previous read.
    /// </remarks>
    public Matrix3x2 LocalTransform
    {
        get
        {
            var origin = Origin.Get();
            var position = Position.Get();
            var rotation = Rotation.Get();
            var scale = Scale.Get();

            if (
                !_hasCachedLocalTransform
                || origin != _cachedOrigin
                || position != _cachedPosition
                || rotation != _cachedRotation
                || scale != _cachedScale
            )
            {
                field =
                    Matrix3x2.CreateTranslation(-origin)
                    * Matrix3x2.CreateScale(scale)
                    * Matrix3x2.CreateRotation(rotation)
                    * Matrix3x2.CreateTranslation(position);

                _cachedOrigin = origin;
                _cachedPosition = position;
                _cachedRotation = rotation;
                _cachedScale = scale;
                _hasCachedLocalTransform = true;
            }

            return field;
        }
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        Origin.Destroy();
        Position.Destroy();
        Rotation.Destroy();
        Scale.Destroy();

        base.OnDestroy();
    }
}
