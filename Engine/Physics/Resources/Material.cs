using Mirage.Common;
using Mirage.Common.Events;

namespace Mirage.Physics.Resources;

/// <summary>
/// Defines reusable physical surface properties for colliders.
/// </summary>
/// <remarks>
/// A physical material describes how surfaces interact during contact.
/// Materials are reusable resources and may be shared by multiple colliders.
/// </remarks>
public class PhysicsMaterial : Resource
{
    /// <summary>
    /// Gets the coefficient of friction applied during contact.
    /// </summary>
    public readonly Store<float> Friction;

    /// <summary>
    /// Gets the coefficient of restitution applied during contact.
    /// </summary>
    public readonly Store<float> Restitution;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicsMaterial"/> class.
    /// </summary>
    /// <param name="friction">
    /// The initial coefficient of friction. Must be finite and non-negative.
    /// </param>
    /// <param name="restitution">
    /// The initial coefficient of restitution, from <c>0</c> for no bounce
    /// to <c>1</c> for full bounce.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="friction"/> is negative or non-finite,
    /// or when <paramref name="restitution"/> is non-finite or outside the
    /// range from <c>0</c> through <c>1</c>.
    /// </exception>
    public PhysicsMaterial(float friction = 0.6f, float restitution = 0f)
    {
        ValidateFriction(friction);
        ValidateRestitution(restitution);

        Friction = new Store<float>(friction);
        Restitution = new Store<float>(restitution);

        Friction.Connect(ValidateFriction, true);
        Restitution.Connect(ValidateRestitution, true);
    }

    private void ValidateFriction(float friction)
    {
        if (!float.IsFinite(friction) || friction < 0f)
            throw new ArgumentOutOfRangeException(nameof(friction));
    }

    private void ValidateRestitution(float restitution)
    {
        if (!float.IsFinite(restitution) || restitution is < 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(restitution));
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        Friction.Destroy();
        Restitution.Destroy();

        base.OnDestroy();
    }
}
