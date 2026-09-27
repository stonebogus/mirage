using System.Numerics;
using Mirage.Common;
using Mirage.Common.Collections;
using Mirage.Common.Events;
using Mirage.Physics.Colliders;

namespace Mirage.Physics.Resources;

/// <summary>
/// Defines how a physics body participates in simulation.
/// </summary>
public enum BodyType
{
    /// <summary>
    /// The body does not move during simulation.
    /// </summary>
    Static,

    /// <summary>
    /// The body is moved explicitly and is not affected by forces or gravity.
    /// </summary>
    Kinematic,

    /// <summary>
    /// The body is fully simulated and responds to forces, gravity, and collisions.
    /// </summary>
    Dynamic,
}

/// <summary>
/// Represents the physical state and collision geometry of a simulated object.
/// </summary>
/// <remarks>
/// A physics body stores properties that affect how an object behaves during
/// simulation. Spatial placement is owned by the spatial object associated
/// with the body and is not stored here.
///
/// Colliders are borrowed resources and are not destroyed with the body.
/// </remarks>
public class PhysicsBody : Resource
{
    /// <summary>
    /// Gets the amount of damping applied to angular motion.
    /// </summary>
    public readonly Store<float> AngularDamping;

    /// <summary>
    /// Gets the body's angular velocity in radians per second.
    /// </summary>
    public readonly Store<float> AngularVelocity;

    /// <summary>
    /// Gets the colliders that define the body's collision geometry.
    /// </summary>
    public readonly ReactiveSet<Collider> Colliders = [];

    /// <summary>
    /// Gets whether physical simulation prevents this body from rotating.
    /// </summary>
    public readonly Store<bool> FixedRotation;

    /// <summary>
    /// Gets the scale applied to gravity for this body.
    /// </summary>
    public readonly Store<float> GravityScale;

    /// <summary>
    /// Gets the amount of damping applied to linear motion.
    /// </summary>
    public readonly Store<float> LinearDamping;

    /// <summary>
    /// Gets the body's linear velocity in units per second.
    /// </summary>
    public readonly Store<Vector2> LinearVelocity;

    /// <summary>
    /// Gets how the body participates in physical simulation.
    /// </summary>
    public readonly Store<BodyType> Type;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicsBody"/> class.
    /// </summary>
    /// <param name="colliders">
    /// The colliders that define the body's collision geometry.
    /// </param>
    /// <param name="type">
    /// The initial simulation type of the body.
    /// </param>
    public PhysicsBody(IEnumerable<Collider>? colliders = null, BodyType type = BodyType.Dynamic)
    {
        foreach (var collider in colliders ?? [])
        {
            Colliders.Add(collider);
        }

        AngularDamping = new Store<float>(0f);
        AngularVelocity = new Store<float>(0f);
        FixedRotation = new Store<bool>(false);
        GravityScale = new Store<float>(1f);
        LinearDamping = new Store<float>(0f);
        LinearVelocity = new Store<Vector2>(Vector2.Zero);
        Type = new Store<BodyType>(type);
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        AngularDamping.Destroy();
        AngularVelocity.Destroy();
        FixedRotation.Destroy();
        GravityScale.Destroy();
        LinearDamping.Destroy();
        LinearVelocity.Destroy();
        Type.Destroy();
        Colliders.Destroy();
    }
}
