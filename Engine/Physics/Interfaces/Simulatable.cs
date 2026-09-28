using System.Numerics;
using Mirage.Common.Events;
using Mirage.Physics.Resources;

namespace Mirage.Physics.Interfaces;

/// <summary>
/// Represents an object that can participate in physical simulation.
/// </summary>
/// <remarks>
/// A simulatable exposes the physical body being simulated together with its
/// world-space position and rotation.
///
/// The simulation system operates exclusively through this contract and does
/// not depend on the spatial or node representation of the object.
/// </remarks>
public interface ISimulatable
{
    /// <summary>
    /// Gets the physical body associated with the simulated object.
    /// </summary>
    /// <remarks>The simulation system borrows both the store and its body.
    /// Their lifetime is managed by the implementing object and the body's resource owner.</remarks>
    Store<PhysicsBody> Body { get; }

    /// <summary>
    /// Gets the world-space position of the simulated object.
    /// </summary>
    Vector2 GlobalPosition { get; set; }

    /// <summary>
    /// Gets the world-space rotation of the simulated object, expressed in radians.
    /// </summary>
    float GlobalRotation { get; set; }
}
