using System.Numerics;
using Mirage.Common.Collections;
using Mirage.Common.Events;
using Mirage.Common.Lifecycle;
using Mirage.Physics.Interfaces;

namespace Mirage.Simulating;

public class SimulationSpace : Destroyable
{
    private bool _composed;
    private bool _compositionStarted;
    private bool _configured;
    private bool _configurationStarted;

    /// <summary>
    /// Gets the gravity applied within the simulation space.
    /// </summary>
    public readonly Store<Vector2> Gravity;

    /// <summary>
    ///  Gets the unique identifier of this space.
    /// </summary>
    public readonly string Identifier;

    /// <summary>
    /// Gets the objects participating in the simulation space.
    /// </summary>
    public readonly ReactiveSet<ISimulatable> Simulatables = [];

    /// <summary>
    /// Initializes a new instance of a <see cref="SimulationSpace"/> class.
    /// </summary>
    /// <param name="identifier">
    /// The unique identifier of the space.
    /// </param>
    /// <param name="simulatables">
    /// The initial objects participating in the simulation.
    /// </param>
    /// <param name="gravity">
    /// The gravity applied within the space.
    /// </param>
    public SimulationSpace(
        string identifier,
        IEnumerable<ISimulatable>? simulatables = null,
        Vector2? gravity = null
    )
    {
        Identifier = identifier;
        foreach (var simulatable in simulatables ?? [])
        {
            Simulatables.Add(simulatable);
        }
        Gravity = new Store<Vector2>(gravity ?? new Vector2(0f, 9.81f));
    }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        if (_compositionStarted)
            throw new InvalidOperationException("Composition has already started or failed.");

        _compositionStarted = true;

        var composedObjects = Compose().ToArray();
        HashSet<ISimulatable> objects = [];

        foreach (var simulatable in composedObjects)
        {
            if (Simulatables.Contains(simulatable) || !objects.Add(simulatable))
                throw new InvalidOperationException("Duplicate composed object found.");
        }

        foreach (var simulatable in composedObjects)
            Simulatables.Add(simulatable);

        _composed = true;
    }

    private void EnsureConfigured()
    {
        if (_configured)
            return;

        if (_configurationStarted)
            throw new InvalidOperationException("Configuration has already started or failed.");

        _configurationStarted = true;
        Configure();
        _configured = true;
    }

    /// <summary>
    /// Composes the simulatables managed by this object.
    /// </summary>
    /// <returns>The simulatables to register, in enumeration order.</returns>
    /// <remarks>
    /// Composition occurs once when first prepared by the simulator, before simulation bindings are created.
    /// Constructor-provided objects are registered before composed objects.
    /// All composed objects are registered before configuration occurs.
    /// The space references simulatables without owning or destroying them.
    /// If composition fails, later lifecycle calls reject further initialization.
    /// </remarks>
    protected virtual IEnumerable<ISimulatable> Compose()
    {
        yield break;
    }

    /// <summary>
    /// Configures relationships and behavior after composition, before startup or first use.
    /// </summary>
    /// <remarks>
    /// All constructor-provided and composed objects are available here.
    /// This hook is invoked at most once, including across later lifecycle cycles.
    /// If configuration throws, later lifecycle calls reject further initialization
    /// rather than repeating configuration side effects.
    /// </remarks>
    protected virtual void Configure() { }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        Gravity.Destroy();
        Simulatables.Destroy();
    }

    internal void Prepare()
    {
        ThrowIfDestroyed();
        EnsureComposed();
        EnsureConfigured();
    }
}
